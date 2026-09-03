using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Models;
using Trellochocero.Api.Services;

namespace Trellochocero.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "SuperSecretKeyForTrellochoceroJwtAuthentication2026!MustBeAtLeast32BytesLong" },
            { "Jwt:Issuer", "TrellochoceroApi" },
            { "Jwt:Audience", "TrellochoceroClient" },
            { "Jwt:ExpirationInMinutes", "1440" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthService(
            _userManagerMock.Object,
            _configuration,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    // ========== RegisterAsync Tests ==========

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsSuccessWithAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "Test User", "Password1");

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
        result.Value.FullName.Should().Be(request.FullName);
        result.Value.Token.Should().NotBeNullOrEmpty();

        // Verify token is valid JWT
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Value.Token);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == request.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest("existing@example.com", "Test User", "Password1");
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FullName = "Existing User"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Email already in use");
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "Test User", "weak");

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 6 characters." }));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ValidationErrors.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyFields_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest("", "", "");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    // ========== LoginAsync Tests ==========

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password1");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password1");

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FullName = "Test User"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyFields_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest("", "");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    // ========== GetUserProfileAsync Tests ==========

    [Fact]
    public async Task GetUserProfileAsync_WithValidId_ReturnsUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            FullName = "Test User",
            AvatarUrl = "https://example.com/avatar.jpg",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(userId);
        result.Value.Email.Should().Be(user.Email);
        result.Value.FullName.Should().Be(user.FullName);
        result.Value.AvatarUrl.Should().Be(user.AvatarUrl);
        result.Value.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.GetUserProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Contain("User not found");
    }
}
