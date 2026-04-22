import { useMutation, useQueryClient } from "@tanstack/react-query";
import { purchaseShoppingListItemWithExpense } from "../../../api/shoppingLists";
import type { PurchaseShoppingListItemInput } from "../schemas";

export function usePurchaseShoppingListItemWithExpense(shoppingListId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      shoppingListItemId,
      input,
    }: {
      shoppingListItemId: string;
      input: PurchaseShoppingListItemInput;
    }) => purchaseShoppingListItemWithExpense(shoppingListId, shoppingListItemId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["shoppingList", shoppingListId] });
      await queryClient.invalidateQueries({ queryKey: ["shoppingLists"] });
      await queryClient.invalidateQueries({ queryKey: ["expenses"] });
    },
  });
}