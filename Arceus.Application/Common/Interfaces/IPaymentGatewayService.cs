using Arceus.Domain.ValueObjects;

namespace Arceus.Application.Common.Interfaces;

public interface IPaymentGatewayService
{
    Task<PaymentResult> ProcessPaymentAsync(string paymentToken, Money amount, CancellationToken cancellationToken = default);
}

public record PaymentResult(bool IsSuccess, string? TransactionId, string? ErrorMessage);