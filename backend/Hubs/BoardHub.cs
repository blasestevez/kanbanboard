using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Trellochocero.Api.Hubs;

[Authorize]
public class BoardHub : Hub
{
    private readonly ILogger<BoardHub> _logger;

    public BoardHub(ILogger<BoardHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinBoard(string boardId)
    {
        var groupName = $"board-{boardId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveBoard(string boardId)
    {
        var groupName = $"board-{boardId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left {GroupName}", Context.ConnectionId, groupName);
    }
}
