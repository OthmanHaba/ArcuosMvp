using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;
using MediatR;

namespace Arceus.Application.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, CreateTransactionResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateTransactionResult> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        // Validate that shares add up to total amount
        var totalShares = request.DriverShare + request.PartnerShare + request.CompanyShare;
        if (Math.Abs(totalShares.Amount - request.TotalAmount.Amount) > 0.0001m)
        {
            throw new InvalidOperationException("The sum of all shares must equal the total amount");
        }

        // Fetch all required accounts
        var customerAccount = await _accountRepository.GetByOwnerAndTypeAsync(request.CustomerId, AccountType.Wallet, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} does not have a wallet account");

        var driverAccount = await _accountRepository.GetByOwnerAndTypeAsync(request.DriverId, AccountType.Revenue, cancellationToken)
            ?? throw new InvalidOperationException($"Driver {request.DriverId} does not have a revenue account");

        var partnerAccount = await _accountRepository.GetByOwnerAndTypeAsync(request.PartnerId, AccountType.Revenue, cancellationToken)
            ?? throw new InvalidOperationException($"Partner {request.PartnerId} does not have a revenue account");

        var companyAccount = await _accountRepository.GetByOwnerAndTypeAsync(request.CompanyId, AccountType.Revenue, cancellationToken)
            ?? throw new InvalidOperationException($"Company {request.CompanyId} does not have a revenue account");

        // Business rule validation: customer must have sufficient funds
        if (!customerAccount.HasSufficientFunds(request.TotalAmount))
        {
            throw new InvalidOperationException("Customer has insufficient funds for this transaction");
        }

        // Create the transaction
        var description = request.OrderId.HasValue
            ? $"Order payment for order #{request.OrderId}"
            : "Payment transaction";

        var transaction = new Transaction(description, request.OrderId);

        // Add journal entries
        // Debit customer account (money out)
        transaction.AddJournalEntry(customerAccount.Id, request.TotalAmount, Money.Zero);

        // Credit driver account (money in)
        if (request.DriverShare > Money.Zero)
        {
            transaction.AddJournalEntry(driverAccount.Id, Money.Zero, request.DriverShare);
        }

        // Credit partner account (money in)
        if (request.PartnerShare > Money.Zero)
        {
            transaction.AddJournalEntry(partnerAccount.Id, Money.Zero, request.PartnerShare);
        }

        // Credit company account (money in)
        if (request.CompanyShare > Money.Zero)
        {
            transaction.AddJournalEntry(companyAccount.Id, Money.Zero, request.CompanyShare);
        }

        // Validate double-entry accounting
        transaction.ValidateDoubleEntry();

        // Update account balances
        customerAccount.Debit(request.TotalAmount);

        if (request.DriverShare > Money.Zero)
        {
            driverAccount.Credit(request.DriverShare);
        }

        if (request.PartnerShare > Money.Zero)
        {
            partnerAccount.Credit(request.PartnerShare);
        }

        if (request.CompanyShare > Money.Zero)
        {
            companyAccount.Credit(request.CompanyShare);
        }

        // Mark transaction as complete to generate domain events
        transaction.MarkComplete();

        // Persist changes
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        _accountRepository.Update(customerAccount);
        _accountRepository.Update(driverAccount);
        _accountRepository.Update(partnerAccount);
        _accountRepository.Update(companyAccount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateTransactionResult(transaction.Id);
    }
}