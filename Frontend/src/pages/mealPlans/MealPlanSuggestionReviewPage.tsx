import { useNavigate } from "react-router-dom";
import { EmptyState } from "../../components/ui/PageState";
import { MealPlanSuggestionEntryCard } from "../../features/mealPlans/components/MealPlanSuggestionEntryCard";
import { useAcceptMealPlanSuggestion } from "../../features/mealPlans/hooks/useAcceptMealPlanSuggestion";
import { useMealPlanSuggestionStore } from "../../features/mealPlans/store/useMealPlanSuggestionStore";

export function MealPlanSuggestionReviewPage() {
  const navigate = useNavigate();
  const { request, suggestion, clearSuggestion } = useMealPlanSuggestionStore();
  const mutation = useAcceptMealPlanSuggestion();

  if (!request || !suggestion) {
    return (
      <EmptyState
        title="No suggestion to review"
        message="Generate a meal plan suggestion first."
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
          Confidence: {suggestion.confidence}
        </p>
        <p className="mt-1 text-sm text-slate-500">
          Needs review: {suggestion.needsReview ? "Yes" : "No"}
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
          Failed to accept suggestion.
        </p>
      ) : null}

      <div className="flex gap-3">
        <button
          type="button"
          onClick={onAccept}
          disabled={mutation.isPending}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
        >
          {mutation.isPending ? "Accepting..." : "Accept suggestion"}
        </button>

        <button
          type="button"
          onClick={clearSuggestion}
          className="rounded-lg border px-4 py-2 text-sm"
        >
          Clear review
        </button>
      </div>
    </div>
  );
}