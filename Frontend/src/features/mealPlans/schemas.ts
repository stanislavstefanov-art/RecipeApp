import { z } from "zod";

export const mealPlanAssignmentSchema = z.object({
  personId: z.string().uuid(),
  personName: z.string(),
  assignedRecipeId: z.string().uuid(),
  assignedRecipeName: z.string(),
  recipeVariationId: z.string().uuid().nullable().optional(),
  recipeVariationName: z.string().nullable().optional(),
  portionMultiplier: z.number(),
  notes: z.string().nullable().optional(),
});

export const mealPlanEntrySchema = z.object({
  id: z.string().uuid(),
  baseRecipeId: z.string().uuid(),
  baseRecipeName: z.string(),
  plannedDate: z.string(),
  mealType: z.number(),
  scope: z.number(),
  assignments: z.array(mealPlanAssignmentSchema),
});

export const mealPlanDetailsSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  householdId: z.string().uuid(),
  householdName: z.string(),
  entries: z.array(mealPlanEntrySchema),
});

export const mealPlanListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  householdId: z.string().uuid(),
  householdName: z.string(),
  entryCount: z.number(),
});

export const updateMealPlanAssignmentSchema = z.object({
  personId: z.string().uuid(),
  assignedRecipeId: z.string().uuid(),
  recipeVariationId: z.string().uuid().nullable(),
  portionMultiplier: z.coerce.number().gt(0, "Portion must be greater than 0"),
  notes: z.string().max(1000).nullable().optional(),
});

export const suggestMealPlanInputSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
  householdId: z.string().uuid("Household is required"),
  startDate: z.string().min(1, "Start date is required"),
  numberOfDays: z.coerce.number().int().min(1).max(31),
  mealTypes: z.array(z.coerce.number()).min(1, "Select at least one meal type"),
});

export const suggestMealPlanAssignmentSchema = z.object({
  personId: z.string().uuid(),
  assignedRecipeId: z.string().uuid(),
  recipeVariationId: z.string().uuid().nullable().optional(),
  portionMultiplier: z.number(),
  notes: z.string().nullable().optional(),
});

export const suggestMealPlanEntrySchema = z.object({
  baseRecipeId: z.string().uuid(),
  plannedDate: z.string(),
  mealType: z.number(),
  scope: z.number(),
  assignments: z.array(suggestMealPlanAssignmentSchema),
});

export const mealPlanSuggestionSchema = z.object({
  name: z.string(),
  entries: z.array(suggestMealPlanEntrySchema),
  confidence: z.number(),
  needsReview: z.boolean(),
  notes: z.string().nullable().optional(),
});

export const acceptMealPlanSuggestionInputSchema = z.object({
  name: z.string(),
  householdId: z.string().uuid(),
  entries: z.array(
    z.object({
      baseRecipeId: z.string().uuid(),
      plannedDate: z.string(),
      mealType: z.number(),
      scope: z.number(),
      assignments: z.array(
        z.object({
          personId: z.string().uuid(),
          assignedRecipeId: z.string().uuid(),
          recipeVariationId: z.string().uuid().nullable().optional(),
          portionMultiplier: z.number(),
          notes: z.string().nullable().optional(),
        }),
      ),
    }),
  ),
});

export const acceptMealPlanSuggestionResponseSchema = z.object({
  mealPlanId: z.string().uuid(),
  name: z.string(),
});

export type MealPlanAssignment = z.infer<typeof mealPlanAssignmentSchema>;
export type MealPlanEntry = z.infer<typeof mealPlanEntrySchema>;
export type MealPlanDetails = z.infer<typeof mealPlanDetailsSchema>;
export type MealPlanListItem = z.infer<typeof mealPlanListItemSchema>;

export type UpdateMealPlanAssignmentInput = z.input<typeof updateMealPlanAssignmentSchema>;
export type UpdateMealPlanAssignmentData = z.output<typeof updateMealPlanAssignmentSchema>;

export type SuggestMealPlanInput = z.input<typeof suggestMealPlanInputSchema>;
export type SuggestMealPlanData = z.output<typeof suggestMealPlanInputSchema>;

export type MealPlanSuggestion = z.infer<typeof mealPlanSuggestionSchema>;
export type AcceptMealPlanSuggestionInput = z.infer<typeof acceptMealPlanSuggestionInputSchema>;
export type AcceptMealPlanSuggestionResponse = z.infer<typeof acceptMealPlanSuggestionResponseSchema>;