import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'auth/login', component: class {} },
          { path: 'workspaces', component: class {} },
        ]),
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should login and persist user and token', () => {
    const mockRequest: LoginRequest = {
      email: 'test@example.com',
      password: 'password123',
    };
    const mockResponse: AuthResponse = {
      id: '123',
      email: 'test@example.com',
      fullName: 'Test User',
      token: 'jwt.token.mock',
      avatarUrl: null,
    };

    service.login(mockRequest).subscribe((res) => {
      expect(res).toEqual(mockResponse);
      expect(service.currentUser()?.email).toBe('test@example.com');
      expect(service.getToken()).toBe('jwt.token.mock');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should register and persist user and token', () => {
    const mockRequest: RegisterRequest = {
      fullName: 'Test User',
      email: 'test@example.com',
      password: 'password123',
    };
    const mockResponse: AuthResponse = {
      id: '123',
      email: 'test@example.com',
      fullName: 'Test User',
      token: 'jwt.token.mock',
      avatarUrl: null,
    };

    service.register(mockRequest).subscribe((res) => {
      expect(res).toEqual(mockResponse);
      expect(service.currentUser()?.fullName).toBe('Test User');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should clear data on logout', () => {
    localStorage.setItem('trello_auth_token', 'token');
    service.logout();
    expect(service.getToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });
});
