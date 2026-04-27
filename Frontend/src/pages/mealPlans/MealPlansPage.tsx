import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { useMealPlans } from "../../features/mealPlans/hooks/useMealPlans";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function MealPlansPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = useMealPlans();

  if (isLoading) {
    return <LoadingState title={t('mealPlans.title')} />;
  }

  if (isError) {
    return (
      <ErrorState
        title={t('mealPlans.title')}
        message={error instanceof Error ? error.message : undefined}
      />
    );
  }

  return (
    <div className="space-y-6">
      <SectionHeader
        title={t('mealPlans.title')}
        description={t('mealPlans.suggestPlanDesc')}
        action={
          <Link
            to="/meal-plans/suggest"
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
          >
            {t('mealPlans.suggest')}
          </Link>
        }
      />

      {!data || data.length === 0 ? (
        <EmptyState
          title={t('mealPlans.noMealPlans')}
          message={t('mealPlans.generateDesc')}
        />
      ) : (
        <div className="grid gap-4">
          {data.map((mealPlan) => (
            <Link
              key={mealPlan.id}
              to={`/meal-plans/${mealPlan.id}`}
              className="rounded-xl border bg-white p-5 hover:shadow-sm"
            >
              <h3 className="font-medium">{mealPlan.name}</h3>
              <p className="mt-1 text-sm text-slate-500">
                {t('mealPlans.householdLabel', { name: mealPlan.householdName })}
              </p>
              <p className="mt-1 text-sm text-slate-500">
                {t('mealPlans.entryCountLabel', { count: mealPlan.entryCount })}
              </p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
