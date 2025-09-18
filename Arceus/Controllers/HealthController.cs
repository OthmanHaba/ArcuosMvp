using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse(
            "Healthy",
            "CoreLedger API is running",
            DateTime.UtcNow,
            Environment.Version.ToString()
        ));
    }

    [HttpGet("endpoints")]
    public ActionResult<EndpointsResponse> GetEndpoints()
    {
        var endpoints = new List<string>
        {
            "GET /api/health",
            "GET /api/health/endpoints",
            "POST /api/transactions",
            "POST /api/accounts/charge",
            "POST /api/integration/customers",
            "POST /api/integration/orders/jet",
            "POST /api/integration/orders/eat",
            "POST /api/integration/orders/vendor",
            "POST /api/integration/wallet/charge",
            "POST /api/integration/drivers",
            "POST /api/integration/vendors",
            "POST /api/integration/restaurants"
        };

        return Ok(new EndpointsResponse(endpoints));
    }
}

public record HealthResponse(
    string Status,
    string Message,
    DateTime Timestamp,
    string Version
);

public record EndpointsResponse(List<string> Endpoints);