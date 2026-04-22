import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPerson } from "../../../api/persons";
import type { CreatePersonData } from "../schemas";

export function useCreatePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreatePersonData) => createPerson(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["persons"] });
    },
  });
}