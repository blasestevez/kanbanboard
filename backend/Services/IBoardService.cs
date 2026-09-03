using Trellochocero.Api.DTOs;

namespace Trellochocero.Api.Services;

public interface IBoardService
{
    Task<Result<List<BoardSummaryResponse>>> GetWorkspaceBoardsAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<BoardDetailResponse>> GetBoardByIdAsync(Guid boardId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<BoardSummaryResponse>> CreateBoardAsync(Guid workspaceId, CreateBoardRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<BoardDetailResponse>> UpdateBoardAsync(Guid boardId, UpdateBoardRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteBoardAsync(Guid boardId, Guid currentUserId, CancellationToken cancellationToken = default);
}
