import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { WorkspaceService } from './workspace.service';
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

describe('WorkspaceService', () => {
  let service: WorkspaceService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/workspaces`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        WorkspaceService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(WorkspaceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get all workspaces and update signal', () => {
    const mockWorkspaces: WorkspaceSummary[] = [
      {
        id: 'ws-1',
        name: 'Workspace Alpha',
        description: 'Alpha team',
        logoUrl: null,
        ownerId: 'user-1',
        currentUserRole: 'Owner',
        memberCount: 2,
        createdAt: '2026-09-02T20:00:00Z',
      },
    ];

    service.getWorkspaces().subscribe((res) => {
      expect(res).toEqual(mockWorkspaces);
      expect(service.workspaces()).toEqual(mockWorkspaces);
      expect(service.isLoading()).toBe(false);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockWorkspaces);
  });

  it('should get workspace detail and update activeWorkspace signal', () => {
    const mockDetail: WorkspaceDetail = {
      id: 'ws-1',
      name: 'Workspace Alpha',
      description: 'Alpha team',
      logoUrl: null,
      ownerId: 'user-1',
      currentUserRole: 'Owner',
      members: [
        {
          userId: 'user-1',
          email: 'owner@example.com',
          fullName: 'Alice Owner',
          avatarUrl: null,
          role: 'Owner',
          joinedAt: '2026-09-02T20:00:00Z',
        },
      ],
      createdAt: '2026-09-02T20:00:00Z',
    };

    service.getWorkspace('ws-1').subscribe((res) => {
      expect(res).toEqual(mockDetail);
      expect(service.activeWorkspace()).toEqual(mockDetail);
    });

    const req = httpMock.expectOne(`${apiUrl}/ws-1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockDetail);
  });

  it('should create workspace and prepend to workspaces signal', () => {
    const createReq: CreateWorkspaceRequest = {
      name: 'New Workspace',
      description: 'New Description',
    };
    const mockCreated: WorkspaceSummary = {
      id: 'ws-new',
      name: 'New Workspace',
      description: 'New Description',
      logoUrl: null,
      ownerId: 'user-1',
      currentUserRole: 'Owner',
      memberCount: 1,
      createdAt: '2026-09-02T20:00:00Z',
    };

    service.createWorkspace(createReq).subscribe((res) => {
      expect(res).toEqual(mockCreated);
      expect(service.workspaces()[0]).toEqual(mockCreated);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(mockCreated);
  });

  it('should update workspace details', () => {
    const updateReq: UpdateWorkspaceRequest = {
      name: 'Updated Name',
      description: 'Updated Desc',
    };
    const mockUpdated: WorkspaceDetail = {
      id: 'ws-1',
      name: 'Updated Name',
      description: 'Updated Desc',
      logoUrl: null,
      ownerId: 'user-1',
      currentUserRole: 'Owner',
      members: [],
      createdAt: '2026-09-02T20:00:00Z',
    };

    service.updateWorkspace('ws-1', updateReq).subscribe((res) => {
      expect(res.name).toBe('Updated Name');
      expect(service.activeWorkspace()?.name).toBe('Updated Name');
    });

    const req = httpMock.expectOne(`${apiUrl}/ws-1`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockUpdated);
  });

  it('should delete workspace', () => {
    service.deleteWorkspace('ws-1').subscribe();

    const req = httpMock.expectOne(`${apiUrl}/ws-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should add member to workspace', () => {
    const addReq: AddWorkspaceMemberRequest = {
      email: 'bob@example.com',
      role: 'Member',
    };
    const mockMember: WorkspaceMember = {
      userId: 'user-2',
      email: 'bob@example.com',
      fullName: 'Bob Member',
      avatarUrl: null,
      role: 'Member',
      joinedAt: '2026-09-02T20:05:00Z',
    };

    service.addMember('ws-1', addReq).subscribe((res) => {
      expect(res).toEqual(mockMember);
    });

    const req = httpMock.expectOne(`${apiUrl}/ws-1/members`);
    expect(req.request.method).toBe('POST');
    req.flush(mockMember);
  });

  it('should update member role', () => {
    const updateRoleReq: UpdateMemberRoleRequest = { role: 'Observer' };
    const mockUpdatedMember: WorkspaceMember = {
      userId: 'user-2',
      email: 'bob@example.com',
      fullName: 'Bob Member',
      avatarUrl: null,
      role: 'Observer',
      joinedAt: '2026-09-02T20:05:00Z',
    };

    service.updateMemberRole('ws-1', 'user-2', updateRoleReq).subscribe((res) => {
      expect(res.role).toBe('Observer');
    });

    const req = httpMock.expectOne(`${apiUrl}/ws-1/members/user-2`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockUpdatedMember);
  });

  it('should remove member from workspace', () => {
    service.removeMember('ws-1', 'user-2').subscribe();

    const req = httpMock.expectOne(`${apiUrl}/ws-1/members/user-2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
