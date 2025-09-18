using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using MediatR;

namespace Arceus.Application.Features.Accounts.Commands.ChargeWallet;

public class ChargeWalletCommandHandler : IRequestHandler<ChargeWalletCommand, ChargeWalletResult>
{
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChargeWalletCommandHandler(
        IPaymentGatewayService paymentGatewayService,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork
    )
    {
        _paymentGatewayService = paymentGatewayService;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChargeWalletResult> Handle(ChargeWalletCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= Money.Zero)
        {
            throw new ArgumentException("Charge amount must be positive");
        }

        var paymentResult = await _paymentGatewayService.ProcessPaymentAsync(
            request.PaymentToken,
            request.Amount,
            cancellationToken);

        if (!paymentResult.IsSuccess)
        {
            throw new InvalidOperationException($"Payment failed: {paymentResult.ErrorMessage}");
        }

        // Fetch required accounts
        var customerWalletAccount =
            await _accountRepository.GetByOwnerAndTypeAsync(request.CustomerId, AccountType.Wallet, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} does not have a wallet account");

        var companyCashAccount =
            await _accountRepository.GetByOwnerAndTypeAsync(request.CompanyId, AccountType.Payable, cancellationToken)
            ?? throw new InvalidOperationException($"Company {request.CompanyId} does not have a payable account");

        // Create the transaction
        var description =
            $"Wallet top-up for customer {request.CustomerId} - Payment ID: {paymentResult.TransactionId}";
        var transaction = new Transaction(description);

        // Add journal entries
        // Credit customer wallet (money in)
        transaction.AddJournalEntry(customerWalletAccount.Id, Money.Zero, request.Amount);

        // Debit company cash account (representing cash received from payment gateway)
        transaction.AddJournalEntry(companyCashAccount.Id, request.Amount, Money.Zero);

        // Validate double-entry accounting
        transaction.ValidateDoubleEntry();

        // Update account balances
        customerWalletAccount.Credit(request.Amount);
        companyCashAccount.Debit(request.Amount);

        // Mark transaction as complete
        transaction.MarkComplete();

        // Persist changes
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        _accountRepository.Update(customerWalletAccount);
        _accountRepository.Update(companyCashAccount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChargeWalletResult(transaction.Id, paymentResult.TransactionId!);
    }
}