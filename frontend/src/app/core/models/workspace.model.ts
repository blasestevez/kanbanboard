export type WorkspaceRole = 'Owner' | 'Member' | 'Observer';

export interface WorkspaceSummary {
  id: string;
  name: string;
  description?: string | null;
  logoUrl?: string | null;
  ownerId: string;
  currentUserRole: WorkspaceRole;
  memberCount: number;
  createdAt: string;
}

export interface WorkspaceMember {
  userId: string;
  email: string;
  fullName: string;
  avatarUrl?: string | null;
  role: WorkspaceRole;
  joinedAt: string;
}

export interface WorkspaceDetail {
  id: string;
  name: string;
  description?: string | null;
  logoUrl?: string | null;
  ownerId: string;
  currentUserRole: WorkspaceRole;
  members: WorkspaceMember[];
  createdAt: string;
}

export interface CreateWorkspaceRequest {
  name: string;
  description?: string | null;
  logoUrl?: string | null;
}

export interface UpdateWorkspaceRequest {
  name: string;
  description?: string | null;
  logoUrl?: string | null;
}

export interface AddWorkspaceMemberRequest {
  email: string;
  role: WorkspaceRole;
}

export interface UpdateMemberRoleRequest {
  role: WorkspaceRole;
}
