import { apiClient } from "./client";
import {
  shoppingListDetailsSchema,
  shoppingListListItemSchema,
  type CreateShoppingListInput,
  type PurchaseShoppingListItemInput,
} from "../features/shoppingLists/schemas";

export async function getShoppingLists() {
  const res = await apiClient.get("/api/shopping-lists");
  return shoppingListListItemSchema.array().parse(res.data);
}

export async function getShoppingList(shoppingListId: string) {
  const res = await apiClient.get(`/api/shopping-lists/${shoppingListId}`);
  return shoppingListDetailsSchema.parse(res.data);
}

export async function createShoppingList(input: CreateShoppingListInput) {
  const res = await apiClient.post("/api/shopping-lists", input);
  return shoppingListListItemSchema.pick({ id: true, name: true }).extend({
    items: shoppingListListItemSchema.shape.items.optional().default([]),
  }).parse(res.data);
}

export async function generateShoppingListFromMealPlan(
  mealPlanId: string,
  shoppingListId: string,
) {
  await apiClient.post(`/api/meal-plans/${mealPlanId}/shopping-lists/${shoppingListId}`);
}

export async function regenerateShoppingListFromMealPlan(
  mealPlanId: string,
  shoppingListId: string,
) {
  await apiClient.post(
    `/api/meal-plans/${mealPlanId}/shopping-lists/${shoppingListId}/regenerate`,
  );
}

export async function markShoppingListItemPending(
  shoppingListId: string,
  shoppingListItemId: string,
) {
  await apiClient.post(
    `/api/shopping-lists/${shoppingListId}/items/${shoppingListItemId}/pending`,
  );
}

export async function purchaseShoppingListItemWithExpense(
  shoppingListId: string,
  shoppingListItemId: string,
  input: PurchaseShoppingListItemInput,
) {
  await apiClient.post(
    `/api/shopping-lists/${shoppingListId}/items/${shoppingListItemId}/purchase-with-expense`,
    {
      ...input,
      description: input.description || null,
    },
  );
}