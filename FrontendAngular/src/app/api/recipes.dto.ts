export interface RecipeListItemDto {
  readonly id: string;
  readonly name: string;
}

export interface CreateRecipeRequest {
  readonly name: string;
}

export interface CreateRecipeResponse {
  readonly id: string;
}

export interface UpdateRecipeRequest {
  readonly name: string;
}

export interface AddIngredientRequest {
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface AddStepRequest {
  readonly instruction: string;
}

export interface IngredientDto {
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface RecipeStepDto {
  readonly order: number;
  readonly instruction: string;
}

export interface RecipeDto {
  readonly id: string;
  readonly name: string;
  readonly ingredients: readonly IngredientDto[];
  readonly steps: readonly RecipeStepDto[];
}

export interface SuggestSubstitutionsRequest {
  readonly ingredientName: string;
  readonly recipeContext?: string;
  readonly dietaryGoal?: string;
}

export interface IngredientSubstituteDto {
  readonly name: string;
  readonly reason: string;
  readonly quantityAdjustment?: string;
  readonly isDirectReplacement: boolean;
}

export interface IngredientSubstitutionSuggestionDto {
  readonly originalIngredient: string;
  readonly substitutes: readonly IngredientSubstituteDto[];
  readonly confidence: number;
  readonly needsReview: boolean;
  readonly notes?: string;
}
