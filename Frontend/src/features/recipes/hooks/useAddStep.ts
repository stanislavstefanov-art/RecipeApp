import { useMutation, useQueryClient } from "@tanstack/react-query";
import { addStep } from "../../../api/recipes";
import type { AddStepInput } from "../schemas";

export function useAddStep(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AddStepInput) => addStep(recipeId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}