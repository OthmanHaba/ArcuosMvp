using Arceus.Domain.ValueObjects;
using MediatR;

namespace Arceus.Application.Features.Transactions.Commands.CreateTransaction;

public record CreateTransactionCommand(
    long CustomerId,
    long? OrderId,
    Money TotalAmount,
    Money DriverShare,
    Money PartnerShare,
    Money CompanyShare,
    long DriverId,
    long PartnerId,
    long CompanyId
) : IRequest<CreateTransactionResult>;

public record CreateTransactionResult(long TransactionId);