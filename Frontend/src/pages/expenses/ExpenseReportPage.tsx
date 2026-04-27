import { useState } from "react";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { ExpenseInsightsPanel } from "../../features/expenses/components/ExpenseInsightsPanel";
import { ExpenseReportQueryForm } from "../../features/expenses/components/ExpenseReportQueryForm";
import { ExpenseReportSummary } from "../../features/expenses/components/ExpenseReportSummary";
import { useExpenseInsights } from "../../features/expenses/hooks/useExpenseInsights";
import { useMonthlyExpenseReport } from "../../features/expenses/hooks/useMonthlyExpenseReport";
import type { MonthlyExpenseQueryData } from "../../features/expenses/schemas";

export function ExpenseReportPage() {
  const { t } = useTranslation();
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
        <h2 className="text-2xl font-semibold">{t('expenses.monthly')}</h2>
        <p className="text-sm text-slate-500">
          {t('expenses.monthlyDesc')}
        </p>
      </div>

      <ExpenseReportQueryForm initialValues={query} onSubmit={setQuery} />

      {reportQuery.isLoading ? (
        <LoadingState title={t('expenses.report')} />
      ) : reportQuery.isError ? (
        <ErrorState
          title={t('expenses.report')}
          message={reportQuery.error instanceof Error ? reportQuery.error.message : undefined}
        />
      ) : !reportQuery.data ? (
        <EmptyState title={t('expenses.noExpenses')} />
      ) : (
        <>
          <ExpenseReportSummary report={reportQuery.data} />

          <div className="rounded-xl border bg-white p-6">
            <h3 className="text-lg font-medium">{t('expenses.byCategory')}</h3>

            {reportQuery.data.categories.length === 0 ? (
              <p className="mt-4 text-sm text-slate-500">{t('expenses.noCategories')}</p>
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
        <LoadingState title={t('expenses.insights')} />
      ) : insightsQuery.isError ? (
        <ErrorState
          title={t('expenses.insights')}
          message={insightsQuery.error instanceof Error ? insightsQuery.error.message : undefined}
        />
      ) : insightsQuery.data ? (
        <ExpenseInsightsPanel insight={insightsQuery.data} />
      ) : null}
    </div>
  );
}
