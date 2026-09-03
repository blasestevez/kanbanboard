using Microsoft.AspNetCore.Http;
using Trellochocero.Api.DTOs;

namespace Trellochocero.Api.Services;

public interface ICardService
{
    // Card Core
    Task<Result<CardDetailResponse>> CreateCardAsync(Guid listId, CreateCardRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<CardDetailResponse>> GetCardByIdAsync(Guid cardId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<CardDetailResponse>> UpdateCardAsync(Guid cardId, UpdateCardRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> MoveCardAsync(Guid cardId, MoveCardRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteCardAsync(Guid cardId, Guid currentUserId, CancellationToken cancellationToken = default);

    // Labels
    Task<Result<List<LabelResponse>>> GetBoardLabelsAsync(Guid boardId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<LabelResponse>> CreateBoardLabelAsync(Guid boardId, CreateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> AddLabelToCardAsync(Guid cardId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveLabelFromCardAsync(Guid cardId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default);

    // Checklists & Items
    Task<Result<ChecklistResponse>> CreateChecklistAsync(Guid cardId, CreateChecklistRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteChecklistAsync(Guid checklistId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<ChecklistItemResponse>> CreateChecklistItemAsync(Guid checklistId, CreateChecklistItemRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<ChecklistItemResponse>> UpdateChecklistItemAsync(Guid itemId, UpdateChecklistItemRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteChecklistItemAsync(Guid itemId, Guid currentUserId, CancellationToken cancellationToken = default);

    // Comments
    Task<Result<CommentResponse>> AddCommentAsync(Guid cardId, CreateCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<CommentResponse>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);

    // Attachments
    Task<Result<AttachmentResponse>> UploadAttachmentAsync(Guid cardId, IFormFile file, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAttachmentAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default);

    // Card Members
    Task<Result<bool>> AddMemberToCardAsync(Guid cardId, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveMemberFromCardAsync(Guid cardId, Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);
}
