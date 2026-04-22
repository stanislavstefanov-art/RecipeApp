import { z } from "zod";

export const recipeVariationSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  notes: z.string().nullable().optional(),
  ingredientAdjustmentNotes: z.string().nullable().optional(),
});

export const recipeListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
});

export const recipeIngredientSchema = z.object({
  name: z.string(),
  quantity: z.number(),
  unit: z.string(),
});

export const recipeStepSchema = z.object({
  order: z.number(),
  instruction: z.string(),
});

export const recipeDetailsSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  ingredients: z.array(recipeIngredientSchema),
  steps: z.array(recipeStepSchema),
  variations: z.array(recipeVariationSchema).default([]),
});

export const createRecipeSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
});

export const updateRecipeSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
});

export const addIngredientSchema = z.object({
  name: z.string().min(1, "Ingredient name is required").max(200),
  quantity: z.coerce.number().gt(0, "Quantity must be greater than 0"),
  unit: z.string().min(1, "Unit is required").max(50),
});

export const addStepSchema = z.object({
  instruction: z.string().min(1, "Instruction is required").max(1000),
});

export type RecipeVariation = z.infer<typeof recipeVariationSchema>;
export type RecipeListItem = z.infer<typeof recipeListItemSchema>;
export type RecipeDetails = z.infer<typeof recipeDetailsSchema>;

export type CreateRecipeInput = z.input<typeof createRecipeSchema>;
export type CreateRecipeData = z.output<typeof createRecipeSchema>;

export type UpdateRecipeInput = z.input<typeof updateRecipeSchema>;
export type UpdateRecipeData = z.output<typeof updateRecipeSchema>;

export type AddIngredientInput = z.input<typeof addIngredientSchema>;
export type AddIngredientData = z.output<typeof addIngredientSchema>;

export type AddStepInput = z.input<typeof addStepSchema>;
export type AddStepData = z.output<typeof addStepSchema>;