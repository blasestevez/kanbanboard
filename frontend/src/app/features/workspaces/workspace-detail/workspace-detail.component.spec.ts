import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { WorkspaceDetailComponent } from './workspace-detail.component';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { BoardService } from '../../../core/services/board.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  WorkspaceDetail,
  WorkspaceMember,
} from '../../../core/models/workspace.model';
import { BoardSummary } from '../../../core/models/board.model';
import { User } from '../../../core/models/auth.model';

describe('WorkspaceDetailComponent', () => {
  let component: WorkspaceDetailComponent;
  let fixture: ComponentFixture<WorkspaceDetailComponent>;

  const mockDetail: WorkspaceDetail = {
    id: 'ws-1',
    name: 'Alpha Team',
    description: 'Alpha team description',
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
      {
        userId: 'user-2',
        email: 'member@example.com',
        fullName: 'Bob Member',
        avatarUrl: null,
        role: 'Member',
        joinedAt: '2026-09-02T20:05:00Z',
      },
    ],
    createdAt: '2026-09-02T20:00:00Z',
  };

  const mockBoards: BoardSummary[] = [
    {
      id: 'board-1',
      workspaceId: 'ws-1',
      title: 'Roadmap Q4',
      backgroundColor: '#0079bf',
      backgroundImageUrl: null,
      isClosed: false,
      position: 0,
      listsCount: 3,
      createdAt: '2026-09-02T20:00:00Z',
    },
  ];

  const mockUser: User = {
    id: 'user-1',
    email: 'owner@example.com',
    fullName: 'Alice Owner',
  };

  let workspaceServiceSpy: {
    activeWorkspace: ReturnType<typeof signal<WorkspaceDetail | null>>;
    getWorkspace: ReturnType<typeof vi.fn>;
    updateWorkspace: ReturnType<typeof vi.fn>;
    deleteWorkspace: ReturnType<typeof vi.fn>;
    addMember: ReturnType<typeof vi.fn>;
    updateMemberRole: ReturnType<typeof vi.fn>;
    removeMember: ReturnType<typeof vi.fn>;
  };

  let boardServiceSpy: {
    getWorkspaceBoards: ReturnType<typeof vi.fn>;
    createBoard: ReturnType<typeof vi.fn>;
  };

  let authServiceSpy: {
    currentUser: ReturnType<typeof signal<User | null>>;
  };

  beforeEach(async () => {
    workspaceServiceSpy = {
      activeWorkspace: signal<WorkspaceDetail | null>(mockDetail),
      getWorkspace: vi.fn().mockReturnValue(of(mockDetail)),
      updateWorkspace: vi.fn(),
      deleteWorkspace: vi.fn(),
      addMember: vi.fn(),
      updateMemberRole: vi.fn(),
      removeMember: vi.fn(),
    };

    boardServiceSpy = {
      getWorkspaceBoards: vi.fn().mockReturnValue(of(mockBoards)),
      createBoard: vi.fn(),
    };

    authServiceSpy = {
      currentUser: signal<User | null>(mockUser),
    };

    await TestBed.configureTestingModule({
      imports: [WorkspaceDetailComponent],
      providers: [
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        { provide: BoardService, useValue: boardServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([
          { path: 'workspaces', component: class {} },
          { path: 'workspaces/:id', component: class {} },
          { path: 'boards/:id', component: class {} },
        ]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'ws-1' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load workspace detail and boards on init', () => {
    expect(workspaceServiceSpy.getWorkspace).toHaveBeenCalledWith('ws-1');
    expect(boardServiceSpy.getWorkspaceBoards).toHaveBeenCalledWith('ws-1');
    expect(component.workspace()?.name).toBe('Alpha Team');
    expect(component.boards().length).toBe(1);
    expect(component.isOwner()).toBe(true);
    expect(component.canCreateBoard()).toBe(true);
  });

  it('should switch tabs', () => {
    expect(component.activeTab()).toBe('members');
    component.setActiveTab('boards');
    expect(component.activeTab()).toBe('boards');
  });

  it('should create a new board in workspace', () => {
    const createdBoard: BoardSummary = {
      id: 'board-2',
      workspaceId: 'ws-1',
      title: 'Marketing Sprint',
      backgroundColor: '#519839',
      backgroundImageUrl: null,
      isClosed: false,
      position: 1,
      listsCount: 0,
      createdAt: '2026-09-02T20:00:00Z',
    };
    boardServiceSpy.createBoard.mockReturnValue(of(createdBoard));

    component.openCreateBoardModal();
    expect(component.isCreateBoardModalOpen()).toBe(true);

    component.createBoardForm.controls.title.setValue('Marketing Sprint');
    component.selectBoardColor('#519839');

    component.onCreateBoardSubmit();

    expect(boardServiceSpy.createBoard).toHaveBeenCalledWith('ws-1', {
      title: 'Marketing Sprint',
      backgroundColor: '#519839',
    });
    expect(component.isCreateBoardModalOpen()).toBe(false);
  });

  it('should invite a new member', () => {
    const newMember: WorkspaceMember = {
      userId: 'user-3',
      email: 'carol@example.com',
      fullName: 'Carol Observer',
      avatarUrl: null,
      role: 'Observer',
      joinedAt: '2026-09-02T20:10:00Z',
    };
    workspaceServiceSpy.addMember.mockReturnValue(of(newMember));

    component.openInviteModal();
    component.inviteForm.controls.email.setValue('carol@example.com');
    component.inviteForm.controls.role.setValue('Observer');

    component.onInviteSubmit();

    expect(workspaceServiceSpy.addMember).toHaveBeenCalledWith('ws-1', {
      email: 'carol@example.com',
      role: 'Observer',
    });
    expect(component.isInviteModalOpen()).toBe(false);
  });

  it('should update workspace details', () => {
    const updatedDetail: WorkspaceDetail = {
      ...mockDetail,
      name: 'Alpha Team Renamed',
      description: 'New Description',
    };
    workspaceServiceSpy.updateWorkspace.mockReturnValue(of(updatedDetail));

    component.openEditModal();
    component.editForm.controls.name.setValue('Alpha Team Renamed');
    component.editForm.controls.description.setValue('New Description');

    component.onEditSubmit();

    expect(workspaceServiceSpy.updateWorkspace).toHaveBeenCalledWith('ws-1', {
      name: 'Alpha Team Renamed',
      description: 'New Description',
    });
    expect(component.isEditModalOpen()).toBe(false);
  });

  it('should update member role', () => {
    const updatedMember: WorkspaceMember = {
      ...mockDetail.members[1],
      role: 'Observer',
    };
    workspaceServiceSpy.updateMemberRole.mockReturnValue(of(updatedMember));

    const mockEvent = {
      target: { value: 'Observer' } as HTMLSelectElement,
    } as unknown as Event;

    component.onRoleChange(mockDetail.members[1], mockEvent);

    expect(workspaceServiceSpy.updateMemberRole).toHaveBeenCalledWith(
      'ws-1',
      'user-2',
      { role: 'Observer' }
    );
  });

  it('should remove a member after confirmation', () => {
    workspaceServiceSpy.removeMember.mockReturnValue(of(undefined));

    component.promptRemoveMember(mockDetail.members[1]);
    expect(component.memberToRemove()).toEqual(mockDetail.members[1]);

    component.confirmRemoveMember();

    expect(workspaceServiceSpy.removeMember).toHaveBeenCalledWith(
      'ws-1',
      'user-2'
    );
    expect(component.memberToRemove()).toBeNull();
  });

  it('should delete workspace after confirmation', () => {
    workspaceServiceSpy.deleteWorkspace.mockReturnValue(of(undefined));

    component.openDeleteModal();
    expect(component.isDeleteConfirmOpen()).toBe(true);

    component.confirmDeleteWorkspace();

    expect(workspaceServiceSpy.deleteWorkspace).toHaveBeenCalledWith('ws-1');
    expect(component.isDeleteConfirmOpen()).toBe(false);
  });
});
