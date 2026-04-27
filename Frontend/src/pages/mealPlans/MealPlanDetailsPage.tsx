import { Link, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { EditMealPlanAssignmentModal } from "../../features/mealPlans/components/EditMealPlanAssignmentModal";
import { MealPlanEntryCard } from "../../features/mealPlans/components/MealPlanEntryCard";
import { useGroupedMealPlanEntries } from "../../features/mealPlans/hooks/useGroupedMealPlanEntries";
import { useMealPlan } from "../../features/mealPlans/hooks/useMealPlan";
import { formatPlannedDate } from "../../features/mealPlans/utils";

export function MealPlanDetailsPage() {
  const { t } = useTranslation();
  const { mealPlanId = "" } = useParams();
  const { data, isLoading, isError, error } = useMealPlan(mealPlanId);
  const groupedEntries = useGroupedMealPlanEntries(data?.entries);

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

  if (!data) {
    return <EmptyState title={t('mealPlans.noMealPlans')} />;
  }

  return (
    <>
      <div className="space-y-6">
        <Link to="/meal-plans" className="text-sm text-slate-500">
          ← {t('common.back')}
        </Link>

        <div className="rounded-xl border bg-white p-6">
          <h2 className="text-2xl font-semibold">{data.name}</h2>
          <p className="mt-1 text-sm text-slate-500">
            {t('mealPlans.householdLabel', { name: data.householdName })}
          </p>
        </div>

        {groupedEntries.length === 0 ? (
          <EmptyState
            title={t('mealPlans.noEntries')}
            message={t('mealPlans.noEntriesDesc')}
          />
        ) : (
          <div className="space-y-8">
            {groupedEntries.map((group) => (
              <section key={group.date} className="space-y-4">
                <h3 className="text-lg font-semibold">
                  {formatPlannedDate(group.date)}
                </h3>

                <div className="grid gap-4">
                  {group.entries.map((entry) => (
                    <MealPlanEntryCard key={entry.id} entry={entry} />
                  ))}
                </div>
              </section>
            ))}
          </div>
        )}
      </div>

      <EditMealPlanAssignmentModal mealPlanId={data.id} />
    </>
  );
}
