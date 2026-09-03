namespace Trellochocero.Api.DTOs;

public record CreateCardRequest(string Title, string? Description, string? CoverColor);
public record UpdateCardRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    bool IsComplete,
    string? CoverColor,
    string? CoverImageUrl);

public record MoveCardRequest(Guid TargetListId, int NewPosition);

public record CardDetailResponse(
    Guid Id,
    Guid ListId,
    Guid BoardId,
    string Title,
    string? Description,
    int Position,
    DateTime? DueDate,
    bool IsComplete,
    string? CoverColor,
    string? CoverImageUrl,
    DateTime CreatedAt,
    List<CardMemberResponse> Members,
    List<LabelResponse> Labels,
    List<ChecklistResponse> Checklists,
    List<CommentResponse> Comments,
    List<AttachmentResponse> Attachments);

public record CardMemberResponse(Guid UserId, string FullName, string Email, string? AvatarUrl);
public record LabelResponse(Guid Id, string Name, string Color);
public record ChecklistResponse(Guid Id, string Title, int Position, List<ChecklistItemResponse> Items);
public record ChecklistItemResponse(Guid Id, string Text, bool IsChecked, int Position);
public record CommentResponse(Guid Id, Guid AuthorId, string AuthorName, string? AuthorAvatarUrl, string Text, DateTime CreatedAt, DateTime? UpdatedAt);
public record AttachmentResponse(Guid Id, string FileName, string FileUrl, string ContentType, long FileSizeBytes, DateTime CreatedAt);

public record CreateLabelRequest(string Name, string Color);
public record CreateChecklistRequest(string Title);
public record CreateChecklistItemRequest(string Text);
public record UpdateChecklistItemRequest(string Text, bool IsChecked);
public record CreateCommentRequest(string Text);
public record UpdateCommentRequest(string Text);
