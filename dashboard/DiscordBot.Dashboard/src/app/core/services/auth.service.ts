import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DiscordLoginResponse,
  ExchangeTokenRequest,
  ExchangeTokenResponse,
  UserProfile
} from '../models/auth.models';

const TOKEN_KEY = 'discord_bot_jwt';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getDiscordLoginUrl(): Observable<DiscordLoginResponse> {
    return this.http.get<DiscordLoginResponse>(`${this.baseUrl}/api/auth/discord/login`);
  }

  exchangeCode(code: string): Observable<ExchangeTokenResponse> {
    const body: ExchangeTokenRequest = { code };
    return this.http
      .post<ExchangeTokenResponse>(`${this.baseUrl}/api/auth/token`, body)
      .pipe(tap(res => this.setToken(res.accessToken)));
  }

  getCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/api/auth/me`);
  }

  logout(): void {
    this.clearToken();
  }
}
