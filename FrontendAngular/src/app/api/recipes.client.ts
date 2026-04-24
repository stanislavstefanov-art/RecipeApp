import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateRecipeRequest,
  CreateRecipeResponse,
  RecipeListItemDto,
} from './recipes.dto';

const API_BASE_URL = 'http://localhost:5117';

@Injectable({ providedIn: 'root' })
export class RecipesClient {
  private readonly http = inject(HttpClient);

  list(): Observable<RecipeListItemDto[]> {
    return this.http.get<RecipeListItemDto[]>(`${API_BASE_URL}/api/recipes`);
  }

  create(payload: CreateRecipeRequest): Observable<CreateRecipeResponse> {
    return this.http.post<CreateRecipeResponse>(`${API_BASE_URL}/api/recipes`, payload);
  }
}
