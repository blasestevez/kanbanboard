import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  GitHubAuthRequest,
  GoogleAuthRequest,
  LoginRequest,
  OAuthConfigResponse,
  RegisterRequest,
  User,
  UserProfileResponse,
} from '../models/auth.model';

const TOKEN_KEY = 'trello_auth_token';
const USER_KEY = 'trello_auth_user';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private get apiUrl(): string {
    return environment.apiUrl;
  }

  private readonly _currentUser = signal<User | null>(this.loadUserFromStorage());
  private readonly _token = signal<string | null>(this.loadTokenFromStorage());

  readonly currentUser = this._currentUser.asReadonly();
  readonly token = this._token.asReadonly();
  readonly isAuthenticated = computed(() => {
    const currentToken = this._token();
    if (!currentToken) {
      return false;
    }
    return !this.isTokenExpired(currentToken);
  });

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/auth/register`, request)
      .pipe(tap((response) => this.handleAuthSuccess(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/auth/login`, request)
      .pipe(tap((response) => this.handleAuthSuccess(response)));
  }

  getOAuthConfig(): Observable<OAuthConfigResponse> {
    return this.http.get<OAuthConfigResponse>(`${this.apiUrl}/auth/oauth-config`);
  }

  googleAuth(idToken: string): Observable<AuthResponse> {
    const payload: GoogleAuthRequest = { idToken };
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/auth/google`, payload)
      .pipe(tap((response) => this.handleAuthSuccess(response)));
  }

  githubAuth(code: string, redirectUri?: string): Observable<AuthResponse> {
    const payload: GitHubAuthRequest = { code, redirectUri };
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/auth/github`, payload)
      .pipe(tap((response) => this.handleAuthSuccess(response)));
  }

  redirectToGitHub(clientId: string): void {
    const redirectUri = `${window.location.origin}/auth/callback`;
    const scope = 'user:email';
    const state = Math.random().toString(36).substring(2, 15);
    sessionStorage.setItem('oauth_state', state);
    sessionStorage.setItem('oauth_provider', 'github');

    const githubUrl = `https://github.com/login/oauth/authorize?client_id=${encodeURIComponent(
      clientId
    )}&redirect_uri=${encodeURIComponent(redirectUri)}&scope=${encodeURIComponent(
      scope
    )}&state=${encodeURIComponent(state)}`;

    window.location.href = githubUrl;
  }

  loadGoogleScript(): Promise<void> {
    if (typeof window === 'undefined') return Promise.resolve();
    if ((window as any).google?.accounts?.id) return Promise.resolve();

    return new Promise((resolve, reject) => {
      const existing = document.getElementById('google-gsi-client');
      if (existing) {
        resolve();
        return;
      }
      const script = document.createElement('script');
      script.id = 'google-gsi-client';
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('No se pudo cargar Google Identity Services'));
      document.head.appendChild(script);
    });
  }

  initiateGoogleLogin(clientId: string): Promise<string> {
    return new Promise((resolve, reject) => {
      this.loadGoogleScript()
        .then(() => {
          const google = (window as any).google;
          if (!google?.accounts?.id) {
            reject(new Error('Google Identity Services no está disponible.'));
            return;
          }

          google.accounts.id.initialize({
            client_id: clientId,
            callback: (response: any) => {
              if (response?.credential) {
                resolve(response.credential);
              } else {
                reject(new Error('No se recibió la credencial de Google.'));
              }
            },
            auto_select: false,
            cancel_on_tap_outside: true,
          });

          google.accounts.id.prompt((notification: any) => {
            if (notification?.isNotDisplayed?.() || notification?.isSkippedMoment?.()) {
              const reason = notification?.getNotDisplayedReason?.() || 'desconocida';
              reject(new Error(`El diálogo de Google no se pudo mostrar (${reason}).`));
            }
          });
        })
        .catch(reject);
    });
  }

  getProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.apiUrl}/users/me`).pipe(
      tap((profile) => {
        const user: User = {
          id: profile.id,
          email: profile.email,
          fullName: profile.fullName,
          avatarUrl: profile.avatarUrl,
          createdAt: profile.createdAt,
        };
        this.updateCurrentUser(user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    const currentToken = this._token();
    if (currentToken && this.isTokenExpired(currentToken)) {
      this.logout();
      return null;
    }
    return currentToken;
  }

  getUser(): User | null {
    return this._currentUser();
  }

  private handleAuthSuccess(response: AuthResponse): void {
    const user: User = {
      id: response.id,
      email: response.email,
      fullName: response.fullName,
      avatarUrl: response.avatarUrl,
    };

    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));

    this._token.set(response.token);
    this._currentUser.set(user);
  }

  private updateCurrentUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._currentUser.set(user);
  }

  private loadTokenFromStorage(): string | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token && this.isTokenExpired(token)) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      return null;
    }
    return token;
  }

  private loadUserFromStorage(): User | null {
    const userJson = localStorage.getItem(USER_KEY);
    if (!userJson) {
      return null;
    }
    try {
      return JSON.parse(userJson) as User;
    } catch {
      localStorage.removeItem(USER_KEY);
      return null;
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) {
        return false;
      }
      const payloadBase64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const payloadJson = decodeURIComponent(
        atob(payloadBase64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      const payload = JSON.parse(payloadJson);
      if (payload.exp) {
        return Date.now() >= payload.exp * 1000;
      }
      return false;
    } catch {
      return false;
    }
  }
}
