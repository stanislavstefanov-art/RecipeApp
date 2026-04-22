import { useQuery } from "@tanstack/react-query";
import { getHouseholds } from "../../../api/households";

export function useHouseholds() {
  return useQuery({
    queryKey: ["households"],
    queryFn: getHouseholds,
  });
}