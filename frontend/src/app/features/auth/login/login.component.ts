import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  get emailControl() {
    return this.loginForm.controls.email;
  }

  get passwordControl() {
    return this.loginForm.controls.password;
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((val) => !val);
  }

  onSubmit(): void {
    if (this.loginForm.invalid || this.isLoading()) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.loginForm.getRawValue();

    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/workspaces';
        this.router.navigateByUrl(returnUrl);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  onGoogleLogin(): void {
    if (this.isLoading()) return;
    this.errorMessage.set(null);
    // Placeholder flow for Google OAuth
    const dummyGoogleToken = 'google_oauth_token_placeholder';
    this.isLoading.set(true);
    this.authService.googleAuth(dummyGoogleToken).subscribe({
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

  onGithubLogin(): void {
    if (this.isLoading()) return;
    this.errorMessage.set(null);
    // Placeholder flow for GitHub OAuth
    const dummyGithubCode = 'github_code_placeholder';
    this.isLoading.set(true);
    this.authService.githubAuth(dummyGithubCode).subscribe({
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

    if (err.status === 401) {
      return 'Invalid email or password.';
    }

    return 'An unexpected error occurred. Please try again.';
  }
}
