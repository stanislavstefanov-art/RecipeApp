export interface MealPlanListItemDto {
  readonly id: string;
  readonly name: string;
  readonly householdId: string;
  readonly householdName: string;
  readonly entryCount: number;
}

export interface CreateMealPlanRequest {
  readonly name: string;
  readonly householdId: string;
}

export interface CreateMealPlanResponse {
  readonly id: string;
  readonly name: string;
}

export interface MealPlanSuggestionAssignmentDto {
  readonly personId: string;
  readonly assignedRecipeId: string;
  readonly recipeVariationId: string | null;
  readonly portionMultiplier: number;
  readonly notes: string | null;
}

export interface MealPlanSuggestionEntryDto {
  readonly baseRecipeId: string;
  readonly plannedDate: string;
  readonly mealType: number;
  readonly scope: number;
  readonly assignments: readonly MealPlanSuggestionAssignmentDto[];
}

export interface MealPlanSuggestionDto {
  readonly name: string;
  readonly entries: readonly MealPlanSuggestionEntryDto[];
  readonly confidence: number;
  readonly needsReview: boolean;
  readonly notes?: string;
}

export interface SuggestMealPlanRequest {
  readonly name: string;
  readonly householdId: string;
  readonly startDate: string;
  readonly numberOfDays: number;
  readonly mealTypes: readonly number[];
}

export interface AcceptMealPlanSuggestionAssignmentRequest {
  readonly personId: string;
  readonly assignedRecipeId: string;
  readonly recipeVariationId: string | null;
  readonly portionMultiplier: number;
  readonly notes: string | null;
}

export interface AcceptMealPlanSuggestionEntryRequest {
  readonly baseRecipeId: string;
  readonly plannedDate: string;
  readonly mealType: number;
  readonly scope: number;
  readonly assignments: readonly AcceptMealPlanSuggestionAssignmentRequest[];
}

export interface AcceptMealPlanSuggestionRequest {
  readonly name: string;
  readonly householdId: string;
  readonly entries: readonly AcceptMealPlanSuggestionEntryRequest[];
}

export interface AcceptMealPlanSuggestionResponse {
  readonly mealPlanId: string;
  readonly name: string;
}

export interface MealPlanEntryAssignmentDto {
  readonly personId: string;
  readonly personName: string;
  readonly assignedRecipeId: string;
  readonly assignedRecipeName: string;
  readonly recipeVariationId: string | null;
  readonly recipeVariationName: string | null;
  readonly portionMultiplier: number;
  readonly notes: string | null;
}

export interface MealPlanEntryDto {
  readonly id: string;
  readonly baseRecipeId: string;
  readonly baseRecipeName: string;
  readonly plannedDate: string;
  readonly mealType: number;
  readonly scope: number;
  readonly assignments: readonly MealPlanEntryAssignmentDto[];
}

export interface MealPlanDetailsDto {
  readonly id: string;
  readonly name: string;
  readonly householdId: string;
  readonly householdName: string;
  readonly entries: readonly MealPlanEntryDto[];
}

export interface UpdateMealPlanPersonAssignmentRequest {
  readonly personId: string;
  readonly assignedRecipeId: string;
  readonly recipeVariationId: string | null;
  readonly portionMultiplier: number;
  readonly notes: string | null;
}
