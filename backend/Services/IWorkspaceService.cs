using Trellochocero.Api.DTOs;

namespace Trellochocero.Api.Services;

public interface IWorkspaceService
{
    Task<Result<List<WorkspaceSummaryResponse>>> GetUserWorkspacesAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceDetailResponse>> GetWorkspaceByIdAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceSummaryResponse>> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceSummaryResponse>> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteWorkspaceAsync(Guid workspaceId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceMemberResponse>> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceMemberResponse>> UpdateMemberRoleAsync(Guid workspaceId, Guid targetUserId, UpdateMemberRoleRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveMemberAsync(Guid workspaceId, Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken = default);
}
