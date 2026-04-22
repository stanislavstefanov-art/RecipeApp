import { useQuery } from "@tanstack/react-query";
import { getExpenses } from "../../../api/expenses";

export function useExpenses() {
  return useQuery({
    queryKey: ["expenses"],
    queryFn: getExpenses,
  });
}