import { useQuery } from "@tanstack/react-query";
import { getMealPlan } from "../../../api/mealPlans";

export function useMealPlan(mealPlanId: string) {
  return useQuery({
    queryKey: ["mealPlan", mealPlanId],
    queryFn: () => getMealPlan(mealPlanId),
    enabled: !!mealPlanId,
  });
}