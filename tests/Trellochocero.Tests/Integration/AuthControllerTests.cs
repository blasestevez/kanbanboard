using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Trellochocero.Api.Data;
using Trellochocero.Api.DTOs;

namespace Trellochocero.Tests.Integration;

public class AuthControllerTests : IClassFixture<AuthControllerTests.TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public AuthControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ========== Registration Tests ==========

    [Fact]
    public async Task Register_WithValidData_Returns201WithToken()
    {
        // Arrange
        var request = new RegisterRequest(
            $"register-valid-{Guid.NewGuid():N}@test.com",
            "Test User",
            "Password1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Email.Should().Be(request.Email);
        auth.FullName.Should().Be(request.FullName);
        auth.Token.Should().NotBeNullOrEmpty();
        auth.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest(email, "First User", "Password1");

        // First registration should succeed
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - second registration with same email
        var secondRequest = new RegisterRequest(email, "Second User", "Password1");
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", secondRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400()
    {
        // Arrange - empty body
        var request = new { email = "", fullName = "", password = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ========== Login Tests ==========

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        // Arrange - register first
        var email = $"login-valid-{Guid.NewGuid():N}@test.com";
        var password = "Password1";
        var registerRequest = new RegisterRequest(email, "Login User", password);
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act
        var loginRequest = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Email.Should().Be(email);
        auth.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@test.com", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ========== Users/Me Tests ==========

    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithProfile()
    {
        // Arrange - register to get a token
        var email = $"me-valid-{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterRequest(email, "Profile User", "Password1");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(email);
        profile.FullName.Should().Be("Profile User");
        profile.Id.Should().Be(auth.Id);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithInvalidToken_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ========== OAuth Tests ==========

    [Fact]
    public async Task GetOAuthConfig_Returns200WithConfiguration()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/oauth-config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<OAuthConfigResponse>();
        config.Should().NotBeNull();
    }

    [Fact]
    public async Task GoogleAuth_WithDemoToken_Returns200WithToken()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google", new GoogleAuthRequest("demo-google"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Email.Should().Be("google.demo@kanbanboard.dev");
        auth.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GitHubAuth_WithDemoCode_Returns200WithToken()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/github", new GitHubAuthRequest("demo-github"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Email.Should().Be("github.demo@kanbanboard.dev");
        auth.Token.Should().NotBeNullOrWhiteSpace();
    }

    // ========== Test Factory ==========

    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // Provide a dummy connection string so AddDatabase() doesn't throw
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Host=localhost;Database=test_dummy" }
                });
            });
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL database/EF-related registrations from the real app
                var descriptorsToRemove = services
                    .Where(d =>
                    {
                        var typeName = d.ServiceType.FullName ?? string.Empty;
                        var implName = d.ImplementationType?.FullName ?? string.Empty;
                        return typeName.Contains("DbContextOptions") ||
                               typeName.Contains("Npgsql") ||
                               implName.Contains("Npgsql");
                    })
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                    services.Remove(descriptor);

                // Re-add with InMemory provider
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        }
    }


}
