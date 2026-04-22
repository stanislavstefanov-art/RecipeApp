import { useQuery } from "@tanstack/react-query";
import { getMealPlans } from "../../../api/mealPlans";

export function useMealPlans() {
  return useQuery({
    queryKey: ["mealPlans"],
    queryFn: getMealPlans,
  });
}