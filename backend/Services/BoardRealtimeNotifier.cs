using Microsoft.AspNetCore.SignalR;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Hubs;

namespace Trellochocero.Api.Services;

public class BoardRealtimeNotifier : IBoardRealtimeNotifier
{
    private readonly IHubContext<BoardHub> _hubContext;
    private readonly ILogger<BoardRealtimeNotifier> _logger;

    public BoardRealtimeNotifier(IHubContext<BoardHub> hubContext, ILogger<BoardRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    private static string GetGroupName(Guid boardId) => $"board-{boardId}";

    public async Task NotifyBoardUpdatedAsync(Guid boardId, BoardUpdatedPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("BoardUpdated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast BoardUpdated for board {BoardId}", boardId);
        }
    }

    public async Task NotifyListCreatedAsync(Guid boardId, BoardListResponse list, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("ListCreated", list, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ListCreated for board {BoardId}, list {ListId}", boardId, list.Id);
        }
    }

    public async Task NotifyListUpdatedAsync(Guid boardId, BoardListResponse list, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("ListUpdated", list, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ListUpdated for board {BoardId}, list {ListId}", boardId, list.Id);
        }
    }

    public async Task NotifyListDeletedAsync(Guid boardId, Guid listId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("ListDeleted", listId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ListDeleted for board {BoardId}, list {ListId}", boardId, listId);
        }
    }

    public async Task NotifyListsReorderedAsync(Guid boardId, List<Guid> listIds, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("ListsReordered", listIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ListsReordered for board {BoardId}", boardId);
        }
    }

    public async Task NotifyCardCreatedAsync(Guid boardId, CardDetailResponse card, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("CardCreated", card, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast CardCreated for board {BoardId}, card {CardId}", boardId, card.Id);
        }
    }

    public async Task NotifyCardUpdatedAsync(Guid boardId, CardDetailResponse card, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("CardUpdated", card, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast CardUpdated for board {BoardId}, card {CardId}", boardId, card.Id);
        }
    }

    public async Task NotifyCardMovedAsync(Guid boardId, Guid cardId, Guid sourceListId, Guid targetListId, int newPosition, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new CardMovedPayload(cardId, sourceListId, targetListId, newPosition);
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("CardMoved", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast CardMoved for board {BoardId}, card {CardId}", boardId, cardId);
        }
    }

    public async Task NotifyCardDeletedAsync(Guid boardId, Guid cardId, Guid listId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new CardDeletedPayload(cardId, listId);
            await _hubContext.Clients.Group(GetGroupName(boardId))
                .SendAsync("CardDeleted", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast CardDeleted for board {BoardId}, card {CardId}", boardId, cardId);
        }
    }
}
