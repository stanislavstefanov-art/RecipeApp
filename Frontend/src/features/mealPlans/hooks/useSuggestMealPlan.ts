import { useMutation } from "@tanstack/react-query";
import { suggestMealPlan } from "../../../api/mealPlans";
import type { SuggestMealPlanInput } from "../schemas";

export function useSuggestMealPlan() {
  return useMutation({
    mutationFn: (input: SuggestMealPlanInput) => suggestMealPlan(input),
  });
}