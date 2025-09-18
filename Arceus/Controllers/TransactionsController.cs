using Arceus.Application.Features.Transactions.Commands.CreateTransaction;
using Arceus.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CreateTransactionResult>> CreateOrderTransaction(
        [FromBody] CreateOrderTransactionRequest request,
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

            var result = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetTransaction),
                new { id = result.TransactionId },
                result);
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

    [HttpGet("{id}")]
    public async Task<ActionResult> GetTransaction(long id)
    {
        // This would be implemented with a query handler
        return Ok(new { id, message = "Transaction retrieval not implemented yet" });
    }
}

public record CreateOrderTransactionRequest(
    long CustomerId,
    long? OrderId,
    decimal TotalAmount,
    decimal DriverShare,
    decimal PartnerShare,
    decimal CompanyShare,
    long DriverId,
    long PartnerId,
    long CompanyId
);