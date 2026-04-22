import { useParams, Link } from "react-router-dom";
import {
  LoadingState,
  ErrorState,
  EmptyState,
} from "../../components/ui/PageState";
import { useRecipe } from "../../features/recipes/hooks/useRecipe";
import { UpdateRecipeNameForm } from "../../features/recipes/components/UpdateRecipeNameForm";
import { AddIngredientForm } from "../../features/recipes/components/AddIngredientForm";
import { AddStepForm } from "../../features/recipes/components/AddStepForm";
import { DeleteRecipeButton } from "../../features/recipes/components/DeleteRecipeButton";

export function RecipeDetailsPage() {
  const { recipeId = "" } = useParams();
  const { data, isLoading, isError, error } = useRecipe(recipeId);

  if (isLoading) return <LoadingState title="Loading recipe" />;

  if (isError) {
    return (
      <ErrorState
        title="Failed to load recipe"
        message={error instanceof Error ? error.message : "Unknown"}
      />
    );
  }

  if (!data) return <EmptyState title="Recipe not found" />;

  return (
    <div className="space-y-5 sm:space-y-6">
      <Link to="/recipes" className="text-sm text-slate-500">
        ← Back
      </Link>

      <div className="flex flex-col gap-4 rounded-xl border bg-white p-5 sm:flex-row sm:items-start sm:justify-between sm:p-6">
        <div>
          <h2 className="text-2xl font-semibold sm:text-3xl">{data.name}</h2>
          <p className="mt-1 text-sm text-slate-500">
            Manage recipe details, ingredients, and steps.
          </p>
        </div>

        <DeleteRecipeButton recipeId={data.id} />
      </div>

      <div className="grid gap-5 sm:gap-6 lg:grid-cols-2">
        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <h3 className="mb-4 text-base font-medium sm:text-lg">Update recipe</h3>
          <UpdateRecipeNameForm recipeId={data.id} initialName={data.name} />
        </section>

        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <h3 className="mb-4 text-base font-medium sm:text-lg">Add ingredient</h3>
          <AddIngredientForm recipeId={data.id} />
        </section>
      </div>

      <div className="grid gap-5 sm:gap-6 lg:grid-cols-2">
        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-medium sm:text-lg">Ingredients</h3>
            <span className="text-sm text-slate-500">{data.ingredients.length}</span>
          </div>

          {data.ingredients.length === 0 ? (
            <p className="mt-4 text-sm text-slate-500">No ingredients</p>
          ) : (
            <ul className="mt-4 space-y-3">
              {data.ingredients.map((ingredient, idx) => (
                <li key={`${ingredient.name}-${idx}`} className="flex justify-between gap-3 border-b pb-3 last:border-b-0 last:pb-0">
                  <span className="min-w-0">{ingredient.name}</span>
                  <span className="shrink-0 text-sm text-slate-500">
                    {ingredient.quantity} {ingredient.unit}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <h3 className="mb-4 text-base font-medium sm:text-lg">Add step</h3>
          <AddStepForm recipeId={data.id} />
        </section>
      </div>

      <section className="rounded-xl border bg-white p-5 sm:p-6">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-medium sm:text-lg">Steps</h3>
          <span className="text-sm text-slate-500">{data.steps.length}</span>
        </div>

        {data.steps.length === 0 ? (
          <p className="mt-4 text-sm text-slate-500">No steps</p>
        ) : (
          <ol className="mt-4 space-y-3">
            {data.steps.map((step) => (
              <li key={step.order} className="flex gap-3">
                <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold">
                  {step.order}
                </span>
                <span className="text-sm text-slate-700">{step.instruction}</span>
              </li>
            ))}
          </ol>
        )}
      </section>
    </div>
  );
}