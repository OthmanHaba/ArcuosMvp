using Arceus.Application.Common;
using Arceus.Application.Features.Accounts.Commands.ChargeWallet;
using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/wallet")]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WalletController(
        IMediator mediator,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("charge")]
    public async Task<ActionResult<ChargeWalletResponse>> ChargeWallet(
        [FromBody] ChargeWalletIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ChargeWalletCommand(
                request.CustomerId,
                new Money(request.Amount),
                request.PaymentToken
            );

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ChargeWalletResponse(
                result.TransactionId,
                result.PaymentTransactionId,
                request.Amount
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("deduct")]
    public async Task<ActionResult<DeductWalletResponse>> DeductWallet(
        [FromBody] DeductWalletRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get customer wallet account
            var walletAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                request.CustomerId,
                AccountType.Wallet,
                cancellationToken);

            if (walletAccount == null)
            {
                return NotFound(new { error = "Customer wallet not found" });
            }

            // Check sufficient funds
            if (!walletAccount.HasSufficientFunds(new Money(request.Amount)))
            {
                return BadRequest(new { error = "Insufficient wallet balance" });
            }

            // Create debit transaction
            var transaction = new Transaction($"Wallet deduction for order #{request.OrderId}", request.OrderId);

            // Debit customer wallet
            transaction.AddJournalEntry(walletAccount.Idddd, new Money(request.Amount), Money.Zero);

            // Credit company revenue (assuming company ID is provided)
            var companyRevenueAccount = await _accountRepository.GetByOwnerAndTypeAsync(
                Global.CompanyId,
                AccountType.Revenue,
                cancellationToken);

            if (companyRevenueAccount != null)
            {
                transaction.AddJournalEntry(companyRevenueAccount.Idddd, Money.Zero, new Money(request.Amount));
            }

            transaction.MarkComplete();

            // Update account balances
            walletAccount.Debit(new Money(request.Amount));
            companyRevenueAccount?.Credit(new Money(request.Amount));

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            _accountRepository.Update(walletAccount);
            if (companyRevenueAccount != null)
            {
                _accountRepository.Update(companyRevenueAccount);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(new DeductWalletResponse(
                transaction.Id,
                walletAccount.Balance.Amount
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{customerId}/balance")]
    public async Task<ActionResult<WalletBalanceResponse>> GetWalletBalance(
        long customerId,
        CancellationToken cancellationToken)
    {
        var walletAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            customerId,
            AccountType.Wallet,
            cancellationToken);

        if (walletAccount == null)
        {
            return NotFound(new { error = "Customer wallet not found" });
        }

        return Ok(new WalletBalanceResponse(
            customerId,
            walletAccount.Balance.Amount,
            DateTime.UtcNow
        ));
    }

    [HttpGet("{customerId}/transactions")]
    public async Task<ActionResult<WalletTransactionsResponse>> GetWalletTransactions(
        long customerId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var walletAccount = await _accountRepository.GetByOwnerAndTypeAsync(
            customerId,
            AccountType.Wallet,
            cancellationToken);

        if (walletAccount == null)
        {
            return NotFound(new { error = "Customer wallet not found" });
        }

        // This would need to be implemented with proper pagination in the repository
        return Ok(new WalletTransactionsResponse(
            customerId,
            new List<WalletTransactionItem>(), // Placeholder
            page,
            pageSize,
            0
        ));
    }
}

public record ChargeWalletIntegrationRequest(
    long CustomerId,
    decimal Amount,
    string PaymentToken
);

public record ChargeWalletResponse(
    long TransactionId,
    string PaymentTransactionId,
    decimal Amount
);

public record DeductWalletRequest(
    long CustomerId,
    decimal Amount,
    long? OrderId
);

public record DeductWalletResponse(
    long TransactionId,
    decimal RemainingBalance
);

public record WalletBalanceResponse(
    long CustomerId,
    decimal Balance,
    DateTime AsOfDate
);

public record WalletTransactionsResponse(
    long CustomerId,
    List<WalletTransactionItem> Transactions,
    int Page,
    int PageSize,
    int TotalCount
);

public record WalletTransactionItem(
    long TransactionId,
    string Description,
    decimal Amount,
    string Type, // "CREDIT" or "DEBIT"
    DateTime CreatedAt
);