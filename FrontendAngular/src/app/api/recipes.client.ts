import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddIngredientRequest,
  AddStepRequest,
  CookingLogEntryDto,
  CreateRecipeRequest,
  CreateRecipeResponse,
  ImportRecipeRequest,
  ImportedRecipeDto,
  IngredientSubstitutionSuggestionDto,
  LogCookingRequest,
  MoveStepRequest,
  RateRecipeRequest,
  RecipeDto,
  RecipeListItemDto,
  RecipeRatingDto,
  SetDifficultyRequest,
  SetAppropriateForRequest,
  SetMealsPerCookRequest,
  SetRecipeOriginRequest,
  SetRecipeTypeRequest,
  SuggestSubstitutionsRequest,
  UpdateIngredientRequest,
  UpdateRecipeRequest,
  UpdateStepRequest,
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

  setDifficulty(recipeId: string, payload: SetDifficultyRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/recipes/${recipeId}/difficulty`, payload);
  }

  setRecipeType(recipeId: string, payload: SetRecipeTypeRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/recipes/${recipeId}/type`, payload);
  }

  setOrigin(recipeId: string, payload: SetRecipeOriginRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/recipes/${recipeId}/origin`, payload);
  }

  setMealsPerCook(recipeId: string, payload: SetMealsPerCookRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/recipes/${recipeId}/meals-per-cook`, payload);
  }

  setAppropriateFor(recipeId: string, payload: SetAppropriateForRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/recipes/${recipeId}/appropriate-for`, payload);
  }

  updateIngredient(recipeId: string, ingredientId: string, payload: UpdateIngredientRequest): Observable<void> {
    return this.http.put<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/ingredients/${ingredientId}`,
      payload,
    );
  }

  removeIngredient(recipeId: string, ingredientId: string): Observable<void> {
    return this.http.delete<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/ingredients/${ingredientId}`,
    );
  }

  removeStep(recipeId: string, stepId: string): Observable<void> {
    return this.http.delete<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/steps/${stepId}`,
    );
  }

  updateStep(recipeId: string, stepId: string, payload: UpdateStepRequest): Observable<void> {
    return this.http.put<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/steps/${stepId}`,
      payload,
    );
  }

  moveStep(recipeId: string, stepId: string, payload: MoveStepRequest): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/recipes/${recipeId}/steps/${stepId}/move`,
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

  rate(recipeId: string, payload: RateRecipeRequest): Observable<RecipeRatingDto> {
    return this.http.post<RecipeRatingDto>(
      `${API_BASE_URL}/api/recipes/${recipeId}/ratings`,
      payload,
    );
  }

  deleteRating(recipeId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/recipes/${recipeId}/ratings`);
  }

  logCooking(payload: LogCookingRequest): Observable<CookingLogEntryDto> {
    return this.http.post<CookingLogEntryDto>(`${API_BASE_URL}/api/cooking-log`, payload);
  }

  uploadImage(recipeId: string, file: File): Observable<{ imageUrl: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(`${API_BASE_URL}/api/recipes/${recipeId}/image`, form);
  }

  deleteImage(recipeId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/recipes/${recipeId}/image`);
  }

  deleteCookingEntry(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/cooking-log/${id}`);
  }

  getCookingHistory(recipeId: string): Observable<CookingLogEntryDto[]> {
    return this.http.get<CookingLogEntryDto[]>(
      `${API_BASE_URL}/api/cooking-log/recipe/${recipeId}`,
    );
  }
}
