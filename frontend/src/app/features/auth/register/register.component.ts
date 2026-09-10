import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

const passwordMatchValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  if (password && confirmPassword && password !== confirmPassword) {
    return { passwordMismatch: true };
  }
  return null;
};

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly showConfirmPassword = signal(false);

  readonly registerForm = this.fb.nonNullable.group(
    {
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: [passwordMatchValidator] }
  );

  get fullNameControl() {
    return this.registerForm.controls.fullName;
  }

  get emailControl() {
    return this.registerForm.controls.email;
  }

  get passwordControl() {
    return this.registerForm.controls.password;
  }

  get confirmPasswordControl() {
    return this.registerForm.controls.confirmPassword;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((val) => !val);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update((val) => !val);
  }

  onSubmit(): void {
    if (this.registerForm.invalid || this.isLoading()) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { fullName, email, password } = this.registerForm.getRawValue();

    this.authService.register({ fullName, email, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/workspaces']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  readonly oauthModalProvider = signal<'google' | 'github' | null>(null);

  onGoogleLogin(): void {
    if (this.isLoading()) return;
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.authService.getOAuthConfig().subscribe({
      next: (config) => {
        if (config.googleConfigured && config.googleClientId) {
          this.authService
            .initiateGoogleLogin(config.googleClientId)
            .then((idToken) => {
              this.authService.googleAuth(idToken).subscribe({
                next: () => {
                  this.isLoading.set(false);
                  this.router.navigate(['/workspaces']);
                },
                error: (err: HttpErrorResponse) => {
                  this.isLoading.set(false);
                  this.errorMessage.set(this.extractErrorMessage(err));
                },
              });
            })
            .catch((err) => {
              this.isLoading.set(false);
              this.errorMessage.set(err.message || 'Error al conectar con Google.');
            });
        } else {
          this.isLoading.set(false);
          this.oauthModalProvider.set('google');
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.oauthModalProvider.set('google');
      },
    });
  }

  onGithubLogin(): void {
    if (this.isLoading()) return;
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.authService.getOAuthConfig().subscribe({
      next: (config) => {
        if (config.gitHubConfigured && config.gitHubClientId) {
          this.authService.redirectToGitHub(config.gitHubClientId);
        } else {
          this.isLoading.set(false);
          this.oauthModalProvider.set('github');
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.oauthModalProvider.set('github');
      },
    });
  }

  loginWithDemo(provider: 'google' | 'github'): void {
    this.oauthModalProvider.set(null);
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const auth$ =
      provider === 'google'
        ? this.authService.googleAuth('demo-google')
        : this.authService.githubAuth('demo-github');

    auth$.subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/workspaces']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Unable to connect to server. Please check your connection.';
    }

    if (err.error?.errors) {
      const messages = Object.values(err.error.errors).flat() as string[];
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (err.error?.title) {
      return err.error.title;
    }

    if (err.error?.message) {
      return err.error.message;
    }

    if (err.status === 400) {
      return 'Registration failed. Please verify your details.';
    }

    return 'An unexpected error occurred. Please try again.';
  }
}
