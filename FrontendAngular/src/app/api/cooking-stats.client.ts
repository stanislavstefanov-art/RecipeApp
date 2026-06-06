import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { RecipeCookingStatDto } from './cooking-stats.dto';

import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class CookingStatsClient {
  private readonly http = inject(HttpClient);

  list(): Observable<RecipeCookingStatDto[]> {
    return this.http.get<RecipeCookingStatDto[]>(`${API_BASE_URL}/api/cooking-log/stats`);
  }
}
