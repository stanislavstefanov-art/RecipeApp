import { apiClient } from "./client";
import {
  acceptMealPlanSuggestionResponseSchema,
  mealPlanDetailsSchema,
  mealPlanListItemSchema,
  mealPlanSuggestionSchema,
  type AcceptMealPlanSuggestionInput,
  type SuggestMealPlanInput,
  type UpdateMealPlanAssignmentInput,
} from "../features/mealPlans/schemas";

export async function getMealPlans() {
  const res = await apiClient.get("/api/meal-plans");
  return mealPlanListItemSchema.array().parse(res.data);
}

export async function getMealPlan(mealPlanId: string) {
  const res = await apiClient.get(`/api/meal-plans/${mealPlanId}`);
  return mealPlanDetailsSchema.parse(res.data);
}

export async function updateMealPlanAssignment(
  mealPlanId: string,
  mealPlanEntryId: string,
  input: UpdateMealPlanAssignmentInput,
) {
  await apiClient.put(
    `/api/meal-plans/${mealPlanId}/entries/${mealPlanEntryId}/assignments`,
    input,
  );
}

export async function suggestMealPlan(input: SuggestMealPlanInput) {
  const res = await apiClient.post("/api/meal-plans/suggest", input);
  return mealPlanSuggestionSchema.parse(res.data);
}

export async function acceptMealPlanSuggestion(
  input: AcceptMealPlanSuggestionInput,
) {
  const res = await apiClient.post("/api/meal-plans/accept-suggestion", input);
  return acceptMealPlanSuggestionResponseSchema.parse(res.data);
}