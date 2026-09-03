import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { SidebarComponent } from './sidebar.component';
import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceSummary } from '../../core/models/workspace.model';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;
  let workspaceServiceSpy: {
    workspaces: ReturnType<typeof signal<WorkspaceSummary[]>>;
    getWorkspaces: ReturnType<typeof vi.fn>;
  };

  const mockWorkspaces: WorkspaceSummary[] = [
    {
      id: 'ws-1',
      name: 'Engineering',
      description: null,
      logoUrl: null,
      ownerId: 'user-1',
      currentUserRole: 'Owner',
      memberCount: 4,
      createdAt: '2026-09-02T20:00:00Z',
    },
  ];

  beforeEach(async () => {
    workspaceServiceSpy = {
      workspaces: signal<WorkspaceSummary[]>(mockWorkspaces),
      getWorkspaces: vi.fn().mockReturnValue(of(mockWorkspaces)),
    };

    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should compute workspace initials', () => {
    expect(component.getWorkspaceInitials('Engineering')).toBe('E');
    expect(component.getWorkspaceInitials('Marketing Team')).toBe('MT');
  });

  it('should emit closeSidebar', () => {
    let emitted = false;
    component.closeSidebar.subscribe(() => {
      emitted = true;
    });
    component.onClose();
    expect(emitted).toBe(true);
  });
});
