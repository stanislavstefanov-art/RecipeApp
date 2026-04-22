import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createHousehold } from "../../../api/households";
import type { CreateHouseholdInput } from "../schemas";

export function useCreateHousehold() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateHouseholdInput) => createHousehold(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["households"] });
    },
  });
}