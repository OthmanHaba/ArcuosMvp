using Arceus.Domain.ValueObjects;
using MediatR;

namespace Arceus.Application.Features.Accounts.Commands.ChargeWallet;

public record ChargeWalletCommand(
    long CustomerId,
    Money Amount,
    string PaymentToken,
    long CompanyId
) : IRequest<ChargeWalletResult>;

public record ChargeWalletResult(long TransactionId, string PaymentTransactionId);