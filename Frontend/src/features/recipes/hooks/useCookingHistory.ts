import { useQuery } from "@tanstack/react-query";
import { getCookingHistory } from "../../../api/recipes";

export function useCookingHistory(recipeId: string) {
  return useQuery({
    queryKey: ["cookingHistory", recipeId],
    queryFn: () => getCookingHistory(recipeId),
  });
}
