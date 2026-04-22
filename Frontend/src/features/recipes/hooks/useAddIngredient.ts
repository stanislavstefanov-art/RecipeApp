import { useMutation, useQueryClient } from "@tanstack/react-query";
import { addIngredient } from "../../../api/recipes";
import type { AddIngredientInput } from "../schemas";

export function useAddIngredient(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AddIngredientInput) => addIngredient(recipeId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
      await queryClient.invalidateQueries({ queryKey: ["recipes"] });
    },
  });
}