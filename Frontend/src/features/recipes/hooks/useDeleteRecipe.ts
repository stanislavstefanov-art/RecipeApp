import { useMutation, useQueryClient } from "@tanstack/react-query";
import { deleteRecipe } from "../../../api/recipes";

export function useDeleteRecipe() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (recipeId: string) => deleteRecipe(recipeId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["recipes"] });
    },
  });
}