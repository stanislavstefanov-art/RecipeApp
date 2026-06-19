import { z } from "zod";
import type { TFunction } from "i18next";

export const cookingLogPreparerSchema = z.object({
  personId: z.string().uuid(),
  personName: z.string(),
});

export const cookingLogEntrySchema = z.object({
  id: z.string().uuid(),
  recipeId: z.string().uuid(),
  recipeName: z.string(),
  cookedOn: z.string(),
  servings: z.number().int().min(1),
  notes: z.string().nullable().default(null),
  createdAt: z.string(),
  preparedBy: z.array(cookingLogPreparerSchema).default([]),
});
export type CookingLogEntry = z.infer<typeof cookingLogEntrySchema>;
export type CookingLogPreparer = z.infer<typeof cookingLogPreparerSchema>;

export const recipeVariationSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  notes: z.string().nullable().optional(),
  ingredientAdjustmentNotes: z.string().nullable().optional(),
});

export const recipeRatingSchema = z.object({
  id: z.string().uuid(),
  userId: z.string().uuid(),
  stars: z.number().int().min(1).max(5),
  comment: z.string().nullable(),
  createdAt: z.string(),
  updatedAt: z.string().nullable(),
});

export const recipeListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  averageStars: z.number().nullable().default(null),
  ratingCount: z.number().int().default(0),
});

export const recipeIngredientSchema = z.object({
  name: z.string(),
  quantity: z.number(),
  unit: z.string(),
});

export const recipeStepSchema = z.object({
  id: z.string().uuid(),
  order: z.number(),
  instruction: z.string(),
});

export const recipeDetailsSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  ingredients: z.array(recipeIngredientSchema),
  steps: z.array(recipeStepSchema),
  variations: z.array(recipeVariationSchema).default([]),
  averageStars: z.number().nullable().default(null),
  ratingCount: z.number().int().default(0),
  ratings: z.array(recipeRatingSchema).default([]),
  myRating: recipeRatingSchema.nullable().default(null),
});

export const createRecipeSchema = (t: TFunction) =>
  z.object({
    name: z.string().min(1, t("validation.required")).max(200, t("validation.maxLength", { max: 200 })),
  });

export const updateRecipeSchema = (t: TFunction) =>
  z.object({
    name: z.string().min(1, t("validation.required")).max(200, t("validation.maxLength", { max: 200 })),
  });

export const addIngredientSchema = (t: TFunction) =>
  z.object({
    name: z.string().min(1, t("validation.required")).max(200, t("validation.maxLength", { max: 200 })),
    quantity: z.coerce.number().gt(0, t("validation.greaterThan", { min: 0 })),
    unit: z.string().min(1, t("validation.required")).max(50, t("validation.maxLength", { max: 50 })),
  });

export const addStepSchema = (t: TFunction) =>
  z.object({
    instruction: z.string().min(1, t("validation.required")).max(1000, t("validation.maxLength", { max: 1000 })),
  });

export type RecipeVariation = z.infer<typeof recipeVariationSchema>;
export type RecipeRating = z.infer<typeof recipeRatingSchema>;
export type RecipeListItem = z.infer<typeof recipeListItemSchema>;
export type RecipeDetails = z.infer<typeof recipeDetailsSchema>;

export type CreateRecipeInput = z.input<ReturnType<typeof createRecipeSchema>>;
export type CreateRecipeData = z.output<ReturnType<typeof createRecipeSchema>>;

export type UpdateRecipeInput = z.input<ReturnType<typeof updateRecipeSchema>>;
export type UpdateRecipeData = z.output<ReturnType<typeof updateRecipeSchema>>;

export type AddIngredientInput = z.input<ReturnType<typeof addIngredientSchema>>;
export type AddIngredientData = z.output<ReturnType<typeof addIngredientSchema>>;

export type AddStepInput = z.input<ReturnType<typeof addStepSchema>>;
export type AddStepData = z.output<ReturnType<typeof addStepSchema>>;
