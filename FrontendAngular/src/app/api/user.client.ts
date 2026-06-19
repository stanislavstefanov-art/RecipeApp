import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

export interface UserProfileDto {
  readonly userId: string;
  readonly email: string;
  readonly displayName: string;
  readonly personId: string | null;
}

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class UserClient {
  private readonly http = inject(HttpClient);

  getProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${API_BASE_URL}/api/user/me`);
  }

  clearAllData(): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/user/data`);
  }
}
