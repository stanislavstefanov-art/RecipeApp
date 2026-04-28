import { useMutation, useQueryClient } from "@tanstack/react-query";
import { logCooking } from "../../../api/recipes";

export function useLogCooking(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      cookedOn,
      servings,
      notes,
    }: {
      cookedOn: string;
      servings: number;
      notes?: string | null;
    }) => logCooking(recipeId, cookedOn, servings, notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cookingHistory", recipeId] });
    },
  });
}
