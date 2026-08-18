using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Metals;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Inbound.Controllers;

/// <summary>
/// Driving / Primary Adapter: REST API controller for metal catalog resources.
/// </summary>
[ApiController]
[Route("api/v1/metals")]
public class MetalsController(IGetMetalsUseCase metalsUseCase) : ControllerBase
{
    private readonly IGetMetalsUseCase _metalsUseCase = metalsUseCase ?? throw new ArgumentNullException(nameof(metalsUseCase));

    /// <summary>GET /api/v1/metals/all — all available metals as <see cref="MetalsDto"/>.</summary>
    [HttpGet("all")]
    public async Task<IEnumerable<MetalsDto>> GetAllMetals(CancellationToken cancellationToken)
    {
        return await _metalsUseCase.GetAllAsync(cancellationToken);
    }
}
