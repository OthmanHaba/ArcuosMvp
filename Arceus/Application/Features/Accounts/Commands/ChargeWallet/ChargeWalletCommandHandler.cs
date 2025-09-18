using Arceus.Application.Common;
using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using MediatR;

namespace Arceus.Application.Features.Accounts.Commands.ChargeWallet;

public class ChargeWalletCommandHandler(
    IPaymentGatewayService paymentGatewayService,
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChargeWalletCommand, ChargeWalletResult>
{
    public async Task<ChargeWalletResult> Handle(ChargeWalletCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= Money.Zero)
        {
            throw new ArgumentException("Charge amount must be positive");
        }

        var paymentResult = await paymentGatewayService.ProcessPaymentAsync(
            request.PaymentToken,
            request.Amount,
            cancellationToken);

        if (!paymentResult.IsSuccess)
        {
            throw new InvalidOperationException($"Payment failed: {paymentResult.ErrorMessage}");
        }

        // Fetch required accounts
        var customerWalletAccount =
            await accountRepository.GetByOwnerAndTypeAsync(request.CustomerId, AccountType.Wallet, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} does not have a wallet account");

        var companyCashAccount =
            await accountRepository.GetByOwnerAndTypeAsync(Global.CompanyId, AccountType.Payable,
                cancellationToken)
            ?? throw new InvalidOperationException($"Company {Global.CompanyId} does not have a payable account");

        // Create the transaction
        var description =
            $"Wallet top-up for customer {request.CustomerId} - Payment ID: {paymentResult.TransactionId}";
        var transaction = new Transaction(description);

        // Add journal entries
        // Credit customer wallet (money in)
        transaction.AddJournalEntry(customerWalletAccount.Idddd, Money.Zero, request.Amount);

        // Debit company cash account (representing cash received from payment gateway)
        transaction.AddJournalEntry(companyCashAccount.Idddd, request.Amount, Money.Zero);

        // Validate double-entry accounting
        transaction.ValidateDoubleEntry();

        // Update account balances
        customerWalletAccount.Credit(request.Amount);
        // companyCashAccount.Debit(request.Amount);

        // Mark transaction as complete
        transaction.MarkComplete();

        // Persist changes
        await transactionRepository.AddAsync(transaction, cancellationToken);
        accountRepository.Update(customerWalletAccount);
        // _accountRepository.Update(companyCashAccount);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChargeWalletResult(transaction.Id, paymentResult.TransactionId!);
    }
}