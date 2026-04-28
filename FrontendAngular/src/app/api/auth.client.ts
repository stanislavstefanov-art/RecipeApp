import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthSessionDto, MeResponseDto } from './auth.dto';

const API = environment.apiBaseUrl;

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthClient {
  private readonly http = inject(HttpClient);

  register(request: RegisterRequest): Observable<AuthSessionDto> {
    return this.http.post<AuthSessionDto>(`${API}/api/auth/register`, request);
  }

  login(request: LoginRequest): Observable<AuthSessionDto> {
    return this.http.post<AuthSessionDto>(`${API}/api/auth/login`, request);
  }

  entraExchange(idToken: string): Observable<AuthSessionDto> {
    return this.http.post<AuthSessionDto>(`${API}/api/auth/entra/exchange`, { idToken });
  }

  me(): Observable<MeResponseDto> {
    return this.http.get<MeResponseDto>(`${API}/api/auth/me`);
  }
}
