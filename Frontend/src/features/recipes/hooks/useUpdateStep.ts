import { useMutation, useQueryClient } from "@tanstack/react-query";
import { updateStep } from "../../../api/recipes";

export function useUpdateStep(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ stepId, instruction }: { stepId: string; instruction: string }) =>
      updateStep(recipeId, stepId, instruction),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}
