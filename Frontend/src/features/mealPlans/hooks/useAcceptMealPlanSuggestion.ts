import { useMutation, useQueryClient } from "@tanstack/react-query";
import { acceptMealPlanSuggestion } from "../../../api/mealPlans";
import type { AcceptMealPlanSuggestionInput } from "../schemas";

export function useAcceptMealPlanSuggestion() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AcceptMealPlanSuggestionInput) =>
      acceptMealPlanSuggestion(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["mealPlans"] });
    },
  });
}