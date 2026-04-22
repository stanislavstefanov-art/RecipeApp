import { apiClient } from "./client";
import {
  recipeDetailsSchema,
  recipeListItemSchema,
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

export async function deleteRecipe(recipeId: string) {
  await apiClient.delete(`/api/recipes/${recipeId}`);
}