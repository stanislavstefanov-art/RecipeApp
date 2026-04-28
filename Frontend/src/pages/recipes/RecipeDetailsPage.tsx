import { useParams, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
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
import { RatingSection } from "../../features/recipes/components/RatingSection";

export function RecipeDetailsPage() {
  const { t } = useTranslation();
  const { recipeId = "" } = useParams();
  const { data, isLoading, isError, error } = useRecipe(recipeId);

  if (isLoading) return <LoadingState title={t('recipes.title')} />;

  if (isError) {
    return (
      <ErrorState
        title={t('recipes.title')}
        message={error instanceof Error ? error.message : undefined}
      />
    );
  }

  if (!data) return <EmptyState title={t('errors.Recipe.NotFound')} />;

  return (
    <div className="space-y-5 sm:space-y-6">
      <Link to="/recipes" className="text-sm text-slate-500">
        ← {t('common.back')}
      </Link>

      <div className="flex flex-col gap-4 rounded-xl border bg-white p-5 sm:flex-row sm:items-start sm:justify-between sm:p-6">
        <div>
          <h2 className="text-2xl font-semibold sm:text-3xl">{data.name}</h2>
          <p className="mt-1 text-sm text-slate-500">
            {t('recipes.detailsDesc')}
          </p>
        </div>

        <DeleteRecipeButton recipeId={data.id} />
      </div>

      <div className="grid gap-5 sm:gap-6 lg:grid-cols-2">
        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <h3 className="mb-4 text-base font-medium sm:text-lg">{t('recipes.updateRecipe')}</h3>
          <UpdateRecipeNameForm recipeId={data.id} initialName={data.name} />
        </section>

        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <h3 className="mb-4 text-base font-medium sm:text-lg">{t('recipes.addIngredient')}</h3>
          <AddIngredientForm recipeId={data.id} />
        </section>
      </div>

      <div className="grid gap-5 sm:gap-6 lg:grid-cols-2">
        <section className="rounded-xl border bg-white p-5 sm:p-6">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-medium sm:text-lg">{t('recipes.ingredients')}</h3>
            <span className="text-sm text-slate-500">{data.ingredients.length}</span>
          </div>

          {data.ingredients.length === 0 ? (
            <p className="mt-4 text-sm text-slate-500">{t('recipes.noIngredients')}</p>
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
          <h3 className="mb-4 text-base font-medium sm:text-lg">{t('recipes.addStep')}</h3>
          <AddStepForm recipeId={data.id} />
        </section>
      </div>

      <section className="rounded-xl border bg-white p-5 sm:p-6">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-medium sm:text-lg">{t('recipes.steps')}</h3>
          <span className="text-sm text-slate-500">{data.steps.length}</span>
        </div>

        {data.steps.length === 0 ? (
          <p className="mt-4 text-sm text-slate-500">{t('recipes.noSteps')}</p>
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

      <RatingSection
        recipeId={data.id}
        ratings={data.ratings}
        myRating={data.myRating}
        averageStars={data.averageStars}
        ratingCount={data.ratingCount}
      />
    </div>
  );
}
