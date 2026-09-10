namespace Trellochocero.Api.DTOs;

// Requests
public record RegisterRequest(string Email, string FullName, string Password);
public record LoginRequest(string Email, string Password);
public record GoogleAuthRequest(string IdToken);
public record GitHubAuthRequest(string Code, string? RedirectUri = null);

// Responses
public record AuthResponse(Guid Id, string Email, string FullName, string Token, string? AvatarUrl);
public record UserProfileResponse(Guid Id, string Email, string FullName, string? AvatarUrl, DateTime CreatedAt);
public record OAuthConfigResponse(
    bool GoogleConfigured,
    string? GoogleClientId,
    bool GitHubConfigured,
    string? GitHubClientId
);
