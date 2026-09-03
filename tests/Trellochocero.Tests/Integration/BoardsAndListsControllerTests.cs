using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Trellochocero.Api.DTOs;

namespace Trellochocero.Tests.Integration;

public class BoardsAndListsControllerTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public BoardsAndListsControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid WorkspaceId)> SetupUserAndWorkspaceAsync()
    {
        var email = $"board-user-{Guid.NewGuid():N}@test.com";
        var reg = new RegisterRequest(email, "Board Creator", "Password123");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var wsRes = await _client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest("Board Test WS", "Desc", null));
        var ws = await wsRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        return (auth.Token, ws!.Id);
    }

    [Fact]
    public async Task CreateBoard_WithValidData_Returns201AndAppearsInList()
    {
        var (_, workspaceId) = await SetupUserAndWorkspaceAsync();

        var req = new CreateBoardRequest("Project Kanban", "#0079bf", null);
        var res = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/boards", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var board = await res.Content.ReadFromJsonAsync<BoardSummaryResponse>();
        board.Should().NotBeNull();
        board!.Title.Should().Be("Project Kanban");
        board.BackgroundColor.Should().Be("#0079bf");
        board.WorkspaceId.Should().Be(workspaceId);

        // List boards in workspace
        var listRes = await _client.GetAsync($"/api/workspaces/{workspaceId}/boards");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var boards = await listRes.Content.ReadFromJsonAsync<List<BoardSummaryResponse>>();
        boards.Should().ContainSingle(b => b.Id == board.Id);
    }

    [Fact]
    public async Task GetBoardById_ReturnsDetailWithLists()
    {
        var (_, workspaceId) = await SetupUserAndWorkspaceAsync();

        var boardRes = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/boards", new CreateBoardRequest("Sprint Board", null, null));
        var board = await boardRes.Content.ReadFromJsonAsync<BoardSummaryResponse>();

        // Create lists
        await _client.PostAsJsonAsync($"/api/boards/{board!.Id}/lists", new CreateListRequest("To Do"));
        await _client.PostAsJsonAsync($"/api/boards/{board.Id}/lists", new CreateListRequest("In Progress"));

        var detailRes = await _client.GetAsync($"/api/boards/{board.Id}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailRes.Content.ReadFromJsonAsync<BoardDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Title.Should().Be("Sprint Board");
        detail.Lists.Should().HaveCount(2);
        detail.Lists[0].Title.Should().Be("To Do");
        detail.Lists[1].Title.Should().Be("In Progress");
    }

    [Fact]
    public async Task ReorderLists_UpdatesPositionsCorrectly()
    {
        var (_, workspaceId) = await SetupUserAndWorkspaceAsync();

        var boardRes = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/boards", new CreateBoardRequest("Reorder Board", null, null));
        var board = await boardRes.Content.ReadFromJsonAsync<BoardSummaryResponse>();

        var l1Res = await _client.PostAsJsonAsync($"/api/boards/{board!.Id}/lists", new CreateListRequest("Col 1"));
        var l1 = await l1Res.Content.ReadFromJsonAsync<BoardListResponse>();

        var l2Res = await _client.PostAsJsonAsync($"/api/boards/{board.Id}/lists", new CreateListRequest("Col 2"));
        var l2 = await l2Res.Content.ReadFromJsonAsync<BoardListResponse>();

        // Reorder Col 2 before Col 1
        var reorderReq = new ReorderListsRequest(new List<Guid> { l2!.Id, l1!.Id });
        var reorderRes = await _client.PutAsJsonAsync($"/api/boards/{board.Id}/lists/reorder", reorderReq);
        reorderRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Fetch board detail to verify new order
        var detailRes = await _client.GetAsync($"/api/boards/{board.Id}");
        var detail = await detailRes.Content.ReadFromJsonAsync<BoardDetailResponse>();
        detail!.Lists[0].Id.Should().Be(l2.Id);
        detail.Lists[1].Id.Should().Be(l1.Id);
    }

    [Fact]
    public async Task DeleteList_RemovesListFromBoard()
    {
        var (_, workspaceId) = await SetupUserAndWorkspaceAsync();

        var boardRes = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/boards", new CreateBoardRequest("List Delete Board", null, null));
        var board = await boardRes.Content.ReadFromJsonAsync<BoardSummaryResponse>();

        var listRes = await _client.PostAsJsonAsync($"/api/boards/{board!.Id}/lists", new CreateListRequest("Temporary List"));
        var list = await listRes.Content.ReadFromJsonAsync<BoardListResponse>();

        var delRes = await _client.DeleteAsync($"/api/lists/{list!.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailRes = await _client.GetAsync($"/api/boards/{board.Id}");
        var detail = await detailRes.Content.ReadFromJsonAsync<BoardDetailResponse>();
        detail!.Lists.Should().BeEmpty();
    }
}
