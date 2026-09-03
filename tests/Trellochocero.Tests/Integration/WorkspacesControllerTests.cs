using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Trellochocero.Api.DTOs;

namespace Trellochocero.Tests.Integration;

public class WorkspacesControllerTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public WorkspacesControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Email)> CreateAuthenticatedUserAsync(string name = "User")
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        var reg = new RegisterRequest(email, name, "Password123");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.Token, auth.Id, auth.Email);
    }

    [Fact]
    public async Task GetWorkspaces_WhenUnauthenticated_Returns401()
    {
        var res = await _client.GetAsync("/api/workspaces");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkspace_WithValidData_Returns201AndSetsOwner()
    {
        var (token, userId, _) = await CreateAuthenticatedUserAsync("Owner1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new CreateWorkspaceRequest("Alpha Workspace", "Description of Alpha", null);
        var res = await _client.PostAsJsonAsync("/api/workspaces", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var ws = await res.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();
        ws.Should().NotBeNull();
        ws!.Name.Should().Be("Alpha Workspace");
        ws.CurrentUserRole.Should().Be("Owner");
        ws.OwnerId.Should().Be(userId);
        ws.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task GetWorkspaceById_AsMember_ReturnsWorkspaceWithMembers()
    {
        var (token, userId, email) = await CreateAuthenticatedUserAsync("Owner2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new CreateWorkspaceRequest("Beta Workspace", "Beta Description", null);
        var createRes = await _client.PostAsJsonAsync("/api/workspaces", req);
        var created = await createRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        var getRes = await _client.GetAsync($"/api/workspaces/{created!.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getRes.Content.ReadFromJsonAsync<WorkspaceDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(created.Id);
        detail.Members.Should().HaveCount(1);
        detail.Members.First().UserId.Should().Be(userId);
        detail.Members.First().Role.Should().Be("Owner");
    }

    [Fact]
    public async Task GetWorkspaceById_AsNonMember_Returns403()
    {
        var (token1, _, _) = await CreateAuthenticatedUserAsync("Owner3");
        var (token2, _, _) = await CreateAuthenticatedUserAsync("Stranger");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var req = new CreateWorkspaceRequest("Private Workspace", "Private", null);
        var createRes = await _client.PostAsJsonAsync("/api/workspaces", req);
        var created = await createRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var getRes = await _client.GetAsync($"/api/workspaces/{created!.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddMember_AsOwner_AddsMemberSuccessfully()
    {
        var (ownerToken, _, _) = await CreateAuthenticatedUserAsync("Owner4");
        var (_, _, memberEmail) = await CreateAuthenticatedUserAsync("Colleague1");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var wsRes = await _client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest("Team WS", null, null));
        var ws = await wsRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        var addMemberReq = new AddWorkspaceMemberRequest(memberEmail, "Member");
        var addRes = await _client.PostAsJsonAsync($"/api/workspaces/{ws!.Id}/members", addMemberReq);
        addRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var member = await addRes.Content.ReadFromJsonAsync<WorkspaceMemberResponse>();
        member.Should().NotBeNull();
        member!.Email.Should().Be(memberEmail);
        member.Role.Should().Be("Member");

        // Verify member can now view the workspace
        var detailRes = await _client.GetAsync($"/api/workspaces/{ws.Id}");
        var detail = await detailRes.Content.ReadFromJsonAsync<WorkspaceDetailResponse>();
        detail!.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateWorkspace_AsOwner_UpdatesData()
    {
        var (token, _, _) = await CreateAuthenticatedUserAsync("Owner5");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var wsRes = await _client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest("Old Name", "Old Desc", null));
        var ws = await wsRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        var updateReq = new UpdateWorkspaceRequest("New Name", "New Desc", "http://logo.png");
        var putRes = await _client.PutAsJsonAsync($"/api/workspaces/{ws!.Id}", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await putRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task DeleteWorkspace_AsOwner_RemovesWorkspace()
    {
        var (token, _, _) = await CreateAuthenticatedUserAsync("Owner6");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var wsRes = await _client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest("To Delete", null, null));
        var ws = await wsRes.Content.ReadFromJsonAsync<WorkspaceSummaryResponse>();

        var delRes = await _client.DeleteAsync($"/api/workspaces/{ws!.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await _client.GetAsync($"/api/workspaces/{ws.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
