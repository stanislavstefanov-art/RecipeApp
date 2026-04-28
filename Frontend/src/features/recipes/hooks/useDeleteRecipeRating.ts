import { useMutation, useQueryClient } from "@tanstack/react-query";
import { deleteRecipeRating } from "../../../api/recipes";

export function useDeleteRecipeRating(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => deleteRecipeRating(recipeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}
