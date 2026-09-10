using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Models;

namespace Trellochocero.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure(
                "Invalid registration payload.",
                400,
                new Dictionary<string, string[]>
                {
                    { "General", ["Email, FullName, and Password are required."] }
                });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure(
                "Email already in use.",
                400,
                new Dictionary<string, string[]>
                {
                    { "Email", ["The email address is already registered."] }
                });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return Result<AuthResponse>.Failure("Registration failed.", 400, errors);
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResponse>.Created(new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            token,
            user.AvatarUrl));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure("Email and Password are required.", 400);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            token,
            user.AvatarUrl));
    }

    public async Task<Result<AuthResponse>> GoogleAuthAsync(GoogleAuthRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return Result<AuthResponse>.Failure("Google ID token is required.", 400);
        }

        if (request.IdToken == "demo-google")
        {
            return await HandleDemoGoogleUserAsync(cancellationToken);
        }

        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var validationSettings = new GoogleJsonWebSignature.ValidationSettings();

        if (!string.IsNullOrWhiteSpace(googleClientId) && !googleClientId.StartsWith("your-", StringComparison.OrdinalIgnoreCase))
        {
            validationSettings.Audience = new[] { googleClientId };
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google token validation failed.");
            return Result<AuthResponse>.Failure("Invalid Google ID token.", 400);
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return Result<AuthResponse>.Failure("Google token did not provide an email address.", 400);
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = payload.Email,
                Email = payload.Email,
                FullName = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name : payload.Email,
                AvatarUrl = payload.Picture,
                EmailConfirmed = payload.EmailVerified,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                return Result<AuthResponse>.Failure("Failed to create user from Google account.", 400, errors);
            }
        }
        else if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
        {
            user.AvatarUrl = payload.Picture;
            await _userManager.UpdateAsync(user);
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            token,
            user.AvatarUrl));
    }

    public async Task<Result<AuthResponse>> GitHubAuthAsync(GitHubAuthRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<AuthResponse>.Failure("GitHub authorization code is required.", 400);
        }

        if (request.Code == "demo-github")
        {
            return await HandleDemoGitHubUserAsync(cancellationToken);
        }

        var clientId = _configuration["Authentication:GitHub:ClientId"] ?? string.Empty;
        var clientSecret = _configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("your-", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(clientSecret) || clientSecret.StartsWith("your-", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("GitHub OAuth attempted but credentials are not configured in backend.");
            return Result<AuthResponse>.Failure("GitHub OAuth is not configured on the server. Please check your credentials.", 400);
        }

        var httpClient = _httpClientFactory.CreateClient("GitHubAuth");

        var tokenParameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", request.Code }
        };

        if (!string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            tokenParameters["redirect_uri"] = request.RedirectUri;
        }

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Headers = { { "Accept", "application/json" } },
            Content = new FormUrlEncodedContent(tokenParameters)
        };
        tokenRequest.Headers.UserAgent.ParseAdd("Kanbanboard-App");

        var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return Result<AuthResponse>.Failure("Failed to communicate with GitHub OAuth service.", 400);
        }

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(tokenData?.AccessToken))
        {
            var errorMessage = tokenData?.ErrorDescription ?? "Invalid GitHub authorization code.";
            return Result<AuthResponse>.Failure(errorMessage, 400);
        }

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
        userRequest.Headers.UserAgent.ParseAdd("Trellochocero-App");

        var userResponse = await httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            return Result<AuthResponse>.Failure("Failed to retrieve user profile from GitHub.", 400);
        }

        var gitHubUser = await userResponse.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken: cancellationToken);
        if (gitHubUser == null)
        {
            return Result<AuthResponse>.Failure("Invalid GitHub user response.", 400);
        }

        var email = gitHubUser.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
            emailsRequest.Headers.UserAgent.ParseAdd("Trellochocero-App");

            var emailsResponse = await httpClient.SendAsync(emailsRequest, cancellationToken);
            if (emailsResponse.IsSuccessStatusCode)
            {
                var emails = await emailsResponse.Content.ReadFromJsonAsync<List<GitHubEmailResponse>>(cancellationToken: cancellationToken);
                email = emails?.FirstOrDefault(e => e.Primary && e.Verified)?.Email
                     ?? emails?.FirstOrDefault(e => e.Verified)?.Email
                     ?? emails?.FirstOrDefault()?.Email;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            email = $"{gitHubUser.Login}@users.noreply.github.com";
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = !string.IsNullOrWhiteSpace(gitHubUser.Name) ? gitHubUser.Name : gitHubUser.Login,
                AvatarUrl = gitHubUser.AvatarUrl,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                return Result<AuthResponse>.Failure("Failed to create user from GitHub account.", 400, errors);
            }
        }
        else if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(gitHubUser.AvatarUrl))
        {
            user.AvatarUrl = gitHubUser.AvatarUrl;
            await _userManager.UpdateAsync(user);
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            token,
            user.AvatarUrl));
    }

    public async Task<Result<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<UserProfileResponse>.Failure("User not found.", 404);
        }

        return Result<UserProfileResponse>.Success(new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.AvatarUrl,
            user.CreatedAt));
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "SuperSecretKeyForTrellochoceroJwtAuthentication2026!MustBeAtLeast32BytesLong";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "TrellochoceroApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "TrellochoceroClient";
        var expirationMinutes = _configuration.GetValue<int?>("Jwt:ExpirationInMinutes")
            ?? _configuration.GetValue<int?>("Jwt:DurationInMinutes")
            ?? 1440;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public OAuthConfigResponse GetOAuthConfig()
    {
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var isGoogleConfigured = !string.IsNullOrWhiteSpace(googleClientId) &&
                                 !googleClientId.StartsWith("your-", StringComparison.OrdinalIgnoreCase);

        var gitHubClientId = _configuration["Authentication:GitHub:ClientId"];
        var gitHubClientSecret = _configuration["Authentication:GitHub:ClientSecret"];
        var isGitHubConfigured = !string.IsNullOrWhiteSpace(gitHubClientId) &&
                                 !string.IsNullOrWhiteSpace(gitHubClientSecret) &&
                                 !gitHubClientId.StartsWith("your-", StringComparison.OrdinalIgnoreCase) &&
                                 !gitHubClientSecret.StartsWith("your-", StringComparison.OrdinalIgnoreCase);

        return new OAuthConfigResponse(
            isGoogleConfigured,
            isGoogleConfigured ? googleClientId : null,
            isGitHubConfigured,
            isGitHubConfigured ? gitHubClientId : null
        );
    }

    private async Task<Result<AuthResponse>> HandleDemoGoogleUserAsync(CancellationToken cancellationToken)
    {
        const string email = "google.demo@kanbanboard.dev";
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = "Google Demo User",
                AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=128&auto=format&fit=crop&q=80",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Result<AuthResponse>.Failure("Failed to create demo Google account.", 400);
            }
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            token,
            user.AvatarUrl));
    }

    private async Task<Result<AuthResponse>> HandleDemoGitHubUserAsync(CancellationToken cancellationToken)
    {
        const string email = "github.demo@kanbanboard.dev";
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = "GitHub Demo User",
                AvatarUrl = "https://avatars.githubusercontent.com/u/583231?v=4",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Result<AuthResponse>.Failure("Failed to create demo GitHub account.", 400);
            }
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email!,
            user.FullName,
            token,
            user.AvatarUrl));
    }

    private record GitHubTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);

    private record GitHubUserResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl);

    private record GitHubEmailResponse(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("primary")] bool Primary,
        [property: JsonPropertyName("verified")] bool Verified);
}
