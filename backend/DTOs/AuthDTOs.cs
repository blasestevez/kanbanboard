namespace Trellochocero.Api.DTOs;

// Requests
public record RegisterRequest(string Email, string FullName, string Password);
public record LoginRequest(string Email, string Password);

// Responses
public record AuthResponse(Guid Id, string Email, string FullName, string Token, string? AvatarUrl);
public record UserProfileResponse(Guid Id, string Email, string FullName, string? AvatarUrl, DateTime CreatedAt);
