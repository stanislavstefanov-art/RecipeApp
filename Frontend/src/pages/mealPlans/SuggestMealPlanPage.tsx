import { SuggestMealPlanForm } from "../../features/mealPlans/components/SuggestMealPlanForm";

export function SuggestMealPlanPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold">Suggest meal plan</h2>
        <p className="text-sm text-slate-500">
          Generate a household-aware meal plan suggestion for review.
        </p>
      </div>

      <SuggestMealPlanForm />
    </div>
  );
}