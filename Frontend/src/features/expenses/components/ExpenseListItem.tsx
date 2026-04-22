import type { Expense } from "../schemas";
import {
  formatCurrency,
  formatDate,
  getExpenseCategoryLabel,
  getExpenseSourceTypeLabel,
} from "../utils";

type Props = {
  expense: Expense;
};

export function ExpenseListItem({ expense }: Props) {
  return (
    <div className="rounded-xl border bg-white p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h3 className="font-medium">{expense.description}</h3>
          <p className="mt-1 text-sm text-slate-500">
            {getExpenseCategoryLabel(expense.category)} · {getExpenseSourceTypeLabel(expense.sourceType)}
          </p>
          <p className="mt-1 text-sm text-slate-500">
            {formatDate(expense.expenseDate)}
          </p>
        </div>

        <div className="text-right">
          <p className="font-medium">{formatCurrency(expense.amount, expense.currency)}</p>
        </div>
      </div>
    </div>
  );
}