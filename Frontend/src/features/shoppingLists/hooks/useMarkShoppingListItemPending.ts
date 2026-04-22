import { useMutation, useQueryClient } from "@tanstack/react-query";
import { markShoppingListItemPending } from "../../../api/shoppingLists";

export function useMarkShoppingListItemPending(shoppingListId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (shoppingListItemId: string) =>
      markShoppingListItemPending(shoppingListId, shoppingListItemId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["shoppingList", shoppingListId] });
      await queryClient.invalidateQueries({ queryKey: ["shoppingLists"] });
    },
  });
}