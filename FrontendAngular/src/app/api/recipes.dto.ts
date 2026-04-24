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
