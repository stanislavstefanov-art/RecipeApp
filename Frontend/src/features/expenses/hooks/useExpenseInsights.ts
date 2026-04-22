import { useQuery } from "@tanstack/react-query";
import { getExpenseInsights } from "../../../api/expenses";

export function useExpenseInsights(year: number, month: number) {
  return useQuery({
    queryKey: ["expenseInsights", year, month],
    queryFn: () => getExpenseInsights(year, month),
    enabled: !!year && !!month,
  });
}