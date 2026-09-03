export interface User {
  id: string;
  email: string;
  fullName: string;
  avatarUrl?: string | null;
  createdAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  fullName: string;
  password: string;
}

export interface GoogleAuthRequest {
  idToken: string;
}

export interface GitHubAuthRequest {
  code: string;
}

export interface AuthResponse {
  id: string;
  email: string;
  fullName: string;
  token: string;
  avatarUrl?: string | null;
}

export interface UserProfileResponse {
  id: string;
  email: string;
  fullName: string;
  avatarUrl?: string | null;
  createdAt: string;
}

export interface ApiErrorResponse {
  type?: string;
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}
