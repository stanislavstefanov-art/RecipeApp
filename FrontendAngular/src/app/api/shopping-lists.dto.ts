export interface ShoppingListItemDto {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unit: string;
  isPurchased: boolean;
  notes: string | null;
  sourceType: number;
  sourceReferenceId: string | null;
}

export interface ShoppingListDto {
  id: string;
  name: string;
  items: ShoppingListItemDto[];
}

export interface CreateShoppingListRequest {
  name: string;
  householdId: string;
}

export interface CreateShoppingListResponse {
  id: string;
  name: string;
}

export interface ShoppingListDetailsDto {
  id: string;
  name: string;
  items: ShoppingListDetailsItemDto[];
}

export interface ShoppingListItemRecipeSourceDto {
  recipeId: string;
  recipeName: string;
  portions: number;
}

export interface ShoppingListDetailsItemDto {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unit: string;
  isPurchased: boolean;
  notes: string | null;
  sourceType: number;
  sourceReferenceId: string | null;
  recipeSources: ShoppingListItemRecipeSourceDto[];
}

export interface AddManualItemRequest {
  productName: string;
  quantity: number;
  unit: string;
}

export interface PurchaseWithExpenseRequest {
  amount: number;
  currency: string;
  expenseDate: string;
  description?: string;
}
