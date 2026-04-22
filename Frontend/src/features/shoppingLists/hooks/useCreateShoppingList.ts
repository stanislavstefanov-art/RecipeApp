import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createShoppingList } from "../../../api/shoppingLists";
import type { CreateShoppingListInput } from "../schemas";

export function useCreateShoppingList() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateShoppingListInput) => createShoppingList(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["shoppingLists"] });
    },
  });
}