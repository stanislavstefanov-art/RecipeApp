import { useMutation, useQueryClient } from "@tanstack/react-query";
import { deleteCookingEntry } from "../../../api/recipes";

export function useDeleteCookingEntry(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteCookingEntry(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cookingHistory", recipeId] });
    },
  });
}
