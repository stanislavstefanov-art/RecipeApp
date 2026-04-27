import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { EmptyState } from "../../components/ui/PageState";
import { MealPlanSuggestionEntryCard } from "../../features/mealPlans/components/MealPlanSuggestionEntryCard";
import { useAcceptMealPlanSuggestion } from "../../features/mealPlans/hooks/useAcceptMealPlanSuggestion";
import { useMealPlanSuggestionStore } from "../../features/mealPlans/store/useMealPlanSuggestionStore";

export function MealPlanSuggestionReviewPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { request, suggestion, clearSuggestion } = useMealPlanSuggestionStore();
  const mutation = useAcceptMealPlanSuggestion();

  if (!request || !suggestion) {
    return (
      <EmptyState
        title={t('mealPlans.noSuggestion')}
        message={t('mealPlans.noSuggestionDesc')}
      />
    );
  }

  const onAccept = async () => {
    const result = await mutation.mutateAsync({
      name: request.name,
      householdId: request.householdId,
      entries: suggestion.entries,
    });

    clearSuggestion();
    navigate(`/meal-plans/${result.mealPlanId}`);
  };

  return (
    <div className="space-y-6">
      <div className="rounded-xl border bg-white p-6">
        <h2 className="text-2xl font-semibold">{suggestion.name}</h2>
        <p className="mt-1 text-sm text-slate-500">
          {t('mealPlans.confidence')}: {suggestion.confidence}
        </p>
        <p className="mt-1 text-sm text-slate-500">
          {t('mealPlans.needsReview')}: {suggestion.needsReview ? t('common.yes') : t('common.no')}
        </p>
        {suggestion.notes ? (
          <p className="mt-3 text-sm text-slate-700">{suggestion.notes}</p>
        ) : null}
      </div>

      <div className="space-y-4">
        {suggestion.entries.map((entry, index) => (
          <MealPlanSuggestionEntryCard
            key={`${entry.baseRecipeId}-${entry.plannedDate}-${index}`}
            entry={entry}
          />
        ))}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">
          {t('mealPlans.failedAccept')}
        </p>
      ) : null}

      <div className="flex gap-3">
        <button
          type="button"
          onClick={onAccept}
          disabled={mutation.isPending}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
        >
          {mutation.isPending ? t('mealPlans.acceptingSuggestion') : t('mealPlans.acceptSuggestion')}
        </button>

        <button
          type="button"
          onClick={clearSuggestion}
          className="rounded-lg border px-4 py-2 text-sm"
        >
          {t('mealPlans.clearReview')}
        </button>
      </div>
    </div>
  );
}
