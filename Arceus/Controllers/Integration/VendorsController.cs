using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/vendors")]
public class VendorsController : ControllerBase
{
    private readonly IContractorRepository _contractorRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendorsController(
        IContractorRepository contractorRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _contractorRepository = contractorRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<CreateVendorResponse>> CreateVendor(
        [FromBody] CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create contractor (vendor)
            var contractor = new Contractor(request.VendorName, ContractorType.Partner);
            await _contractorRepository.AddAsync(contractor, cancellationToken);

            // Create revenue account for vendor earnings
            var revenueAccount = contractor.CreateAccount(AccountType.Revenue);
            await _accountRepository.AddAsync(revenueAccount, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetVendor),
                new { id = contractor.Id },
                new CreateVendorResponse(contractor.Id, revenueAccount.Id));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{vendorId}/bills")]
    public async Task<ActionResult<CreateVendorBillResponse>> CreateVendorBill(
        long vendorId,
        [FromBody] CreateVendorBillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get vendor revenue account
            var vendorAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                vendorId,
                AccountType.Revenue,
                cancellationToken);

            if (vendorAccount == null)
            {
                return NotFound(new { error = "Vendor account not found" });
            }

            // Create bill transaction
            var transaction = new Transaction($"Vendor commission bill - {request.Description}", null);

            // Debit company payable (company owes vendor)
            var companyPayableAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                request.CompanyId,
                AccountType.Payable,
                cancellationToken);

            if (companyPayableAccount != null)
            {
                transaction.AddJournalEntry(companyPayableAccount.Id, new Money(request.Amount), Money.Zero);
            }

            // Credit vendor revenue
            transaction.AddJournalEntry(vendorAccount.Id, Money.Zero, new Money(request.Amount));

            transaction.MarkComplete();

            // Update balances
            companyPayableAccount?.Debit(new Money(request.Amount));
            vendorAccount.Credit(new Money(request.Amount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            if (companyPayableAccount != null)
            {
                _accountRepository.Update(companyPayableAccount);
            }
            _accountRepository.Update(vendorAccount);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new CreateVendorBillResponse(
                transaction.Id,
                request.Amount,
                "CREATED"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{vendorId}")]
    public async Task<ActionResult<GetVendorResponse>> GetVendor(
        long vendorId,
        CancellationToken cancellationToken)
    {
        var contractor = await _contractorRepository.GetByIdAsync(vendorId, cancellationToken);
        if (contractor == null || contractor.ContractorType != ContractorType.Partner)
        {
            return NotFound(new { error = "Vendor not found" });
        }

        var revenueAccount = contractor.Accounts.FirstOrDefault(a => a.AccountType == AccountType.Revenue);

        return Ok(new GetVendorResponse(
            contractor.Id,
            contractor.FullName,
            contractor.CreatedAt,
            revenueAccount?.Balance.Amount ?? 0
        ));
    }

    [HttpGet("{vendorId}/total-amount")]
    public async Task<ActionResult<GetVendorTotalAmountResponse>> GetVendorTotalAmount(
        long vendorId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var vendorAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            vendorId,
            AccountType.Revenue,
            cancellationToken);

        if (vendorAccount == null)
        {
            return NotFound(new { error = "Vendor account not found" });
        }

        // This would need proper implementation with transaction history queries
        var totalSales = vendorAccount.Balance.Amount; // Placeholder
        var commissionRate = 0.15m; // 15% commission example
        var commission = totalSales * commissionRate;
        var netPayout = totalSales - commission;

        return Ok(new GetVendorTotalAmountResponse(
            vendorId,
            totalSales,
            commission,
            netPayout,
            fromDate ?? DateTime.UtcNow.AddDays(-30),
            toDate ?? DateTime.UtcNow
        ));
    }

    [HttpGet("{vendorId}/transactions")]
    public async Task<ActionResult<GetVendorTransactionsResponse>> GetVendorTransactions(
        long vendorId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var vendorAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            vendorId,
            AccountType.Revenue,
            cancellationToken);

        if (vendorAccount == null)
        {
            return NotFound(new { error = "Vendor account not found" });
        }

        // This would need proper implementation with transaction history queries
        return Ok(new GetVendorTransactionsResponse(
            vendorId,
            new List<VendorTransactionItem>(), // Placeholder
            page,
            pageSize,
            0,
            fromDate ?? DateTime.UtcNow.AddDays(-30),
            toDate ?? DateTime.UtcNow
        ));
    }

    [HttpPost("{vendorId}/settlement")]
    public async Task<ActionResult<ProcessVendorSettlementResponse>> ProcessVendorSettlement(
        long vendorId,
        [FromBody] ProcessVendorSettlementRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var vendorAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                vendorId,
                AccountType.Revenue,
                cancellationToken);

            if (vendorAccount == null)
            {
                return NotFound(new { error = "Vendor account not found" });
            }

            if (vendorAccount.Balance < new Money(request.SettlementAmount))
            {
                return BadRequest(new { error = "Insufficient vendor balance for settlement" });
            }

            // Create settlement transaction
            var transaction = new Transaction($"Vendor settlement payment - {request.PaymentReference}", null);

            // Debit vendor account (settlement paid)
            transaction.AddJournalEntry(vendorAccount.Id, new Money(request.SettlementAmount), Money.Zero);

            // Credit company cash account (cash paid out)
            var companyCashAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                request.CompanyId,
                AccountType.Payable,
                cancellationToken);

            if (companyCashAccount != null)
            {
                transaction.AddJournalEntry(companyCashAccount.Id, Money.Zero, new Money(request.SettlementAmount));
            }

            transaction.MarkComplete();

            // Update balances
            vendorAccount.Debit(new Money(request.SettlementAmount));
            companyCashAccount?.Credit(new Money(request.SettlementAmount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            _accountRepository.Update(vendorAccount);
            if (companyCashAccount != null)
            {
                _accountRepository.Update(companyCashAccount);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new ProcessVendorSettlementResponse(
                transaction.Id,
                request.SettlementAmount,
                vendorAccount.Balance.Amount,
                "COMPLETED"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateVendorRequest(string VendorName);

public record CreateVendorResponse(long VendorId, long RevenueAccountId);

public record CreateVendorBillRequest(
    decimal Amount,
    string Description,
    long CompanyId
);

public record CreateVendorBillResponse(
    long TransactionId,
    decimal Amount,
    string Status
);

public record GetVendorResponse(
    long Id,
    string VendorName,
    DateTime CreatedAt,
    decimal TotalEarnings
);

public record GetVendorTotalAmountResponse(
    long VendorId,
    decimal TotalSales,
    decimal Commission,
    decimal NetPayout,
    DateTime FromDate,
    DateTime ToDate
);

public record GetVendorTransactionsResponse(
    long VendorId,
    List<VendorTransactionItem> Transactions,
    int Page,
    int PageSize,
    int TotalCount,
    DateTime FromDate,
    DateTime ToDate
);

public record VendorTransactionItem(
    long TransactionId,
    string Description,
    decimal Amount,
    string Type, // "SALE", "COMMISSION", "SETTLEMENT"
    DateTime CreatedAt
);

public record ProcessVendorSettlementRequest(
    decimal SettlementAmount,
    string PaymentReference,
    long CompanyId
);

public record ProcessVendorSettlementResponse(
    long TransactionId,
    decimal SettlementAmount,
    decimal RemainingBalance,
    string Status
);