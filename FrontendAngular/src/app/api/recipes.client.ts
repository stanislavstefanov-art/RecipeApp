import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddIngredientRequest,
  AddStepRequest,
  CreateRecipeRequest,
  CreateRecipeResponse,
  ImportRecipeRequest,
  ImportedRecipeDto,
  IngredientSubstitutionSuggestionDto,
  RecipeDto,
  RecipeListItemDto,
  SuggestSubstitutionsRequest,
  UpdateRecipeRequest,
} from './recipes.dto';

import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class RecipesClient {
  private readonly http = inject(HttpClient);

  list(): Observable<RecipeListItemDto[]> {
    return this.http.get<RecipeListItemDto[]>(`${API_BASE_URL}/api/recipes`);
  }

  get(id: string): Observable<RecipeDto> {
    return this.http.get<RecipeDto>(`${API_BASE_URL}/api/recipes/${id}`);
  }

  create(payload: CreateRecipeRequest): Observable<CreateRecipeResponse> {
    return this.http.post<CreateRecipeResponse>(`${API_BASE_URL}/api/recipes`, payload);
  }

  updateName(id: string, payload: UpdateRecipeRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/recipes/${id}`, payload);
  }

  addIngredient(recipeId: string, payload: AddIngredientRequest): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/ingredients`,
      payload,
    );
  }

  addStep(recipeId: string, payload: AddStepRequest): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/steps`,
      payload,
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/recipes/${id}`);
  }

  suggestSubstitutions(
    payload: SuggestSubstitutionsRequest,
  ): Observable<IngredientSubstitutionSuggestionDto> {
    return this.http.post<IngredientSubstitutionSuggestionDto>(
      `${API_BASE_URL}/api/recipes/suggest-substitutions`,
      payload,
    );
  }

  importFromText(payload: ImportRecipeRequest): Observable<ImportedRecipeDto> {
    return this.http.post<ImportedRecipeDto>(
      `${API_BASE_URL}/api/recipes/import`,
      payload,
    );
  }
}
