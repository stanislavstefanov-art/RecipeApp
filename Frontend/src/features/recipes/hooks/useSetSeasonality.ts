import { useMutation, useQueryClient } from "@tanstack/react-query";
import { setSeasonality } from "../../../api/recipes";

export function useSetSeasonality(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (seasonality: number) => setSeasonality(recipeId, seasonality),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}
