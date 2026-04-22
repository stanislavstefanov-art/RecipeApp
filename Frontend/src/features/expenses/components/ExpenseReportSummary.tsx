import type { MonthlyExpenseReport } from "../schemas";
import { formatCurrency, formatDate } from "../utils";

type Props = {
  report: MonthlyExpenseReport;
};

export function ExpenseReportSummary({ report }: Props) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <div className="rounded-xl border bg-white p-4 sm:p-5">
        <p className="text-sm text-slate-500">Total</p>
        <p className="mt-2 text-lg font-semibold sm:text-xl">
          {formatCurrency(report.totalAmount, report.currency)}
        </p>
      </div>

      <div className="rounded-xl border bg-white p-4 sm:p-5">
        <p className="text-sm text-slate-500">Expenses</p>
        <p className="mt-2 text-lg font-semibold sm:text-xl">{report.expenseCount}</p>
      </div>

      <div className="rounded-xl border bg-white p-4 sm:p-5">
        <p className="text-sm text-slate-500">Average</p>
        <p className="mt-2 text-lg font-semibold sm:text-xl">
          {formatCurrency(report.averageExpenseAmount, report.currency)}
        </p>
      </div>

      <div className="rounded-xl border bg-white p-4 sm:p-5">
        <p className="text-sm text-slate-500">Top category</p>
        <p className="mt-2 text-lg font-semibold sm:text-xl">
          {report.topCategory || "N/A"}
        </p>
      </div>

      <div className="rounded-xl border bg-white p-4 sm:p-5 sm:col-span-2 xl:col-span-4">
        <p className="text-sm text-slate-500">Food percentage</p>
        <p className="mt-2 text-lg font-semibold sm:text-xl">{report.foodPercentage}%</p>

        {report.largestExpense ? (
          <div className="mt-4 border-t pt-4 text-sm text-slate-700">
            <p>
              Largest expense:{" "}
              <span className="font-medium">
                {formatCurrency(report.largestExpense.amount, report.currency)}
              </span>
            </p>
            <p>{report.largestExpense.description}</p>
            <p>{formatDate(report.largestExpense.expenseDate)}</p>
          </div>
        ) : null}
      </div>
    </div>
  );
}