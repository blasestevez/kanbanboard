using Trellochocero.Api.DTOs;

namespace Trellochocero.Api.Services;

public interface IBoardRealtimeNotifier
{
    Task NotifyBoardUpdatedAsync(Guid boardId, BoardUpdatedPayload payload, CancellationToken cancellationToken = default);
    Task NotifyListCreatedAsync(Guid boardId, BoardListResponse list, CancellationToken cancellationToken = default);
    Task NotifyListUpdatedAsync(Guid boardId, BoardListResponse list, CancellationToken cancellationToken = default);
    Task NotifyListDeletedAsync(Guid boardId, Guid listId, CancellationToken cancellationToken = default);
    Task NotifyListsReorderedAsync(Guid boardId, List<Guid> listIds, CancellationToken cancellationToken = default);
    Task NotifyCardCreatedAsync(Guid boardId, CardDetailResponse card, CancellationToken cancellationToken = default);
    Task NotifyCardUpdatedAsync(Guid boardId, CardDetailResponse card, CancellationToken cancellationToken = default);
    Task NotifyCardMovedAsync(Guid boardId, Guid cardId, Guid sourceListId, Guid targetListId, int newPosition, CancellationToken cancellationToken = default);
    Task NotifyCardDeletedAsync(Guid boardId, Guid cardId, Guid listId, CancellationToken cancellationToken = default);
}
