import { z } from "zod";
import type { TFunction } from "i18next";

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

export const createShoppingListSchema = (t: TFunction) =>
  z.object({
    name: z.string().min(1, t("validation.required")).max(200, t("validation.maxLength", { max: 200 })),
  });

export const purchaseShoppingListItemSchema = (t: TFunction) =>
  z.object({
    amount: z.coerce.number().gt(0, t("validation.greaterThan", { min: 0 })),
    currency: z.string().min(1, t("validation.required")).max(10, t("validation.maxLength", { max: 10 })),
    expenseDate: z.string().min(1, t("validation.required")),
    description: z.string().max(500, t("validation.maxLength", { max: 500 })).nullable().optional(),
  });

export const generateShoppingListFromMealPlanSchema = (t: TFunction) =>
  z.object({
    mealPlanId: z.string().uuid(t("validation.required")),
    shoppingListId: z.string().uuid(t("validation.required")),
  });

export type ShoppingListItem = z.infer<typeof shoppingListItemSchema>;
export type ShoppingListDetails = z.infer<typeof shoppingListDetailsSchema>;
export type ShoppingListListItem = z.infer<typeof shoppingListListItemSchema>;

export type CreateShoppingListInput = z.input<ReturnType<typeof createShoppingListSchema>>;
export type CreateShoppingListData = z.output<ReturnType<typeof createShoppingListSchema>>;

export type PurchaseShoppingListItemInput = z.input<ReturnType<typeof purchaseShoppingListItemSchema>>;
export type PurchaseShoppingListItemData = z.output<ReturnType<typeof purchaseShoppingListItemSchema>>;

export type GenerateShoppingListFromMealPlanInput = z.input<ReturnType<typeof generateShoppingListFromMealPlanSchema>>;
export type GenerateShoppingListFromMealPlanData = z.output<ReturnType<typeof generateShoppingListFromMealPlanSchema>>;
