using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Trellochocero.Api.Hubs;

namespace Trellochocero.Tests.Unit;

public class BoardHubTests
{
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly Mock<HubCallerContext> _callerContextMock;
    private readonly Mock<ILogger<BoardHub>> _loggerMock;
    private readonly BoardHub _hub;

    public BoardHubTests()
    {
        _groupManagerMock = new Mock<IGroupManager>();
        _callerContextMock = new Mock<HubCallerContext>();
        _loggerMock = new Mock<ILogger<BoardHub>>();

        _callerContextMock.Setup(c => c.ConnectionId).Returns("conn-12345");

        _hub = new BoardHub(_loggerMock.Object)
        {
            Groups = _groupManagerMock.Object,
            Context = _callerContextMock.Object
        };
    }

    [Fact]
    public async Task JoinBoard_AddsConnectionToBoardGroup()
    {
        var boardId = Guid.NewGuid().ToString();

        await _hub.JoinBoard(boardId);

        _groupManagerMock.Verify(g => g.AddToGroupAsync("conn-12345", $"board-{boardId}", default), Times.Once);
    }

    [Fact]
    public async Task LeaveBoard_RemovesConnectionFromBoardGroup()
    {
        var boardId = Guid.NewGuid().ToString();

        await _hub.LeaveBoard(boardId);

        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync("conn-12345", $"board-{boardId}", default), Times.Once);
    }
}
