namespace Trellochocero.Api.DTOs;

// Boards DTOs
public record CreateBoardRequest(string Title, string? BackgroundColor, string? BackgroundImageUrl);
public record UpdateBoardRequest(string Title, string? BackgroundColor, string? BackgroundImageUrl, bool IsClosed);

public record BoardSummaryResponse(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string? BackgroundColor,
    string? BackgroundImageUrl,
    bool IsClosed,
    int Position,
    int ListsCount,
    DateTime CreatedAt);

public record BoardDetailResponse(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    string Title,
    string? BackgroundColor,
    string? BackgroundImageUrl,
    bool IsClosed,
    string CurrentUserRole,
    List<BoardListResponse> Lists);

// Lists DTOs
public record CreateListRequest(string Title);
public record UpdateListRequest(string Title, bool IsArchived);
public record ReorderListsRequest(List<Guid> ListIds);

public record BoardListResponse(
    Guid Id,
    Guid BoardId,
    string Title,
    int Position,
    bool IsArchived,
    List<CardSummaryResponse> Cards);

public record CardSummaryResponse(
    Guid Id,
    Guid ListId,
    string Title,
    string? Description,
    int Position,
    DateTime? DueDate,
    bool IsComplete,
    string? CoverColor,
    string? CoverImageUrl,
    int CommentsCount,
    int ChecklistItemsTotal,
    int ChecklistItemsChecked);

// Real-time Event Payloads
public record BoardUpdatedPayload(
    string Title,
    string? BackgroundColor,
    string? BackgroundImageUrl,
    bool IsClosed);

public record CardMovedPayload(
    Guid CardId,
    Guid SourceListId,
    Guid TargetListId,
    int NewPosition);

public record CardDeletedPayload(
    Guid CardId,
    Guid ListId);
