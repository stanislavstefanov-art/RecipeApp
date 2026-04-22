import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createExpense } from "../../../api/expenses";
import type { CreateExpenseInput } from "../schemas";

export function useCreateExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateExpenseInput) => createExpense(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["expenses"] });
    },
  });
}