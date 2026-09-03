using Trellochocero.Api.DTOs;

namespace Trellochocero.Api.Services;

public interface IListService
{
    Task<Result<BoardListResponse>> CreateListAsync(Guid boardId, CreateListRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<BoardListResponse>> UpdateListAsync(Guid listId, UpdateListRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ReorderListsAsync(Guid boardId, ReorderListsRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteListAsync(Guid listId, Guid currentUserId, CancellationToken cancellationToken = default);
}
