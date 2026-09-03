using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Trellochocero.Api.DTOs;

namespace Trellochocero.Tests.Integration;

public class BoardHubIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public BoardHubIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"hub-tester-{Guid.NewGuid():N}@test.com";
        var reg = new RegisterRequest(email, "Hub Tester", "Password123");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    [Fact]
    public async Task Negotiate_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.PostAsync("/hubs/board/negotiate?negotiateVersion=1", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negotiate_WithAccessTokenQueryParam_Returns200OK()
    {
        var token = await _RegisterAndGetTokenAsync();

        var response = await _client.PostAsync($"/hubs/board/negotiate?negotiateVersion=1&access_token={token}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("connectionId");
    }

    [Fact]
    public async Task Negotiate_WithBearerHeader_Returns200OK()
    {
        var token = await _RegisterAndGetTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/board/negotiate?negotiateVersion=1");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("connectionId");
    }

    private Task<string> _RegisterAndGetTokenAsync() => RegisterAndGetTokenAsync();
}
