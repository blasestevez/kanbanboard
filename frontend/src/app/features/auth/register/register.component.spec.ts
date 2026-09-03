import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../../core/services/auth.service';
import { AuthResponse } from '../../../core/models/auth.model';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: {
    register: ReturnType<typeof vi.fn>;
    googleAuth: ReturnType<typeof vi.fn>;
    githubAuth: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceSpy = {
      register: vi.fn(),
      googleAuth: vi.fn(),
      githubAuth: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
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

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should validate password mismatch', () => {
    component.registerForm.controls.fullName.setValue('John Doe');
    component.registerForm.controls.email.setValue('john@example.com');
    component.registerForm.controls.password.setValue('password123');
    component.registerForm.controls.confirmPassword.setValue('different123');

    expect(component.registerForm.valid).toBe(false);
    expect(component.registerForm.errors?.['passwordMismatch']).toBe(true);

    component.registerForm.controls.confirmPassword.setValue('password123');
    expect(component.registerForm.valid).toBe(true);
  });

  it('should call authService.register on valid submit', () => {
    const mockAuthResponse: AuthResponse = {
      id: '1',
      email: 'john@example.com',
      fullName: 'John Doe',
      token: 'jwt-token',
      avatarUrl: null,
    };
    authServiceSpy.register.mockReturnValue(of(mockAuthResponse));

    component.registerForm.controls.fullName.setValue('John Doe');
    component.registerForm.controls.email.setValue('john@example.com');
    component.registerForm.controls.password.setValue('password123');
    component.registerForm.controls.confirmPassword.setValue('password123');

    component.onSubmit();

    expect(authServiceSpy.register).toHaveBeenCalledWith({
      fullName: 'John Doe',
      email: 'john@example.com',
      password: 'password123',
    });
    expect(component.isLoading()).toBe(false);
  });

  it('should handle registration error', () => {
    authServiceSpy.register.mockReturnValue(
      throwError(() => ({
        status: 400,
        error: { title: 'Email already taken' },
      }))
    );

    component.registerForm.controls.fullName.setValue('John Doe');
    component.registerForm.controls.email.setValue('john@example.com');
    component.registerForm.controls.password.setValue('password123');
    component.registerForm.controls.confirmPassword.setValue('password123');

    component.onSubmit();

    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBe('Email already taken');
  });
});
