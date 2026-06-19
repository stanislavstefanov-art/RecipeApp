import { useMutation, useQueryClient } from "@tanstack/react-query";
import { logCooking } from "../../../api/recipes";

export function useLogCooking(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      cookedOn,
      servings,
      notes,
      preparedByPersonIds,
    }: {
      cookedOn: string;
      servings: number;
      notes?: string | null;
      preparedByPersonIds?: string[];
    }) => logCooking(recipeId, cookedOn, servings, notes, preparedByPersonIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cookingHistory", recipeId] });
    },
  });
}
