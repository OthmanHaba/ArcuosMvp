using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly IContractorRepository _contractorRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestaurantsController(
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
    public async Task<ActionResult<CreateRestaurantResponse>> CreateRestaurant(
        [FromBody] CreateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create contractor (restaurant)
            var contractor = new Contractor(request.RestaurantName, ContractorType.Partner);
            await _contractorRepository.AddAsync(contractor, cancellationToken);

            // Create revenue account for restaurant earnings
            var revenueAccount = contractor.CreateAccount(AccountType.Revenue);
            await _accountRepository.AddAsync(revenueAccount, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetRestaurant),
                new { id = contractor.Id },
                new CreateRestaurantResponse(contractor.Id, revenueAccount.Id));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{restaurantId}/earnings")]
    public async Task<ActionResult<AddRestaurantEarningsResponse>> AddRestaurantEarnings(
        long restaurantId,
        [FromBody] AddRestaurantEarningsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get restaurant revenue account
            var restaurantAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                restaurantId,
                AccountType.Revenue,
                cancellationToken);

            if (restaurantAccount == null)
            {
                return NotFound(new { error = "Restaurant account not found" });
            }

            // Create earning transaction
            var description = request.OrderId.HasValue
                ? $"Restaurant earnings for order #{request.OrderId}"
                : "Restaurant earnings";

            var transaction = new Transaction(description, request.OrderId);

            // Credit restaurant account
            transaction.AddJournalEntry(restaurantAccount.Id, Money.Zero, new Money(request.Amount));

            // Debit company payable account (company owes restaurant)
            var companyPayableAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                request.CompanyId,
                AccountType.Payable,
                cancellationToken);

            if (companyPayableAccount != null)
            {
                transaction.AddJournalEntry(companyPayableAccount.Id, new Money(request.Amount), Money.Zero);
            }

            transaction.MarkComplete();

            // Update balances
            restaurantAccount.Credit(new Money(request.Amount));
            companyPayableAccount?.Debit(new Money(request.Amount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            _accountRepository.Update(restaurantAccount);
            if (companyPayableAccount != null)
            {
                _accountRepository.Update(companyPayableAccount);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new AddRestaurantEarningsResponse(
                transaction.Id,
                restaurantAccount.Balance.Amount
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{restaurantId}")]
    public async Task<ActionResult<GetRestaurantResponse>> GetRestaurant(
        long restaurantId,
        CancellationToken cancellationToken)
    {
        var contractor = await _contractorRepository.GetByIdAsync(restaurantId, cancellationToken);
        if (contractor == null || contractor.ContractorType != ContractorType.Partner)
        {
            return NotFound(new { error = "Restaurant not found" });
        }

        var revenueAccount = contractor.Accounts.FirstOrDefault(a => a.AccountType == AccountType.Revenue);

        return Ok(new GetRestaurantResponse(
            contractor.Id,
            contractor.FullName,
            contractor.CreatedAt,
            revenueAccount?.Balance.Amount ?? 0
        ));
    }

    [HttpGet("{restaurantId}/earnings")]
    public async Task<ActionResult<GetRestaurantEarningsResponse>> GetRestaurantEarnings(
        long restaurantId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var restaurantAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            restaurantId,
            AccountType.Revenue,
            cancellationToken);

        if (restaurantAccount == null)
        {
            return NotFound(new { error = "Restaurant account not found" });
        }

        // This would need proper implementation with transaction history queries
        return Ok(new GetRestaurantEarningsResponse(
            restaurantId,
            restaurantAccount.Balance.Amount,
            new List<RestaurantEarningItem>(), // Placeholder
            fromDate ?? DateTime.UtcNow.AddDays(-30),
            toDate ?? DateTime.UtcNow
        ));
    }

    [HttpGet("{restaurantId}/settlement-status")]
    public async Task<ActionResult<GetRestaurantSettlementStatusResponse>> GetRestaurantSettlementStatus(
        long restaurantId,
        CancellationToken cancellationToken)
    {
        var restaurantAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            restaurantId,
            AccountType.Revenue,
            cancellationToken);

        if (restaurantAccount == null)
        {
            return NotFound(new { error = "Restaurant account not found" });
        }

        return Ok(new GetRestaurantSettlementStatusResponse(
            restaurantId,
            restaurantAccount.Balance.Amount,
            restaurantAccount.Balance.Amount > Money.Zero ? "PENDING" : "SETTLED",
            DateTime.UtcNow // Last settlement date placeholder
        ));
    }

    [HttpPost("{restaurantId}/settlement")]
    public async Task<ActionResult<ProcessRestaurantSettlementResponse>> ProcessRestaurantSettlement(
        long restaurantId,
        [FromBody] ProcessRestaurantSettlementRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var restaurantAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                restaurantId,
                AccountType.Revenue,
                cancellationToken);

            if (restaurantAccount == null)
            {
                return NotFound(new { error = "Restaurant account not found" });
            }

            if (restaurantAccount.Balance < new Money(request.SettlementAmount))
            {
                return BadRequest(new { error = "Insufficient restaurant balance for settlement" });
            }

            // Create settlement transaction
            var transaction = new Transaction($"Restaurant settlement payment - {request.PaymentReference}", null);

            // Debit restaurant account (settlement paid)
            transaction.AddJournalEntry(restaurantAccount.Id, new Money(request.SettlementAmount), Money.Zero);

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
            restaurantAccount.Debit(new Money(request.SettlementAmount));
            companyCashAccount?.Credit(new Money(request.SettlementAmount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            _accountRepository.Update(restaurantAccount);
            if (companyCashAccount != null)
            {
                _accountRepository.Update(companyCashAccount);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new ProcessRestaurantSettlementResponse(
                transaction.Id,
                request.SettlementAmount,
                restaurantAccount.Balance.Amount,
                "COMPLETED"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateRestaurantRequest(string RestaurantName);

public record CreateRestaurantResponse(long RestaurantId, long RevenueAccountId);

public record AddRestaurantEarningsRequest(
    decimal Amount,
    long? OrderId,
    long CompanyId
);

public record AddRestaurantEarningsResponse(
    long TransactionId,
    decimal TotalEarnings
);

public record GetRestaurantResponse(
    long Id,
    string RestaurantName,
    DateTime CreatedAt,
    decimal TotalEarnings
);

public record GetRestaurantEarningsResponse(
    long RestaurantId,
    decimal TotalEarnings,
    List<RestaurantEarningItem> Earnings,
    DateTime FromDate,
    DateTime ToDate
);

public record RestaurantEarningItem(
    long TransactionId,
    string Description,
    decimal Amount,
    DateTime EarnedAt
);

public record GetRestaurantSettlementStatusResponse(
    long RestaurantId,
    decimal PendingAmount,
    string Status, // "PENDING", "SETTLED", "PROCESSING"
    DateTime? LastSettlementDate
);

public record ProcessRestaurantSettlementRequest(
    decimal SettlementAmount,
    string PaymentReference,
    long CompanyId
);

public record ProcessRestaurantSettlementResponse(
    long TransactionId,
    decimal SettlementAmount,
    decimal RemainingBalance,
    string Status
);