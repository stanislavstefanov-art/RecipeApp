import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class UserClient {
  private readonly http = inject(HttpClient);

  clearAllData(): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/user/data`);
  }
}
