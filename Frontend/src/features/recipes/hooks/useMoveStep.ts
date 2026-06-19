import { useMutation, useQueryClient } from "@tanstack/react-query";
import { moveStep } from "../../../api/recipes";

export function useMoveStep(recipeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ stepId, direction }: { stepId: string; direction: "up" | "down" }) =>
      moveStep(recipeId, stepId, direction),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipe", recipeId] });
    },
  });
}
