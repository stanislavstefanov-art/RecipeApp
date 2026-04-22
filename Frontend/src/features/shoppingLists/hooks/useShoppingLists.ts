import { useQuery } from "@tanstack/react-query";
import { getShoppingLists } from "../../../api/shoppingLists";

export function useShoppingLists() {
  return useQuery({
    queryKey: ["shoppingLists"],
    queryFn: getShoppingLists,
  });
}