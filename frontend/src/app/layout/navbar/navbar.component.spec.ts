import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { NavbarComponent } from './navbar.component';
import { AuthService } from '../../core/services/auth.service';
import { User } from '../../core/models/auth.model';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  let authServiceSpy: {
    currentUser: ReturnType<typeof signal<User | null>>;
    logout: ReturnType<typeof vi.fn>;
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

    await TestBed.configureTestingModule({
      imports: [NavbarComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should compute user initials', () => {
    expect(component.userInitials()).toBe('JD');
  });

  it('should toggle dropdown menu', () => {
    expect(component.isDropdownOpen()).toBe(false);
    component.toggleMenu();
    expect(component.isDropdownOpen()).toBe(true);
    component.closeMenu();
    expect(component.isDropdownOpen()).toBe(false);
  });

  it('should call authService.logout on logout', () => {
    component.onLogout();
    expect(authServiceSpy.logout).toHaveBeenCalled();
  });
});
