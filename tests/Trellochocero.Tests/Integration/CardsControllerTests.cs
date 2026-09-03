using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Trellochocero.Api.DTOs;

namespace Trellochocero.Tests.Integration;

public class CardsControllerTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public CardsControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid WorkspaceId, Guid BoardId, Guid ListId)> SetupBoardAndListAsync()
    {
        var email = $"card-user-{Guid.NewGuid():N}@test.com";
        var reg = new RegisterRequest(email, "Card Tester", "Password123");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var wsRes = await _client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest("Card WS", null, null));
        var ws = await wsRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        var boardRes = await _client.PostAsJsonAsync($"/api/workspaces/{ws!.Id}/boards", new CreateBoardRequest("Card Board", "#0079bf", null));
        var board = await boardRes.Content.ReadFromJsonAsync<BoardSummaryResponse>();

        var listRes = await _client.PostAsJsonAsync($"/api/boards/{board!.Id}/lists", new CreateListRequest("Sprint Backlog"));
        var list = await listRes.Content.ReadFromJsonAsync<BoardListResponse>();

        return (auth.Token, ws.Id, board.Id, list!.Id);
    }

    [Fact]
    public async Task CreateCard_Returns201AndCardDetail()
    {
        var (_, _, _, listId) = await SetupBoardAndListAsync();

        var req = new CreateCardRequest("Implementar JWT", "Detalle de JWT", "#0079bf");
        var res = await _client.PostAsJsonAsync($"/api/lists/{listId}/cards", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var card = await res.Content.ReadFromJsonAsync<CardDetailResponse>();
        card.Should().NotBeNull();
        card!.Title.Should().Be("Implementar JWT");
        card.Description.Should().Be("Detalle de JWT");
        card.CoverColor.Should().Be("#0079bf");
        card.ListId.Should().Be(listId);
    }

    [Fact]
    public async Task MoveCard_BetweenLists_UpdatesPositionAndListId()
    {
        var (_, _, boardId, listId1) = await SetupBoardAndListAsync();

        // Create second list
        var list2Res = await _client.PostAsJsonAsync($"/api/boards/{boardId}/lists", new CreateListRequest("In Progress"));
        var list2 = await list2Res.Content.ReadFromJsonAsync<BoardListResponse>();

        // Create card in list 1
        var cardRes = await _client.PostAsJsonAsync($"/api/lists/{listId1}/cards", new CreateCardRequest("Movable Card", null, null));
        var card = await cardRes.Content.ReadFromJsonAsync<CardDetailResponse>();

        // Move card to list 2 at position 0
        var moveReq = new MoveCardRequest(list2!.Id, 0);
        var moveRes = await _client.PutAsJsonAsync($"/api/cards/{card!.Id}/move", moveReq);
        moveRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify card is now in list 2
        var getRes = await _client.GetAsync($"/api/cards/{card.Id}");
        var movedCard = await getRes.Content.ReadFromJsonAsync<CardDetailResponse>();
        movedCard!.ListId.Should().Be(list2.Id);
        movedCard.Position.Should().Be(0);
    }

    [Fact]
    public async Task ChecklistsAndItems_CreateAndToggle_WorksSuccessfully()
    {
        var (_, _, _, listId) = await SetupBoardAndListAsync();

        var cardRes = await _client.PostAsJsonAsync($"/api/lists/{listId}/cards", new CreateCardRequest("Checklist Card", null, null));
        var card = await cardRes.Content.ReadFromJsonAsync<CardDetailResponse>();

        // Add checklist
        var chkRes = await _client.PostAsJsonAsync($"/api/cards/{card!.Id}/checklists", new CreateChecklistRequest("QA Checks"));
        chkRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var checklist = await chkRes.Content.ReadFromJsonAsync<ChecklistResponse>();
        checklist.Should().NotBeNull();
        checklist!.Title.Should().Be("QA Checks");

        // Add checklist item
        var itemRes = await _client.PostAsJsonAsync($"/api/checklists/{checklist.Id}/items", new CreateChecklistItemRequest("Run unit tests"));
        itemRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await itemRes.Content.ReadFromJsonAsync<ChecklistItemResponse>();
        item!.Text.Should().Be("Run unit tests");
        item.IsChecked.Should().BeFalse();

        // Toggle item checked
        var toggleRes = await _client.PutAsJsonAsync($"/api/checklist-items/{item.Id}", new UpdateChecklistItemRequest("Run unit tests", true));
        toggleRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedItem = await toggleRes.Content.ReadFromJsonAsync<ChecklistItemResponse>();
        updatedItem!.IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task Comments_AddAndRetrieve_WorksSuccessfully()
    {
        var (_, _, _, listId) = await SetupBoardAndListAsync();

        var cardRes = await _client.PostAsJsonAsync($"/api/lists/{listId}/cards", new CreateCardRequest("Comment Card", null, null));
        var card = await cardRes.Content.ReadFromJsonAsync<CardDetailResponse>();

        // Add comment
        var commentReq = new CreateCommentRequest("Este es un comentario importante.");
        var commentRes = await _client.PostAsJsonAsync($"/api/cards/{card!.Id}/comments", commentReq);
        commentRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var comment = await commentRes.Content.ReadFromJsonAsync<CommentResponse>();
        comment.Should().NotBeNull();
        comment!.Text.Should().Be("Este es un comentario importante.");

        // Verify appears in card detail
        var getRes = await _client.GetAsync($"/api/cards/{card.Id}");
        var detail = await getRes.Content.ReadFromJsonAsync<CardDetailResponse>();
        detail!.Comments.Should().ContainSingle(c => c.Id == comment.Id);
    }
}
