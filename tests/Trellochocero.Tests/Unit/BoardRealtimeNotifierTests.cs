using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Hubs;
using Trellochocero.Api.Services;

namespace Trellochocero.Tests.Unit;

public class BoardRealtimeNotifierTests
{
    private readonly Mock<IHubContext<BoardHub>> _hubContextMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<ILogger<BoardRealtimeNotifier>> _loggerMock;
    private readonly BoardRealtimeNotifier _notifier;

    public BoardRealtimeNotifierTests()
    {
        _hubContextMock = new Mock<IHubContext<BoardHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _loggerMock = new Mock<ILogger<BoardRealtimeNotifier>>();

        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _notifier = new BoardRealtimeNotifier(_hubContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyBoardUpdatedAsync_SendsBoardUpdatedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var payload = new BoardUpdatedPayload("New Title", "#123456", null, false);

        await _notifier.NotifyBoardUpdatedAsync(boardId, payload);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "BoardUpdated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], payload)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyListCreatedAsync_SendsListCreatedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var listDto = new BoardListResponse(Guid.NewGuid(), boardId, "List 1", 0, false, new List<CardSummaryResponse>());

        await _notifier.NotifyListCreatedAsync(boardId, listDto);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "ListCreated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], listDto)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyListUpdatedAsync_SendsListUpdatedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var listDto = new BoardListResponse(Guid.NewGuid(), boardId, "Updated List", 0, false, new List<CardSummaryResponse>());

        await _notifier.NotifyListUpdatedAsync(boardId, listDto);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "ListUpdated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], listDto)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyListDeletedAsync_SendsListDeletedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        await _notifier.NotifyListDeletedAsync(boardId, listId);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "ListDeleted",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], listId)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyListsReorderedAsync_SendsListsReorderedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var listIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        await _notifier.NotifyListsReorderedAsync(boardId, listIds);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "ListsReordered",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], listIds)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyCardCreatedAsync_SendsCardCreatedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var cardDto = new CardDetailResponse(
            Guid.NewGuid(), Guid.NewGuid(), boardId, "Card 1", null, 0, null, false, null, null,
            DateTime.UtcNow, [], [], [], [], []);

        await _notifier.NotifyCardCreatedAsync(boardId, cardDto);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "CardCreated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], cardDto)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyCardUpdatedAsync_SendsCardUpdatedToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var cardDto = new CardDetailResponse(
            Guid.NewGuid(), Guid.NewGuid(), boardId, "Card Updated", null, 0, null, true, null, null,
            DateTime.UtcNow, [], [], [], [], []);

        await _notifier.NotifyCardUpdatedAsync(boardId, cardDto);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "CardUpdated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], cardDto)),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyCardMovedAsync_SendsCardMovedPayloadToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var sourceListId = Guid.NewGuid();
        var targetListId = Guid.NewGuid();
        var newPosition = 2;

        await _notifier.NotifyCardMovedAsync(boardId, cardId, sourceListId, targetListId, newPosition);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "CardMoved",
            It.Is<object?[]>(args =>
                args.Length == 1 &&
                args[0] is CardMovedPayload &&
                ((CardMovedPayload)args[0]!).CardId == cardId &&
                ((CardMovedPayload)args[0]!).SourceListId == sourceListId &&
                ((CardMovedPayload)args[0]!).TargetListId == targetListId &&
                ((CardMovedPayload)args[0]!).NewPosition == newPosition),
            default), Times.Once);
    }

    [Fact]
    public async Task NotifyCardDeletedAsync_SendsCardDeletedPayloadToBoardGroup()
    {
        var boardId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        await _notifier.NotifyCardDeletedAsync(boardId, cardId, listId);

        _hubClientsMock.Verify(c => c.Group($"board-{boardId}"), Times.Once);
        _clientProxyMock.Verify(p => p.SendCoreAsync(
            "CardDeleted",
            It.Is<object?[]>(args =>
                args.Length == 1 &&
                args[0] is CardDeletedPayload &&
                ((CardDeletedPayload)args[0]!).CardId == cardId &&
                ((CardDeletedPayload)args[0]!).ListId == listId),
            default), Times.Once);
    }

    [Fact]
    public async Task Notifier_WhenExceptionThrownByHub_CatchesAndLogsWithoutThrowing()
    {
        var boardId = Guid.NewGuid();
        _clientProxyMock
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .ThrowsAsync(new InvalidOperationException("SignalR failure"));

        var act = async () => await _notifier.NotifyBoardUpdatedAsync(
            boardId,
            new BoardUpdatedPayload("Title", null, null, false));

        await act.Should().NotThrowAsync();
    }
}
