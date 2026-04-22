import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createRecipe } from "../../../api/recipes";
import type { CreateRecipeInput } from "../schemas";

export function useCreateRecipe() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateRecipeInput) => createRecipe(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipes"] });
    },
  });
}