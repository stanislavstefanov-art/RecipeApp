import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CreatePersonRequest, CreatePersonResponse, PersonDto } from './persons.dto';

const API_BASE_URL = 'http://localhost:5117';

@Injectable({ providedIn: 'root' })
export class PersonsClient {
  private readonly http = inject(HttpClient);

  list(): Observable<PersonDto[]> {
    return this.http.get<PersonDto[]>(`${API_BASE_URL}/api/persons`);
  }

  create(payload: CreatePersonRequest): Observable<CreatePersonResponse> {
    return this.http.post<CreatePersonResponse>(`${API_BASE_URL}/api/persons`, payload);
  }
}
