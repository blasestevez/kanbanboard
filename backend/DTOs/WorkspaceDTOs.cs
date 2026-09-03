namespace Trellochocero.Api.DTOs;

// Workspaces DTOs
public record CreateWorkspaceRequest(string Name, string? Description, string? LogoUrl);
public record UpdateWorkspaceRequest(string Name, string? Description, string? LogoUrl);
public record AddWorkspaceMemberRequest(string Email, string Role);
public record UpdateMemberRoleRequest(string Role);

public record WorkspaceSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? LogoUrl,
    Guid OwnerId,
    string CurrentUserRole,
    int MemberCount,
    DateTime CreatedAt);

public record WorkspaceDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string? LogoUrl,
    Guid OwnerId,
    string CurrentUserRole,
    List<WorkspaceMemberResponse> Members,
    DateTime CreatedAt);

public record WorkspaceMemberResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);
