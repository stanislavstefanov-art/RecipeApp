import { useState } from "react";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { ExpenseInsightsPanel } from "../../features/expenses/components/ExpenseInsightsPanel";
import { ExpenseReportQueryForm } from "../../features/expenses/components/ExpenseReportQueryForm";
import { ExpenseReportSummary } from "../../features/expenses/components/ExpenseReportSummary";
import { useExpenseInsights } from "../../features/expenses/hooks/useExpenseInsights";
import { useMonthlyExpenseReport } from "../../features/expenses/hooks/useMonthlyExpenseReport";
import type { MonthlyExpenseQueryData } from "../../features/expenses/schemas";

export function ExpenseReportPage() {
  const now = new Date();

  const [query, setQuery] = useState<MonthlyExpenseQueryData>({
    year: now.getFullYear(),
    month: now.getMonth() + 1,
  });

  const reportQuery = useMonthlyExpenseReport(query.year, query.month);
  const insightsQuery = useExpenseInsights(query.year, query.month);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold">Monthly expense report</h2>
        <p className="text-sm text-slate-500">
          Review monthly totals, category breakdowns, and AI-generated insights.
        </p>
      </div>

      <ExpenseReportQueryForm initialValues={query} onSubmit={setQuery} />

      {reportQuery.isLoading ? (
        <LoadingState title="Loading expense report" />
      ) : reportQuery.isError ? (
        <ErrorState
          title="Failed to load expense report"
          message={reportQuery.error instanceof Error ? reportQuery.error.message : "Unknown error"}
        />
      ) : !reportQuery.data ? (
        <EmptyState title="No report data" />
      ) : (
        <>
          <ExpenseReportSummary report={reportQuery.data} />

          <div className="rounded-xl border bg-white p-6">
            <h3 className="text-lg font-medium">Category breakdown</h3>

            {reportQuery.data.categories.length === 0 ? (
              <p className="mt-4 text-sm text-slate-500">No categories found.</p>
            ) : (
              <div className="mt-4 grid gap-3">
                {reportQuery.data.categories.map((category) => (
                  <div
                    key={category.category}
                    className="flex items-center justify-between rounded-lg border p-4"
                  >
                    <div>
                      <p className="font-medium">{category.category}</p>
                      <p className="text-sm text-slate-500">{category.percentage}%</p>
                    </div>
                    <p className="font-medium">
                      {category.amount} {reportQuery.data.currency}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}

      {insightsQuery.isLoading ? (
        <LoadingState title="Loading insights" />
      ) : insightsQuery.isError ? (
        <ErrorState
          title="Failed to load insights"
          message={insightsQuery.error instanceof Error ? insightsQuery.error.message : "Unknown error"}
        />
      ) : insightsQuery.data ? (
        <ExpenseInsightsPanel insight={insightsQuery.data} />
      ) : null}
    </div>
  );
}