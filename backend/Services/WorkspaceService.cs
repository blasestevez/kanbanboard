using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trellochocero.Api.Data;
using Trellochocero.Api.DTOs;
using Trellochocero.Api.Models;

namespace Trellochocero.Api.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WorkspaceService> _logger;

    public WorkspaceService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<WorkspaceService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<List<WorkspaceSummaryResponse>>> GetUserWorkspacesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var workspaces = await _dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == currentUserId))
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceSummaryResponse(
                w.Id,
                w.Name,
                w.Description,
                w.LogoUrl,
                w.OwnerId,
                w.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role.ToString()).FirstOrDefault() ?? "Member",
                w.Members.Count,
                w.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<List<WorkspaceSummaryResponse>>.Success(workspaces);
    }

    public async Task<Result<WorkspaceDetailResponse>> GetWorkspaceByIdAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.Workspaces
            .AsNoTracking()
            .Include(w => w.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<WorkspaceDetailResponse>.Failure("Workspace not found.", 404);
        }

        var currentMember = workspace.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (currentMember == null)
        {
            return Result<WorkspaceDetailResponse>.Failure("You do not have access to this workspace.", 403);
        }

        var memberResponses = workspace.Members
            .OrderBy(m => m.Role == WorkspaceRole.Owner ? 0 : m.Role == WorkspaceRole.Member ? 1 : 2)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new WorkspaceMemberResponse(
                m.UserId,
                m.User.Email ?? string.Empty,
                m.User.FullName,
                m.User.AvatarUrl,
                m.Role.ToString(),
                m.JoinedAt
            ))
            .ToList();

        var response = new WorkspaceDetailResponse(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.LogoUrl,
            workspace.OwnerId,
            currentMember.Role.ToString(),
            memberResponses,
            workspace.CreatedAt
        );

        return Result<WorkspaceDetailResponse>.Success(response);
    }

    public async Task<Result<WorkspaceSummaryResponse>> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<WorkspaceSummaryResponse>.Failure(
                "Workspace name is required.",
                400,
                new Dictionary<string, string[]> { { "Name", ["The Name field is required."] } });
        }

        if (request.Name.Trim().Length > 100)
        {
            return Result<WorkspaceSummaryResponse>.Failure(
                "Workspace name must not exceed 100 characters.",
                400,
                new Dictionary<string, string[]> { { "Name", ["The Name field must not exceed 100 characters."] } });
        }

        if (request.Description?.Length > 500)
        {
            return Result<WorkspaceSummaryResponse>.Failure(
                "Description must not exceed 500 characters.",
                400,
                new Dictionary<string, string[]> { { "Description", ["The Description field must not exceed 500 characters."] } });
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            OwnerId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        workspace.Members.Add(new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = currentUserId,
            Role = WorkspaceRole.Owner,
            JoinedAt = workspace.CreatedAt
        });

        _dbContext.Workspaces.Add(workspace);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorkspaceSummaryResponse(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.LogoUrl,
            workspace.OwnerId,
            WorkspaceRole.Owner.ToString(),
            1,
            workspace.CreatedAt
        );

        return Result<WorkspaceSummaryResponse>.Created(response);
    }

    public async Task<Result<WorkspaceSummaryResponse>> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<WorkspaceSummaryResponse>.Failure(
                "Workspace name is required.",
                400,
                new Dictionary<string, string[]> { { "Name", ["The Name field is required."] } });
        }

        if (request.Name.Trim().Length > 100)
        {
            return Result<WorkspaceSummaryResponse>.Failure(
                "Workspace name must not exceed 100 characters.",
                400,
                new Dictionary<string, string[]> { { "Name", ["The Name field must not exceed 100 characters."] } });
        }

        if (request.Description?.Length > 500)
        {
            return Result<WorkspaceSummaryResponse>.Failure(
                "Description must not exceed 500 characters.",
                400,
                new Dictionary<string, string[]> { { "Description", ["The Description field must not exceed 500 characters."] } });
        }

        var workspace = await _dbContext.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<WorkspaceSummaryResponse>.Failure("Workspace not found.", 404);
        }

        var isOwner = workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);
        if (!isOwner)
        {
            return Result<WorkspaceSummaryResponse>.Failure("Only workspace owners can update workspace details.", 403);
        }

        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description?.Trim();
        workspace.LogoUrl = request.LogoUrl?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorkspaceSummaryResponse(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.LogoUrl,
            workspace.OwnerId,
            WorkspaceRole.Owner.ToString(),
            workspace.Members.Count,
            workspace.CreatedAt
        );

        return Result<WorkspaceSummaryResponse>.Success(response);
    }

    public async Task<Result<bool>> DeleteWorkspaceAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<bool>.Failure("Workspace not found.", 404);
        }

        var isOwner = workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);
        if (!isOwner)
        {
            return Result<bool>.Failure("Only workspace owners can delete this workspace.", 403);
        }

        _dbContext.Workspaces.Remove(workspace);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, 204);
    }

    public async Task<Result<WorkspaceMemberResponse>> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<WorkspaceMemberResponse>.Failure(
                "Email is required.",
                400,
                new Dictionary<string, string[]> { { "Email", ["The Email field is required."] } });
        }

        if (!Enum.TryParse<WorkspaceRole>(request.Role, true, out var role))
        {
            return Result<WorkspaceMemberResponse>.Failure(
                "Invalid workspace role. Valid roles are: Owner, Member, Observer.",
                400,
                new Dictionary<string, string[]> { { "Role", ["Invalid role specified."] } });
        }

        var workspace = await _dbContext.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<WorkspaceMemberResponse>.Failure("Workspace not found.", 404);
        }

        var isOwner = workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);
        if (!isOwner)
        {
            return Result<WorkspaceMemberResponse>.Failure("Only workspace owners can add members.", 403);
        }

        var targetUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (targetUser == null)
        {
            return Result<WorkspaceMemberResponse>.Failure(
                "User with the specified email does not exist.",
                404,
                new Dictionary<string, string[]> { { "Email", ["No registered user found with this email address."] } });
        }

        if (workspace.Members.Any(m => m.UserId == targetUser.Id))
        {
            return Result<WorkspaceMemberResponse>.Failure(
                "User is already a member of this workspace.",
                400,
                new Dictionary<string, string[]> { { "Email", ["User is already a member of this workspace."] } });
        }

        var newMember = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = targetUser.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.WorkspaceMembers.Add(newMember);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorkspaceMemberResponse(
            targetUser.Id,
            targetUser.Email!,
            targetUser.FullName,
            targetUser.AvatarUrl,
            role.ToString(),
            newMember.JoinedAt
        );

        return Result<WorkspaceMemberResponse>.Success(response);
    }

    public async Task<Result<WorkspaceMemberResponse>> UpdateMemberRoleAsync(Guid workspaceId, Guid targetUserId, UpdateMemberRoleRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<WorkspaceRole>(request.Role, true, out var role))
        {
            return Result<WorkspaceMemberResponse>.Failure(
                "Invalid workspace role. Valid roles are: Owner, Member, Observer.",
                400,
                new Dictionary<string, string[]> { { "Role", ["Invalid role specified."] } });
        }

        var workspace = await _dbContext.Workspaces
            .Include(w => w.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<WorkspaceMemberResponse>.Failure("Workspace not found.", 404);
        }

        var isOwner = workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);
        if (!isOwner)
        {
            return Result<WorkspaceMemberResponse>.Failure("Only workspace owners can update member roles.", 403);
        }

        var member = workspace.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (member == null)
        {
            return Result<WorkspaceMemberResponse>.Failure("Member not found in this workspace.", 404);
        }

        if (member.Role == WorkspaceRole.Owner && role != WorkspaceRole.Owner)
        {
            var ownerCount = workspace.Members.Count(m => m.Role == WorkspaceRole.Owner);
            if (ownerCount <= 1)
            {
                return Result<WorkspaceMemberResponse>.Failure("Cannot demote the only Owner of the workspace.", 400);
            }
        }

        member.Role = role;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = member.User;
        var response = new WorkspaceMemberResponse(
            targetUserId,
            user.Email ?? string.Empty,
            user.FullName,
            user.AvatarUrl,
            role.ToString(),
            member.JoinedAt
        );

        return Result<WorkspaceMemberResponse>.Success(response);
    }

    public async Task<Result<bool>> RemoveMemberAsync(Guid workspaceId, Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
        {
            return Result<bool>.Failure("Workspace not found.", 404);
        }

        var member = workspace.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (member == null)
        {
            return Result<bool>.Failure("Member not found in this workspace.", 404);
        }

        if (currentUserId == targetUserId)
        {
            // Voluntary leave
            if (member.Role == WorkspaceRole.Owner)
            {
                var ownerCount = workspace.Members.Count(m => m.Role == WorkspaceRole.Owner);
                if (ownerCount <= 1)
                {
                    return Result<bool>.Failure(
                        "The sole Owner cannot leave the workspace without transferring ownership or deleting the workspace.",
                        400);
                }
            }
        }
        else
        {
            // Removing someone else
            var isOwner = workspace.Members.Any(m => m.UserId == currentUserId && m.Role == WorkspaceRole.Owner);
            if (!isOwner)
            {
                return Result<bool>.Failure("Only workspace owners can remove members from the workspace.", 403);
            }
        }

        _dbContext.WorkspaceMembers.Remove(member);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, 204);
    }
}
