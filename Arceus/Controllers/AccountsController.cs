using Arceus.Application.Features.Accounts.Commands.ChargeWallet;
using Arceus.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("charge")]
    public async Task<ActionResult<ChargeWalletResult>> ChargeWallet(
        [FromBody] ChargeWalletRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ChargeWalletCommand(
                request.CustomerId,
                new Money(request.Amount),
                request.PaymentToken
                // request.CompanyId
            );

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
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

    [HttpGet("{customerId}/balance")]
    public async Task<ActionResult> GetAccountBalance(long customerId)
    {
        // This would be implemented with a query handler
        return Ok(new { customerId, message = "Balance retrieval not implemented yet" });
    }
}

public record ChargeWalletRequest(
    long CustomerId,
    decimal Amount,
    string PaymentToken
    // long CompanyId
);