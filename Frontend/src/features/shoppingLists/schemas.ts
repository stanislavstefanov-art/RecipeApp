import { z } from "zod";

export const shoppingListItemSchema = z.object({
  id: z.string().uuid(),
  productId: z.string().uuid(),
  productName: z.string(),
  quantity: z.number(),
  unit: z.string(),
  isPurchased: z.boolean(),
  notes: z.string().nullable().optional(),
  sourceType: z.number(),
  sourceReferenceId: z.string().uuid().nullable().optional(),
});

export const shoppingListDetailsSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  items: z.array(shoppingListItemSchema),
});

export const shoppingListListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  items: z.array(shoppingListItemSchema).optional().default([]),
});

export const createShoppingListSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
});

export const purchaseShoppingListItemSchema = z.object({
  amount: z.coerce.number().gt(0, "Amount must be greater than 0"),
  currency: z.string().min(1, "Currency is required").max(10),
  expenseDate: z.string().min(1, "Expense date is required"),
  description: z.string().max(500).nullable().optional(),
});

export const generateShoppingListFromMealPlanSchema = z.object({
  mealPlanId: z.string().uuid("Meal plan is required"),
  shoppingListId: z.string().uuid("Shopping list is required"),
});

export type ShoppingListItem = z.infer<typeof shoppingListItemSchema>;
export type ShoppingListDetails = z.infer<typeof shoppingListDetailsSchema>;
export type ShoppingListListItem = z.infer<typeof shoppingListListItemSchema>;

export type CreateShoppingListInput = z.input<typeof createShoppingListSchema>;
export type CreateShoppingListData = z.output<typeof createShoppingListSchema>;

export type PurchaseShoppingListItemInput = z.input<typeof purchaseShoppingListItemSchema>;
export type PurchaseShoppingListItemData = z.output<typeof purchaseShoppingListItemSchema>;

export type GenerateShoppingListFromMealPlanInput = z.input<typeof generateShoppingListFromMealPlanSchema>;
export type GenerateShoppingListFromMealPlanData = z.output<typeof generateShoppingListFromMealPlanSchema>;