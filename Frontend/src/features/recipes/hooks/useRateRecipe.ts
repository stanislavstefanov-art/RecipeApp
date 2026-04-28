import { useMutation, useQueryClient } from "@tanstack/react-query";
import { rateRecipe } from "../../../api/recipes";

export function useRateRecipe(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ stars, comment }: { stars: number; comment?: string | null }) =>
      rateRecipe(recipeId, stars, comment),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}
