import { useQuery } from "@tanstack/react-query";
import { getMonthlyExpenseReport } from "../../../api/expenses";

export function useMonthlyExpenseReport(year: number, month: number) {
  return useQuery({
    queryKey: ["expenseReport", year, month],
    queryFn: () => getMonthlyExpenseReport(year, month),
    enabled: !!year && !!month,
  });
}