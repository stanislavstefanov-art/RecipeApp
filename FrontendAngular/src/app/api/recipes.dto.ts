export interface RecipeRatingDto {
  readonly id: string;
  readonly userId: string;
  readonly stars: number;
  readonly comment: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface RecipeListItemDto {
  readonly id: string;
  readonly name: string;
  readonly averageStars: number | null;
  readonly ratingCount: number;
  readonly imageUrl?: string | null;
  readonly recipeType: number;
  readonly isImported: boolean;
  readonly ingredientNames: readonly string[];
}

export interface CreateRecipeRequest {
  readonly name: string;
  readonly householdId: string;
  readonly recipeType: number;
  readonly isImported?: boolean;
  readonly difficultyLevel?: number | null;
}

export interface SetDifficultyRequest {
  readonly difficultyLevel: number | null;
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

export interface UpdateIngredientRequest {
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface AddStepRequest {
  readonly instruction: string;
}

export interface IngredientDto {
  readonly id: string;
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface RecipeStepDto {
  readonly id: string;
  readonly order: number;
  readonly instruction: string;
}

export interface RecipeVariationDto {
  readonly id: string;
  readonly name: string;
}

export interface RecipeDto {
  readonly id: string;
  readonly name: string;
  readonly ingredients: readonly IngredientDto[];
  readonly steps: readonly RecipeStepDto[];
  readonly variations?: readonly RecipeVariationDto[];
  readonly averageStars: number | null;
  readonly ratingCount: number;
  readonly ratings: readonly RecipeRatingDto[];
  readonly myRating: RecipeRatingDto | null;
  readonly imageUrl?: string | null;
  readonly difficultyLevel?: number | null;
  readonly recipeType: number;
}

export interface SetRecipeTypeRequest {
  readonly recipeType: number;
}

export interface CookingLogEntryDto {
  readonly id: string;
  readonly recipeId: string;
  readonly recipeName: string;
  readonly cookedOn: string;
  readonly servings: number;
  readonly notes: string | null;
  readonly createdAt: string;
}

export interface LogCookingRequest {
  readonly recipeId: string;
  readonly cookedOn: string;
  readonly servings: number;
  readonly notes?: string | null;
}

export interface RateRecipeRequest {
  readonly stars: number;
  readonly comment?: string | null;
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

export interface ImportRecipeRequest {
  readonly text: string;
}

export interface ImportedIngredientDto {
  readonly name: string;
  readonly quantity?: string;
  readonly unit?: string;
  readonly notes?: string;
}

export interface ImportedRecipeDto {
  readonly title?: string;
  readonly servings?: number;
  readonly ingredients: readonly ImportedIngredientDto[];
  readonly steps: readonly string[];
  readonly notes?: string;
  readonly confidence: number;
  readonly needsReview: boolean;
}
