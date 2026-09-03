import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { MainLayoutComponent } from './main-layout.component';
import { AuthService } from '../../core/services/auth.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { User } from '../../core/models/auth.model';
import { WorkspaceSummary } from '../../core/models/workspace.model';

describe('MainLayoutComponent', () => {
  let component: MainLayoutComponent;
  let fixture: ComponentFixture<MainLayoutComponent>;
  let authServiceSpy: {
    currentUser: ReturnType<typeof signal<User | null>>;
    logout: ReturnType<typeof vi.fn>;
  };
  let workspaceServiceSpy: {
    workspaces: ReturnType<typeof signal<WorkspaceSummary[]>>;
    getWorkspaces: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceSpy = {
      currentUser: signal<User | null>({
        id: '1',
        email: 'john@example.com',
        fullName: 'John Doe',
      }),
      logout: vi.fn(),
    };

    workspaceServiceSpy = {
      workspaces: signal<WorkspaceSummary[]>([]),
      getWorkspaces: vi.fn().mockReturnValue(of([])),
    };

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle sidebar state', () => {
    expect(component.isSidebarOpen()).toBe(false);
    component.toggleSidebar();
    expect(component.isSidebarOpen()).toBe(true);
    component.closeSidebar();
    expect(component.isSidebarOpen()).toBe(false);
  });
});
