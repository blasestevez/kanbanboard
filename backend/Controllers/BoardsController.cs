using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Services;

namespace Trellochocero.Api.Controllers;

[Authorize]
[ApiController]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardsController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/boards")]
    [ProducesResponseType(typeof(List<BoardSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspaceBoards(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _boardService.GetWorkspaceBoardsAsync(workspaceId, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return Ok(result.Value);
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/boards")]
    [ProducesResponseType(typeof(BoardSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBoard(Guid workspaceId, [FromBody] CreateBoardRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _boardService.CreateBoardAsync(workspaceId, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("api/boards/{id:guid}")]
    [ProducesResponseType(typeof(BoardDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoardById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _boardService.GetBoardByIdAsync(id, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return Ok(result.Value);
    }

    [HttpPut("api/boards/{id:guid}")]
    [ProducesResponseType(typeof(BoardDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBoard(Guid id, [FromBody] UpdateBoardRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _boardService.UpdateBoardAsync(id, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return Ok(result.Value);
    }

    [HttpDelete("api/boards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBoard(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _boardService.DeleteBoardAsync(id, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return NoContent();
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
