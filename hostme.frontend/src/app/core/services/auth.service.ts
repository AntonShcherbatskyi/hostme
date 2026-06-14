import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { AppConfigService } from './app-config.service';
import {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  UserDto,
  RefreshTokenRequest,
  RevokeTokenRequest,
} from '../models/api.models';

const ACCESS_TOKEN_KEY = 'hostme_access_token';
const REFRESH_TOKEN_KEY = 'hostme_refresh_token';
const USER_KEY = 'hostme_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _currentUser = signal<UserDto | null>(this.loadUser());
  private readonly _isAuthenticated = computed(() => this._currentUser() !== null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = this._isAuthenticated;

  constructor(private http: HttpClient, private router: Router, private config: AppConfigService) {}

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http
      .post<ApiResponse<LoginResponse>>(`${this.config.apiUrl}/auth/login`, request)
      .pipe(
        tap((res) => {
          if (!res.isError && res.data) {
            this.storeSession(res.data);
          }
        }),
        catchError((err) => throwError(() => err))
      );
  }

  register(request: RegisterRequest): Observable<ApiResponse<RegisterResponse>> {
    return this.http
      .post<ApiResponse<RegisterResponse>>(`${this.config.apiUrl}/auth/register`, request)
      .pipe(catchError((err) => throwError(() => err)));
  }

  refreshToken(): Observable<ApiResponse<LoginResponse>> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }
    const body: RefreshTokenRequest = { refreshToken };
    return this.http
      .post<ApiResponse<LoginResponse>>(`${this.config.apiUrl}/auth/refresh`, body)
      .pipe(
        tap((res) => {
          if (!res.isError && res.data) {
            this.storeSession(res.data);
          }
        }),
        catchError((err) => throwError(() => err))
      );
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      const body: RevokeTokenRequest = { refreshToken };
      this.http
        .post<ApiResponse<null>>(`${this.config.apiUrl}/auth/revoke`, body)
        .subscribe({ error: () => {} });
    }
    this.clearSession();
    this.router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  private storeSession(data: LoginResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, data.token);
    localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(data.user));
    this._currentUser.set(data.user);
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._currentUser.set(null);
  }

  private loadUser(): UserDto | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as UserDto) : null;
    } catch {
      return null;
    }
  }
}
