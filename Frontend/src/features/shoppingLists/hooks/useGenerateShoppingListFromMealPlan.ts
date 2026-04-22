import { useMutation, useQueryClient } from "@tanstack/react-query";
import { generateShoppingListFromMealPlan } from "../../../api/shoppingLists";

export function useGenerateShoppingListFromMealPlan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      mealPlanId,
      shoppingListId,
    }: {
      mealPlanId: string;
      shoppingListId: string;
    }) => generateShoppingListFromMealPlan(mealPlanId, shoppingListId),
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: ["shoppingList", variables.shoppingListId] });
      await queryClient.invalidateQueries({ queryKey: ["shoppingLists"] });
    },
  });
}