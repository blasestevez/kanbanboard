using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Services;

namespace Trellochocero.Api.Controllers;

[Authorize]
[ApiController]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService)
    {
        _cardService = cardService;
    }

    [HttpPost("api/lists/{listId:guid}/cards")]
    [ProducesResponseType(typeof(CardDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCard(Guid listId, [FromBody] CreateCardRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.CreateCardAsync(listId, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("api/cards/{id:guid}")]
    [ProducesResponseType(typeof(CardDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCardById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.GetCardByIdAsync(id, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return Ok(result.Value);
    }

    [HttpPut("api/cards/{id:guid}")]
    [ProducesResponseType(typeof(CardDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCard(Guid id, [FromBody] UpdateCardRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.UpdateCardAsync(id, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return Ok(result.Value);
    }

    [HttpPut("api/cards/{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveCard(Guid id, [FromBody] MoveCardRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.MoveCardAsync(id, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return NoContent();
    }

    [HttpDelete("api/cards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCard(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.DeleteCardAsync(id, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return NoContent();
    }

    [HttpPost("api/cards/{cardId:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddLabelToCard(Guid cardId, Guid labelId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.AddLabelToCardAsync(cardId, labelId, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return NoContent();
    }

    [HttpDelete("api/cards/{cardId:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLabelFromCard(Guid cardId, Guid labelId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.RemoveLabelFromCardAsync(cardId, labelId, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return NoContent();
    }

    [HttpPost("api/cards/{cardId:guid}/checklists")]
    [ProducesResponseType(typeof(ChecklistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChecklist(Guid cardId, [FromBody] CreateChecklistRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.CreateChecklistAsync(cardId, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("api/cards/{cardId:guid}/comments")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(Guid cardId, [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.AddCommentAsync(cardId, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("api/cards/{cardId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAttachment(Guid cardId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.UploadAttachmentAsync(cardId, file, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("api/cards/{cardId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMemberToCard(Guid cardId, Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.AddMemberToCardAsync(cardId, userId, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToProblemDetails(result);
        }

        return NoContent();
    }

    [HttpDelete("api/cards/{cardId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMemberFromCard(Guid cardId, Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        var result = await _cardService.RemoveMemberFromCardAsync(cardId, userId, currentUserId, cancellationToken);
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
