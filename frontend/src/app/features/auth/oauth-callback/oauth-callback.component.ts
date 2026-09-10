import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card text-center">
        @if (isLoading()) {
          <div class="py-8 flex flex-col items-center justify-center gap-4">
            <div class="spinner-lg"></div>
            <h2 class="text-xl font-semibold text-gray-800 dark:text-gray-100">
              {{ statusMessage() }}
            </h2>
            <p class="text-sm text-gray-500 dark:text-gray-400">
              Por favor espera mientras verificamos tus credenciales...
            </p>
          </div>
        } @else if (errorMessage()) {
          <div class="py-6 flex flex-col items-center gap-4">
            <div class="alert alert-error w-full text-left" role="alert">
              <span>{{ errorMessage() }}</span>
            </div>
            <a routerLink="/auth/login" class="btn-submit text-center mt-4">
              Volver al inicio de sesión
            </a>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .spinner-lg {
      width: 48px;
      height: 48px;
      border: 4px solid rgba(59, 130, 246, 0.2);
      border-top-color: #3b82f6;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class OAuthCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  readonly isLoading = signal(true);
  readonly statusMessage = signal('Iniciando proceso de autenticación...');
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const queryParams = this.route.snapshot.queryParams;
    const errorParam = queryParams['error'];
    const errorDescription = queryParams['error_description'];

    if (errorParam) {
      this.isLoading.set(false);
      this.errorMessage.set(errorDescription || 'El proveedor rechazó la autenticación.');
      return;
    }

    const code = queryParams['code'];
    if (code) {
      this.handleGitHubCallback(code);
      return;
    }

    // Check fragment for Google hash tokens (#id_token=...)
    const fragment = this.route.snapshot.fragment;
    if (fragment) {
      const params = new URLSearchParams(fragment);
      const idToken = params.get('id_token');
      if (idToken) {
        this.handleGoogleCallback(idToken);
        return;
      }
    }

    this.isLoading.set(false);
    this.errorMessage.set('No se encontró ningún código de autenticación en la respuesta.');
  }

  private handleGitHubCallback(code: string): void {
    this.statusMessage.set('Autenticando con GitHub...');
    const redirectUri = `${window.location.origin}/auth/callback`;

    this.authService.githubAuth(code, redirectUri).subscribe({
      next: () => {
        this.statusMessage.set('¡Autenticado con éxito! Redirigiendo...');
        this.router.navigate(['/workspaces']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      }
    });
  }

  private handleGoogleCallback(idToken: string): void {
    this.statusMessage.set('Autenticando con Google...');

    this.authService.googleAuth(idToken).subscribe({
      next: () => {
        this.statusMessage.set('¡Autenticado con éxito! Redirigiendo...');
        this.router.navigate(['/workspaces']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      }
    });
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'No se pudo conectar con el servidor. Revisa tu conexión a internet.';
    }
    if (err.error?.title) {
      return err.error.title;
    }
    if (err.error?.message) {
      return err.error.message;
    }
    return 'Ocurrió un error inesperado al procesar la autenticación.';
  }
}
