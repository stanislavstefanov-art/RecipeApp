import { useQuery } from "@tanstack/react-query";
import { getShoppingList } from "../../../api/shoppingLists";

export function useShoppingList(shoppingListId: string) {
  return useQuery({
    queryKey: ["shoppingList", shoppingListId],
    queryFn: () => getShoppingList(shoppingListId),
    enabled: !!shoppingListId,
  });
}