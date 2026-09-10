using Trellochocero.Api.DTOs;

namespace Trellochocero.Api.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> GoogleAuthAsync(GoogleAuthRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> GitHubAuthAsync(GitHubAuthRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    OAuthConfigResponse GetOAuthConfig();
}
