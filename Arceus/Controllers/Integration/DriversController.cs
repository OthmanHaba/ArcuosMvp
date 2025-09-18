using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/drivers")]
public class DriversController : ControllerBase
{
    private readonly IContractorRepository _contractorRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DriversController(
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
    public async Task<ActionResult<CreateDriverResponse>> CreateDriver(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create contractor (driver)
            var contractor = new Contractor(request.FullName, ContractorType.Driver);
            await _contractorRepository.AddAsync(contractor, cancellationToken);

            // Create revenue account for driver earnings
            var revenueAccount = contractor.CreateAccount(AccountType.Revenue);
            await _accountRepository.AddAsync(revenueAccount, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetDriver),
                new { id = contractor.Id },
                new CreateDriverResponse(contractor.Id, revenueAccount.Id));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{driverId}/earnings")]
    public async Task<ActionResult<AddDriverEarningsResponse>> AddDriverEarnings(
        long driverId,
        [FromBody] AddDriverEarningsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get driver revenue account
            var driverAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                driverId,
                AccountType.Revenue,
                cancellationToken);

            if (driverAccount == null)
            {
                return NotFound(new { error = "Driver account not found" });
            }

            // Create earning transaction
            var description = request.OrderId.HasValue
                ? $"Delivery earnings for order #{request.OrderId}"
                : "Driver earnings";

            var transaction = new Transaction(description, request.OrderId);

            // Credit driver account
            transaction.AddJournalEntry(driverAccount.Id, Money.Zero, new Money(request.Amount));

            // Debit company payable account (company owes driver)
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
            driverAccount.Credit(new Money(request.Amount));
            companyPayableAccount?.Debit(new Money(request.Amount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            _accountRepository.Update(driverAccount);
            if (companyPayableAccount != null)
            {
                _accountRepository.Update(companyPayableAccount);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new AddDriverEarningsResponse(
                transaction.Id,
                driverAccount.Balance.Amount
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{driverId}/rewards")]
    public async Task<ActionResult<AddDriverRewardResponse>> AddDriverReward(
        long driverId,
        [FromBody] AddDriverRewardRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var driverAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                driverId,
                AccountType.Revenue,
                cancellationToken);

            if (driverAccount == null)
            {
                return NotFound(new { error = "Driver account not found" });
            }

            var transaction = new Transaction($"Driver reward: {request.RewardType}", null);

            // Credit driver account
            transaction.AddJournalEntry(driverAccount.Id, Money.Zero, new Money(request.Amount));

            // Debit company payable account
            var companyPayableAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                request.CompanyId,
                AccountType.Payable,
                cancellationToken);

            if (companyPayableAccount != null)
            {
                transaction.AddJournalEntry(companyPayableAccount.Id, new Money(request.Amount), Money.Zero);
            }

            transaction.MarkComplete();

            driverAccount.Credit(new Money(request.Amount));
            companyPayableAccount?.Debit(new Money(request.Amount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            _accountRepository.Update(driverAccount);
            if (companyPayableAccount != null)
            {
                _accountRepository.Update(companyPayableAccount);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new AddDriverRewardResponse(transaction.Id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{driverId}")]
    public async Task<ActionResult<GetDriverResponse>> GetDriver(
        long driverId,
        CancellationToken cancellationToken)
    {
        var contractor = await _contractorRepository.GetByIdAsync(driverId, cancellationToken);
        if (contractor == null || contractor.ContractorType != ContractorType.Driver)
        {
            return NotFound(new { error = "Driver not found" });
        }

        var revenueAccount = contractor.Accounts.FirstOrDefault(a => a.AccountType == AccountType.Revenue);

        return Ok(new GetDriverResponse(
            contractor.Id,
            contractor.FullName,
            contractor.CreatedAt,
            revenueAccount?.Balance.Amount ?? 0
        ));
    }

    [HttpGet("{driverId}/earnings")]
    public async Task<ActionResult<GetDriverEarningsResponse>> GetDriverEarnings(
        long driverId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var driverAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            driverId,
            AccountType.Revenue,
            cancellationToken);

        if (driverAccount == null)
        {
            return NotFound(new { error = "Driver account not found" });
        }

        // This would need proper implementation with transaction history queries
        return Ok(new GetDriverEarningsResponse(
            driverId,
            driverAccount.Balance.Amount,
            new List<DriverEarningItem>(), // Placeholder
            fromDate ?? DateTime.UtcNow.AddDays(-30),
            toDate ?? DateTime.UtcNow
        ));
    }

    [HttpGet("{driverId}/settlement-status")]
    public async Task<ActionResult<GetDriverSettlementStatusResponse>> GetDriverSettlementStatus(
        long driverId,
        CancellationToken cancellationToken)
    {
        var driverAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            driverId,
            AccountType.Revenue,
            cancellationToken);

        if (driverAccount == null)
        {
            return NotFound(new { error = "Driver account not found" });
        }

        return Ok(new GetDriverSettlementStatusResponse(
            driverId,
            driverAccount.Balance.Amount,
            driverAccount.Balance.Amount > Money.Zero ? "PENDING" : "SETTLED",
            DateTime.UtcNow // Last settlement date placeholder
        ));
    }
}

public record CreateDriverRequest(string FullName);

public record CreateDriverResponse(long DriverId, long RevenueAccountId);

public record AddDriverEarningsRequest(
    decimal Amount,
    long? OrderId,
    long CompanyId
);

public record AddDriverEarningsResponse(
    long TransactionId,
    decimal TotalEarnings
);

public record AddDriverRewardRequest(
    decimal Amount,
    string RewardType,
    long CompanyId
);

public record AddDriverRewardResponse(long TransactionId);

public record GetDriverResponse(
    long Id,
    string FullName,
    DateTime CreatedAt,
    decimal TotalEarnings
);

public record GetDriverEarningsResponse(
    long DriverId,
    decimal TotalEarnings,
    List<DriverEarningItem> Earnings,
    DateTime FromDate,
    DateTime ToDate
);

public record DriverEarningItem(
    long TransactionId,
    string Description,
    decimal Amount,
    DateTime EarnedAt
);

public record GetDriverSettlementStatusResponse(
    long DriverId,
    decimal PendingAmount,
    string Status, // "PENDING", "SETTLED", "PROCESSING"
    DateTime? LastSettlementDate
);