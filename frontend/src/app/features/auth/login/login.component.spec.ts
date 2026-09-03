import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';
import { AuthResponse } from '../../../core/models/auth.model';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: {
    login: ReturnType<typeof vi.fn>;
    googleAuth: ReturnType<typeof vi.fn>;
    githubAuth: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceSpy = {
      login: vi.fn(),
      googleAuth: vi.fn(),
      githubAuth: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([
          { path: 'auth/login', component: class {} },
          { path: 'workspaces', component: class {} },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty values and invalid state', () => {
    expect(component.loginForm.valid).toBe(false);
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
  });

  it('should validate email format and password minLength', () => {
    component.loginForm.controls.email.setValue('invalid-email');
    component.loginForm.controls.password.setValue('123');
    expect(component.loginForm.valid).toBe(false);

    component.loginForm.controls.email.setValue('valid@example.com');
    component.loginForm.controls.password.setValue('123456');
    expect(component.loginForm.valid).toBe(true);
  });

  it('should call authService.login on valid submit', () => {
    const mockAuthResponse: AuthResponse = {
      id: '1',
      email: 'user@example.com',
      fullName: 'User',
      token: 'jwt-token',
      avatarUrl: null,
    };
    authServiceSpy.login.mockReturnValue(of(mockAuthResponse));

    component.loginForm.controls.email.setValue('user@example.com');
    component.loginForm.controls.password.setValue('password123');

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({
      email: 'user@example.com',
      password: 'password123',
    });
    expect(component.isLoading()).toBe(false);
  });

  it('should handle login error and set errorMessage', () => {
    authServiceSpy.login.mockReturnValue(
      throwError(() => ({
        status: 401,
        error: { message: 'Invalid credentials' },
      }))
    );

    component.loginForm.controls.email.setValue('user@example.com');
    component.loginForm.controls.password.setValue('wrongpassword');

    component.onSubmit();

    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBe('Invalid credentials');
  });
});
