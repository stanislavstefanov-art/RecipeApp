import { useMutation, useQueryClient } from "@tanstack/react-query";
import { updateMealPlanAssignment } from "../../../api/mealPlans";
import type { UpdateMealPlanAssignmentInput } from "../schemas";

export function useUpdateMealPlanAssignment(mealPlanId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      mealPlanEntryId,
      input,
    }: {
      mealPlanEntryId: string;
      input: UpdateMealPlanAssignmentInput;
    }) => updateMealPlanAssignment(mealPlanId, mealPlanEntryId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["mealPlan", mealPlanId] });
      await queryClient.invalidateQueries({ queryKey: ["mealPlans"] });
    },
  });
}