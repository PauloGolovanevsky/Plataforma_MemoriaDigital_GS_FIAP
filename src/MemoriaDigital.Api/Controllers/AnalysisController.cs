using MemoriaDigital.Application.DTOs;
using MemoriaDigital.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MemoriaDigital.Api.Controllers;

[ApiController]
[Route("api/analyses")]
[Produces("application/json")]
public sealed class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await analysisService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await analysisService.GetByIdAsync(id, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await analysisService.GetPagedAsync(page, pageSize, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
