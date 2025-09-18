using Arceus.Domain.ValueObjects;

namespace Arceus.Domain.Events;

public record TransactionCreatedEvent(
    long TransactionId,
    string Description,
    Money TotalAmount,
    long? OrderId,
    DateTime CreatedAt
);