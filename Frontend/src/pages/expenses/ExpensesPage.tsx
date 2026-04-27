import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreateExpenseForm } from "../../features/expenses/components/CreateExpenseForm";
import { ExpenseListItem } from "../../features/expenses/components/ExpenseListItem";
import { useExpenses } from "../../features/expenses/hooks/useExpenses";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function ExpensesPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = useExpenses();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title={t('expenses.title')}
          description={t('expenses.createDesc')}
        />

        {isLoading ? (
          <LoadingState title={t('expenses.title')} />
        ) : isError ? (
          <ErrorState
            title={t('expenses.title')}
            message={error instanceof Error ? error.message : undefined}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title={t('expenses.noExpenses')} />
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
