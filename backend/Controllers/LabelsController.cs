using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Services;

namespace Trellochocero.Api.Controllers;

[Authorize]
[ApiController]
public class LabelsController : ControllerBase
{
    private readonly ICardService _cardService;

    public LabelsController(ICardService cardService)
    {
        _cardService = cardService;
    }

    [HttpGet("api/boards/{boardId:guid}/labels")]
    [ProducesResponseType(typeof(List<LabelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoardLabels(Guid boardId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.GetBoardLabelsAsync(boardId, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return Ok(result.Value);
    }

    [HttpPost("api/boards/{boardId:guid}/labels")]
    [ProducesResponseType(typeof(LabelResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBoardLabel(Guid boardId, [FromBody] CreateLabelRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.CreateBoardLabelAsync(boardId, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(claim, out userId);
    }

    private IActionResult UnauthorizedProblem()
    {
        return StatusCode(StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "User is not authenticated.",
            Type = "https://tools.ietf.org/html/rfc7807"
        });
    }

    private IActionResult ToProblemDetails<T>(Result<T> result)
    {
        var problemDetails = new ProblemDetails
        {
            Status = result.StatusCode,
            Title = result.Error ?? "An error occurred.",
            Type = "https://tools.ietf.org/html/rfc7807"
        };

        if (result.ValidationErrors != null && result.ValidationErrors.Count > 0)
        {
            problemDetails.Extensions["errors"] = result.ValidationErrors;
        }

        return StatusCode(result.StatusCode, problemDetails);
    }
}
