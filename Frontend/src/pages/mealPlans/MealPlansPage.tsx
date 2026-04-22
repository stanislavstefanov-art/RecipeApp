import { Link } from "react-router-dom";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { useMealPlans } from "../../features/mealPlans/hooks/useMealPlans";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function MealPlansPage() {
  const { data, isLoading, isError, error } = useMealPlans();

  if (isLoading) {
    return <LoadingState title="Loading meal plans" />;
  }

  if (isError) {
    return (
      <ErrorState
        title="Failed to load meal plans"
        message={error instanceof Error ? error.message : "Unknown error"}
      />
    );
  }

  return (
    <div className="space-y-6">
      <SectionHeader
        title="Meal plans"
        description="Browse saved household meal plans."
        action={
          <Link
            to="/meal-plans/suggest"
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
          >
            Suggest meal plan
          </Link>
        }
      />

      {!data || data.length === 0 ? (
        <EmptyState
          title="No meal plans yet"
          message="Generate or create a meal plan to get started."
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
                Household: {mealPlan.householdName}
              </p>
              <p className="mt-1 text-sm text-slate-500">
                Entries: {mealPlan.entryCount}
              </p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}