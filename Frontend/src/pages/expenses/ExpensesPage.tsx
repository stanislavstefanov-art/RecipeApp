import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreateExpenseForm } from "../../features/expenses/components/CreateExpenseForm";
import { ExpenseListItem } from "../../features/expenses/components/ExpenseListItem";
import { useExpenses } from "../../features/expenses/hooks/useExpenses";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function ExpensesPage() {
  const { data, isLoading, isError, error } = useExpenses();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title="Expenses"
          description="Track food and household-related spending."
        />

        {isLoading ? (
          <LoadingState title="Loading expenses" />
        ) : isError ? (
          <ErrorState
            title="Failed to load expenses"
            message={error instanceof Error ? error.message : "Unknown error"}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title="No expenses yet" message="Create your first expense." />
        ) : (
          <div className="grid gap-4">
            {data.map((expense) => (
              <ExpenseListItem key={expense.id} expense={expense} />
            ))}
          </div>
        )}
      </div>

      <div>
        <CreateExpenseForm />
      </div>
    </div>
  );
}