export interface ShoppingListItemDto {
  id: string;
  name: string;
  quantity?: string;
  sourceType: number;
  isPending: boolean;
  isPurchased: boolean;
}

export interface ShoppingListDto {
  id: string;
  name: string;
  items: ShoppingListItemDto[];
}

export interface CreateShoppingListRequest {
  name: string;
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

export interface ShoppingListDetailsItemDto {
  id: string;
  name: string;
  quantity?: string;
  sourceType: number;
  isPending: boolean;
  isPurchased: boolean;
}

export interface PurchaseWithExpenseRequest {
  amount: number;
  currency: string;
  expenseDate: string;
  description?: string;
}
