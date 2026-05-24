import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CreateUnitRequest, MeasurementUnitDto } from './units.dto';
import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class UnitsClient {
  private readonly http = inject(HttpClient);

  list(): Observable<MeasurementUnitDto[]> {
    return this.http.get<MeasurementUnitDto[]>(`${API_BASE_URL}/api/units`);
  }

  create(payload: CreateUnitRequest): Observable<MeasurementUnitDto> {
    return this.http.post<MeasurementUnitDto>(`${API_BASE_URL}/api/units`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/units/${id}`);
  }
}
