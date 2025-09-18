using Arceus.Application.Common.Interfaces;
using Arceus.Domain.ValueObjects;

namespace Arceus.Infrastructure.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    public async Task<PaymentResult> ProcessPaymentAsync(string paymentToken, Money amount, CancellationToken cancellationToken = default)
    {
        // Simulate async payment processing
        await Task.Delay(100, cancellationToken);

        // Simple validation - in real implementation, this would call external payment API
        if (string.IsNullOrWhiteSpace(paymentToken))
        {
            return new PaymentResult(false, null, "Invalid payment token");
        }

        if (amount <= Money.Zero)
        {
            return new PaymentResult(false, null, "Invalid payment amount");
        }

        // Simulate successful payment
        var transactionId = Guid.NewGuid().ToString();
        return new PaymentResult(true, transactionId, null);
    }
}