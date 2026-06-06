export interface PantryItemDto {
  readonly id: string;
  readonly ingredientName: string;
  readonly notes: string | null;
  readonly createdAt: string;
}

export interface AddPantryItemRequest {
  readonly ingredientName: string;
  readonly notes?: string | null;
}
