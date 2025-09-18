using Arceus.Application.Features.Transactions.Commands.CreateTransaction;
using Arceus.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/orders")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost("jet")]
    public async Task<ActionResult<CreateOrderResponse>> CreateJetOrder(
        [FromBody] CreateJetOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateTransactionCommand(
                request.CustomerId,
                request.OrderId,
                new Money(request.TotalAmount),
                new Money(request.DriverShare),
                new Money(request.PartnerShare),
                new Money(request.CompanyShare),
                request.DriverId,
                request.PartnerId,
                request.CompanyId
            );

            var result = await mediator.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetOrderTransaction),
                new { id = result.TransactionId },
                new CreateOrderResponse(result.TransactionId, "JET"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("eat")]
    public async Task<ActionResult<CreateOrderResponse>> CreateEatOrder(
        [FromBody] CreateEatOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateTransactionCommand(
                request.CustomerId,
                request.OrderId,
                new Money(request.TotalAmount),
                new Money(request.DriverShare),
                new Money(request.RestaurantShare),
                new Money(request.CompanyShare),
                request.DriverId,
                request.RestaurantId,
                request.CompanyId
            );

            var result = await mediator.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetOrderTransaction),
                new { id = result.TransactionId },
                new CreateOrderResponse(result.TransactionId, "EAT"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("vendor")]
    public async Task<ActionResult<CreateOrderResponse>> CreateVendorOrder(
        [FromBody] CreateVendorOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateTransactionCommand(
                request.CustomerId,
                request.OrderId,
                new Money(request.TotalAmount),
                new Money(request.DriverShare),
                new Money(request.VendorShare),
                new Money(request.CompanyShare),
                request.DriverId,
                request.VendorId,
                request.CompanyId
            );

            var result = await mediator.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetOrderTransaction),
                new { id = result.TransactionId },
                new CreateOrderResponse(result.TransactionId, "VENDOR"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{orderId}/cancel")]
    public async Task<ActionResult> CancelOrder(
        long orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create reversal transaction
            var command = new CreateTransactionCommand(
                request.CompanyId, // Company pays back
                orderId,
                new Money(request.RefundAmount),
                Money.Zero, // No driver share in refund
                Money.Zero, // No partner share in refund
                new Money(-request.RefundAmount), // Company debit
                request.CompanyId,
                request.CompanyId,
                request.CompanyId
            );

            var result = await mediator.Send(command, cancellationToken);

            return Ok(new { transactionId = result.TransactionId, message = "Order cancelled and refunded" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{orderId}/return")]
    public async Task<ActionResult> ReturnOrder(
        long orderId,
        [FromBody] ReturnOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Similar to cancel but may have different business logic
            var command = new CreateTransactionCommand(
                request.CompanyId,
                orderId,
                new Money(request.ReturnAmount),
                Money.Zero,
                Money.Zero,
                new Money(-request.ReturnAmount),
                request.CompanyId,
                request.CompanyId,
                request.CompanyId
            );

            var result = await mediator.Send(command, cancellationToken);

            return Ok(new { transactionId = result.TransactionId, message = "Order returned and refunded" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("transactions/{id}")]
    public Task<ActionResult> GetOrderTransaction(long id)
    {
        // This would be implemented with a query handler
        return Task.FromResult<ActionResult>(Ok(new { transactionId = id, message = "Transaction details not implemented yet" }));
    }
}

public record CreateJetOrderRequest(
    long CustomerId,
    long OrderId,
    decimal TotalAmount,
    decimal DriverShare,
    decimal PartnerShare,
    decimal CompanyShare,
    long DriverId,
    long PartnerId,
    long CompanyId
);

public record CreateEatOrderRequest(
    long CustomerId,
    long OrderId,
    decimal TotalAmount,
    decimal DriverShare,
    decimal RestaurantShare,
    decimal CompanyShare,
    long DriverId,
    long RestaurantId,
    long CompanyId
);

public record CreateVendorOrderRequest(
    long CustomerId,
    long OrderId,
    decimal TotalAmount,
    decimal DriverShare,
    decimal VendorShare,
    decimal CompanyShare,
    long DriverId,
    long VendorId,
    long CompanyId
);

public record CreateOrderResponse(long TransactionId, string OrderType);

public record CancelOrderRequest(
    decimal RefundAmount,
    long CompanyId
);

public record ReturnOrderRequest(
    decimal ReturnAmount,
    long CompanyId
);