import { useQuery } from "@tanstack/react-query";
import { getHousehold } from "../../../api/households";

export function useHousehold(householdId: string) {
  return useQuery({
    queryKey: ["household", householdId],
    queryFn: () => getHousehold(householdId),
    enabled: !!householdId,
  });
}