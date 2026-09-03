import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { vi } from 'vitest';
import { authGuard } from './auth.guard';
import { guestGuard } from './guest.guard';
import { AuthService } from '../services/auth.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('Guards', () => {
  let authService: { isAuthenticated: ReturnType<typeof vi.fn>; getToken: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    authService = {
      isAuthenticated: vi.fn(),
      getToken: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    router = TestBed.inject(Router);
  });

  describe('authGuard', () => {
    it('should allow access when user is authenticated', () => {
      authService.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() =>
        authGuard(
          {} as ActivatedRouteSnapshot,
          { url: '/workspaces' } as RouterStateSnapshot
        )
      );

      expect(result).toBe(true);
    });

    it('should redirect to login when user is not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() =>
        authGuard(
          {} as ActivatedRouteSnapshot,
          { url: '/workspaces' } as RouterStateSnapshot
        )
      );

      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toContain('/auth/login');
    });
  });

  describe('guestGuard', () => {
    it('should redirect to workspaces when user is authenticated', () => {
      authService.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() =>
        guestGuard(
          {} as ActivatedRouteSnapshot,
          { url: '/auth/login' } as RouterStateSnapshot
        )
      );

      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toContain('/workspaces');
    });

    it('should allow access when user is not authenticated', () => {
      authService.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() =>
        guestGuard(
          {} as ActivatedRouteSnapshot,
          { url: '/auth/login' } as RouterStateSnapshot
        )
      );

      expect(result).toBe(true);
    });
  });
});
