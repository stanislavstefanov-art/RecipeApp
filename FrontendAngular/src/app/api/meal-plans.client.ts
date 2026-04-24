import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AcceptMealPlanSuggestionRequest,
  AcceptMealPlanSuggestionResponse,
  CreateMealPlanRequest,
  CreateMealPlanResponse,
  MealPlanDetailsDto,
  MealPlanListItemDto,
  MealPlanSuggestionDto,
  SuggestMealPlanRequest,
  UpdateMealPlanPersonAssignmentRequest,
} from './meal-plans.dto';

const API_BASE_URL = 'http://localhost:5117';

@Injectable({ providedIn: 'root' })
export class MealPlansClient {
  private readonly http = inject(HttpClient);

  list(): Observable<MealPlanListItemDto[]> {
    return this.http.get<MealPlanListItemDto[]>(`${API_BASE_URL}/api/meal-plans`);
  }

  get(id: string): Observable<MealPlanDetailsDto> {
    return this.http.get<MealPlanDetailsDto>(`${API_BASE_URL}/api/meal-plans/${id}`);
  }

  create(payload: CreateMealPlanRequest): Observable<CreateMealPlanResponse> {
    return this.http.post<CreateMealPlanResponse>(`${API_BASE_URL}/api/meal-plans`, payload);
  }

  suggest(payload: SuggestMealPlanRequest): Observable<MealPlanSuggestionDto> {
    return this.http.post<MealPlanSuggestionDto>(
      `${API_BASE_URL}/api/meal-plans/suggest`,
      payload,
    );
  }

  acceptSuggestion(payload: AcceptMealPlanSuggestionRequest): Observable<AcceptMealPlanSuggestionResponse> {
    return this.http.post<AcceptMealPlanSuggestionResponse>(
      `${API_BASE_URL}/api/meal-plans/accept-suggestion`,
      payload,
    );
  }

  generateFromMealPlan(mealPlanId: string, shoppingListId: string): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/meal-plans/${mealPlanId}/shopping-lists/${shoppingListId}`,
      null,
    );
  }

  regenerateFromMealPlan(mealPlanId: string, shoppingListId: string): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/meal-plans/${mealPlanId}/shopping-lists/${shoppingListId}/regenerate`,
      null,
    );
  }

  updateAssignment(
    mealPlanId: string,
    mealPlanEntryId: string,
    payload: UpdateMealPlanPersonAssignmentRequest,
  ): Observable<void> {
    return this.http.put<void>(
      `${API_BASE_URL}/api/meal-plans/${mealPlanId}/entries/${mealPlanEntryId}/assignments`,
      payload,
    );
  }
}
