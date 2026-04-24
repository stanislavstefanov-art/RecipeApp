import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateHouseholdRequest,
  CreateHouseholdResponse,
  HouseholdDetailsDto,
  HouseholdListItemDto,
} from './households.dto';

const API_BASE_URL = 'http://localhost:5117';

@Injectable({ providedIn: 'root' })
export class HouseholdsClient {
  private readonly http = inject(HttpClient);

  list(): Observable<HouseholdListItemDto[]> {
    return this.http.get<HouseholdListItemDto[]>(`${API_BASE_URL}/api/households`);
  }

  get(id: string): Observable<HouseholdDetailsDto> {
    return this.http.get<HouseholdDetailsDto>(`${API_BASE_URL}/api/households/${id}`);
  }

  create(payload: CreateHouseholdRequest): Observable<CreateHouseholdResponse> {
    return this.http.post<CreateHouseholdResponse>(`${API_BASE_URL}/api/households`, payload);
  }

  addMember(householdId: string, personId: string): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/households/${householdId}/members/${personId}`,
      null,
    );
  }
}
