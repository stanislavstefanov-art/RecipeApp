import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AddPantryItemRequest, PantryItemDto } from './pantry.dto';

import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class PantryClient {
  private readonly http = inject(HttpClient);

  list(): Observable<PantryItemDto[]> {
    return this.http.get<PantryItemDto[]>(`${API_BASE_URL}/api/pantry`);
  }

  add(payload: AddPantryItemRequest): Observable<PantryItemDto> {
    return this.http.post<PantryItemDto>(`${API_BASE_URL}/api/pantry`, payload);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/pantry/${id}`);
  }
}
