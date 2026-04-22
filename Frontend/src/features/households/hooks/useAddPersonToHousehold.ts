import { useMutation, useQueryClient } from "@tanstack/react-query";
import { addPersonToHousehold } from "../../../api/households";

export function useAddPersonToHousehold(householdId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (personId: string) => addPersonToHousehold(householdId, personId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["household", householdId] });
      await queryClient.invalidateQueries({ queryKey: ["households"] });
    },
  });
}