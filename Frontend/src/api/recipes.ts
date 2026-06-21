import { apiClient } from "./client";
import {
  cookingLogEntrySchema,
  recipeDetailsSchema,
  recipeListItemSchema,
  recipeRatingSchema,
  type AddIngredientInput,
  type AddStepInput,
  type CreateRecipeInput,
  type UpdateRecipeInput,
} from "../features/recipes/schemas";

export async function getRecipes() {
  const res = await apiClient.get("/api/recipes");
  return recipeListItemSchema.array().parse(res.data);
}

export async function getRecipe(recipeId: string) {
  const res = await apiClient.get(`/api/recipes/${recipeId}`);
  return recipeDetailsSchema.parse(res.data);
}

export async function createRecipe(input: CreateRecipeInput) {
  const res = await apiClient.post("/api/recipes", input);
  return recipeListItemSchema.parse(res.data);
}

export async function updateRecipe(recipeId: string, input: UpdateRecipeInput) {
  await apiClient.put(`/api/recipes/${recipeId}`, input);
}

export async function addIngredient(recipeId: string, input: AddIngredientInput) {
  await apiClient.post(`/api/recipes/${recipeId}/ingredients`, input);
}

export async function addStep(recipeId: string, input: AddStepInput) {
  await apiClient.post(`/api/recipes/${recipeId}/steps`, input);
}

export async function updateStep(recipeId: string, stepId: string, instruction: string) {
  await apiClient.put(`/api/recipes/${recipeId}/steps/${stepId}`, { instruction });
}

export async function moveStep(recipeId: string, stepId: string, direction: "up" | "down") {
  await apiClient.post(`/api/recipes/${recipeId}/steps/${stepId}/move`, { direction });
}

export async function setSeasonality(recipeId: string, seasonality: number) {
  await apiClient.put(`/api/recipes/${recipeId}/seasonality`, { seasonality });
}

export async function deleteRecipe(recipeId: string) {
  await apiClient.delete(`/api/recipes/${recipeId}`);
}

export async function rateRecipe(recipeId: string, stars: number, comment?: string | null) {
  const res = await apiClient.post(`/api/recipes/${recipeId}/ratings`, { stars, comment });
  return recipeRatingSchema.parse(res.data);
}

export async function deleteRecipeRating(recipeId: string) {
  await apiClient.delete(`/api/recipes/${recipeId}/ratings`);
}

export async function logCooking(
  recipeId: string,
  cookedOn: string,
  servings: number,
  notes?: string | null,
  preparedByPersonIds?: string[],
) {
  const res = await apiClient.post("/api/cooking-log", {
    recipeId,
    cookedOn,
    servings,
    notes,
    preparedByPersonIds: preparedByPersonIds?.length ? preparedByPersonIds : undefined,
  });
  return cookingLogEntrySchema.parse(res.data);
}

export async function deleteCookingEntry(id: string) {
  await apiClient.delete(`/api/cooking-log/${id}`);
}

export async function getCookingHistory(recipeId: string) {
  const res = await apiClient.get(`/api/cooking-log/recipe/${recipeId}`);
  return cookingLogEntrySchema.array().parse(res.data);
}