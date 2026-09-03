import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { WorkspacesComponent } from './workspaces.component';
import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceSummary } from '../../core/models/workspace.model';

describe('WorkspacesComponent', () => {
  let component: WorkspacesComponent;
  let fixture: ComponentFixture<WorkspacesComponent>;
  let workspaceServiceSpy: {
    workspaces: ReturnType<typeof signal<WorkspaceSummary[]>>;
    getWorkspaces: ReturnType<typeof vi.fn>;
    createWorkspace: ReturnType<typeof vi.fn>;
  };

  const mockWorkspaces: WorkspaceSummary[] = [
    {
      id: 'ws-1',
      name: 'Alpha Team',
      description: 'Alpha team description',
      logoUrl: null,
      ownerId: 'user-1',
      currentUserRole: 'Owner',
      memberCount: 3,
      createdAt: '2026-09-02T20:00:00Z',
    },
    {
      id: 'ws-2',
      name: 'Beta Project',
      description: 'Beta project description',
      logoUrl: null,
      ownerId: 'user-2',
      currentUserRole: 'Member',
      memberCount: 5,
      createdAt: '2026-09-02T20:00:00Z',
    },
  ];

  beforeEach(async () => {
    workspaceServiceSpy = {
      workspaces: signal<WorkspaceSummary[]>(mockWorkspaces),
      getWorkspaces: vi.fn().mockReturnValue(of(mockWorkspaces)),
      createWorkspace: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [WorkspacesComponent],
      providers: [
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        provideRouter([
          { path: 'workspaces', component: class {} },
          { path: 'workspaces/:id', component: class {} },
        ]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspacesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load workspaces on init', () => {
    expect(workspaceServiceSpy.getWorkspaces).toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
    expect(component.workspaces().length).toBe(2);
  });

  it('should filter workspaces based on search query', () => {
    expect(component.filteredWorkspaces().length).toBe(2);

    component.searchQuery.set('Alpha');
    expect(component.filteredWorkspaces().length).toBe(1);
    expect(component.filteredWorkspaces()[0].name).toBe('Alpha Team');

    component.searchQuery.set('NonExistent');
    expect(component.filteredWorkspaces().length).toBe(0);
  });

  it('should open and close create workspace modal', () => {
    expect(component.isCreateModalOpen()).toBe(false);
    component.openCreateModal();
    expect(component.isCreateModalOpen()).toBe(true);
    component.closeCreateModal();
    expect(component.isCreateModalOpen()).toBe(false);
  });

  it('should submit create form and navigate to new workspace', () => {
    const created: WorkspaceSummary = {
      id: 'ws-new',
      name: 'Gamma Squad',
      description: null,
      logoUrl: null,
      ownerId: 'user-1',
      currentUserRole: 'Owner',
      memberCount: 1,
      createdAt: '2026-09-02T20:00:00Z',
    };
    workspaceServiceSpy.createWorkspace.mockReturnValue(of(created));

    component.createForm.controls.name.setValue('Gamma Squad');
    component.onCreateSubmit();

    expect(workspaceServiceSpy.createWorkspace).toHaveBeenCalledWith({
      name: 'Gamma Squad',
      description: null,
    });
    expect(component.isCreating()).toBe(false);
    expect(component.isCreateModalOpen()).toBe(false);
  });

  it('should handle error on create workspace failure', () => {
    workspaceServiceSpy.createWorkspace.mockReturnValue(
      throwError(() => ({
        status: 400,
        error: { title: 'Workspace name already in use' },
      }))
    );

    component.createForm.controls.name.setValue('Duplicate Name');
    component.onCreateSubmit();

    expect(component.isCreating()).toBe(false);
    expect(component.errorMessage()).toBe('Workspace name already in use');
  });
});
