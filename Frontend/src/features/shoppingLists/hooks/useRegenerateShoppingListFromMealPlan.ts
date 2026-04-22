import { useMutation, useQueryClient } from "@tanstack/react-query";
import { regenerateShoppingListFromMealPlan } from "../../../api/shoppingLists";

export function useRegenerateShoppingListFromMealPlan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      mealPlanId,
      shoppingListId,
    }: {
      mealPlanId: string;
      shoppingListId: string;
    }) => regenerateShoppingListFromMealPlan(mealPlanId, shoppingListId),
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: ["shoppingList", variables.shoppingListId] });
      await queryClient.invalidateQueries({ queryKey: ["shoppingLists"] });
    },
  });
}