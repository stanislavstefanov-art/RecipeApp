import { useMutation, useQueryClient } from "@tanstack/react-query";
import { updateRecipe } from "../../../api/recipes";
import type { UpdateRecipeInput } from "../schemas";

export function useUpdateRecipe(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateRecipeInput) => updateRecipe(recipeId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["recipes"] });
      await queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}