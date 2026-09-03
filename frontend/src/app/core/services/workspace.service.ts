import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddWorkspaceMemberRequest,
  CreateWorkspaceRequest,
  UpdateMemberRoleRequest,
  UpdateWorkspaceRequest,
  WorkspaceDetail,
  WorkspaceMember,
  WorkspaceSummary,
} from '../models/workspace.model';

@Injectable({
  providedIn: 'root',
})
export class WorkspaceService {
  private readonly http = inject(HttpClient);
  private get apiUrl(): string {
    return `${environment.apiUrl}/workspaces`;
  }

  private readonly _workspaces = signal<WorkspaceSummary[]>([]);
  private readonly _activeWorkspace = signal<WorkspaceDetail | null>(null);
  private readonly _isLoading = signal<boolean>(false);

  readonly workspaces = this._workspaces.asReadonly();
  readonly activeWorkspace = this._activeWorkspace.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  getWorkspaces(): Observable<WorkspaceSummary[]> {
    this._isLoading.set(true);
    return this.http.get<WorkspaceSummary[]>(this.apiUrl).pipe(
      tap({
        next: (workspaces) => {
          this._workspaces.set(workspaces);
          this._isLoading.set(false);
        },
        error: () => this._isLoading.set(false),
      })
    );
  }

  getWorkspace(id: string): Observable<WorkspaceDetail> {
    this._isLoading.set(true);
    return this.http.get<WorkspaceDetail>(`${this.apiUrl}/${id}`).pipe(
      tap({
        next: (workspace) => {
          this._activeWorkspace.set(workspace);
          this._isLoading.set(false);
        },
        error: () => this._isLoading.set(false),
      })
    );
  }

  createWorkspace(req: CreateWorkspaceRequest): Observable<WorkspaceSummary> {
    return this.http.post<WorkspaceSummary>(this.apiUrl, req).pipe(
      tap((newWorkspace) => {
        this._workspaces.update((list) => [newWorkspace, ...list]);
      })
    );
  }

  updateWorkspace(
    id: string,
    req: UpdateWorkspaceRequest
  ): Observable<WorkspaceDetail> {
    return this.http.put<WorkspaceDetail>(`${this.apiUrl}/${id}`, req).pipe(
      tap((updated) => {
        this._activeWorkspace.set(updated);
        this._workspaces.update((list) =>
          list.map((w) =>
            w.id === id
              ? {
                  ...w,
                  name: updated.name,
                  description: updated.description,
                  logoUrl: updated.logoUrl,
                }
              : w
          )
        );
      })
    );
  }

  deleteWorkspace(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        this._workspaces.update((list) => list.filter((w) => w.id !== id));
        if (this._activeWorkspace()?.id === id) {
          this._activeWorkspace.set(null);
        }
      })
    );
  }

  addMember(
    workspaceId: string,
    req: AddWorkspaceMemberRequest
  ): Observable<WorkspaceMember> {
    return this.http
      .post<WorkspaceMember>(`${this.apiUrl}/${workspaceId}/members`, req)
      .pipe(
        tap((newMember) => {
          const active = this._activeWorkspace();
          if (active && active.id === workspaceId) {
            this._activeWorkspace.set({
              ...active,
              members: [...active.members, newMember],
            });
          }
          this._workspaces.update((list) =>
            list.map((w) =>
              w.id === workspaceId
                ? { ...w, memberCount: w.memberCount + 1 }
                : w
            )
          );
        })
      );
  }

  updateMemberRole(
    workspaceId: string,
    userId: string,
    req: UpdateMemberRoleRequest
  ): Observable<WorkspaceMember> {
    return this.http
      .put<WorkspaceMember>(
        `${this.apiUrl}/${workspaceId}/members/${userId}`,
        req
      )
      .pipe(
        tap((updatedMember) => {
          const active = this._activeWorkspace();
          if (active && active.id === workspaceId) {
            this._activeWorkspace.set({
              ...active,
              members: active.members.map((m) =>
                m.userId === userId ? updatedMember : m
              ),
            });
          }
        })
      );
  }

  removeMember(workspaceId: string, userId: string): Observable<void> {
    return this.http
      .delete<void>(`${this.apiUrl}/${workspaceId}/members/${userId}`)
      .pipe(
        tap(() => {
          const active = this._activeWorkspace();
          if (active && active.id === workspaceId) {
            this._activeWorkspace.set({
              ...active,
              members: active.members.filter((m) => m.userId !== userId),
            });
          }
          this._workspaces.update((list) =>
            list.map((w) =>
              w.id === workspaceId
                ? { ...w, memberCount: Math.max(1, w.memberCount - 1) }
                : w
            )
          );
        })
      );
  }
}
