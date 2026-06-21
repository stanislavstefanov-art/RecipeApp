import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  LoadingState,
  ErrorState,
  EmptyState,
} from "../../components/ui/PageState";
import { useRecipe } from "../../features/recipes/hooks/useRecipe";
import { useUpdateStep } from "../../features/recipes/hooks/useUpdateStep";
import { useMoveStep } from "../../features/recipes/hooks/useMoveStep";
import { useSetSeasonality } from "../../features/recipes/hooks/useSetSeasonality";
import { UpdateRecipeNameForm } from "../../features/recipes/components/UpdateRecipeNameForm";
import { AddIngredientForm } from "../../features/recipes/components/AddIngredientForm";
import { AddStepForm } from "../../features/recipes/components/AddStepForm";
import { DeleteRecipeButton } from "../../features/recipes/components/DeleteRecipeButton";
import { RatingSection } from "../../features/recipes/components/RatingSection";
import { CookingHistorySection } from "../../features/recipes/components/CookingHistorySection";

export function RecipeDetailsPage() {
  const { t } = useTranslation();
  const { recipeId = "" } = useParams();
  const { data, isLoading, isError, error } = useRecipe(recipeId);
  const updateStepMutation = useUpdateStep(recipeId);
  const moveStepMutation = useMoveStep(recipeId);
  const setSeasonalityMutation = useSetSeasonality(recipeId);

  const [editingStepId, setEditingStepId] = useState<string | null>(null);
  const [editingInstruction, setEditingInstruction] = useState("");

  function startEdit(stepId: string, currentInstruction: string) {
    setEditingStepId(stepId);
    setEditingInstruction(currentInstruction);
  }

  function cancelEdit() {
    setEditingStepId(null);
    setEditingInstruction("");
  }

  function saveEdit(stepId: string) {
    if (!editingInstruction.trim()) return;
    updateStepMutation.mutate(
      { stepId, instruction: editingInstruction.trim() },
      { onSuccess: () => { setEditingStepId(null); setEditingInstruction(""); } },
    );
  }

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

  const steps = [...data.steps].sort((a, b) => a.order - b.order);

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
          <h3 className="text-base font-medium sm:text-lg">{t('recipes.seasonality')}</h3>
          {setSeasonalityMutation.isPending && (
            <span className="text-sm text-slate-400">{t('common.loading')}</span>
          )}
        </div>
        <div className="mt-3">
          <select
            value={data.seasonality}
            disabled={setSeasonalityMutation.isPending}
            onChange={(e) => setSeasonalityMutation.mutate(Number(e.target.value))}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none disabled:opacity-50"
          >
            {[0, 1, 2, 3, 4].map((s) => (
              <option key={s} value={s}>
                {t(`enums.season.${s}`)}
              </option>
            ))}
          </select>
        </div>
      </section>

      <section className="rounded-xl border bg-white p-5 sm:p-6">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-medium sm:text-lg">{t('recipes.steps')}</h3>
          <span className="text-sm text-slate-500">{steps.length}</span>
        </div>

        {steps.length === 0 ? (
          <p className="mt-4 text-sm text-slate-500">{t('recipes.noSteps')}</p>
        ) : (
          <ol className="mt-4 space-y-3">
            {steps.map((step, idx) => (
              <li key={step.id} className="flex gap-3">
                <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold">
                  {step.order}
                </span>

                {editingStepId === step.id ? (
                  <div className="flex flex-1 flex-col gap-2">
                    <textarea
                      value={editingInstruction}
                      onChange={(e) => setEditingInstruction(e.target.value)}
                      maxLength={1000}
                      rows={3}
                      autoFocus
                      className="w-full rounded-lg border px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-slate-300"
                    />
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => saveEdit(step.id)}
                        disabled={updateStepMutation.isPending || !editingInstruction.trim()}
                        className="rounded-lg bg-slate-900 px-3 py-1 text-xs font-medium text-white disabled:opacity-50"
                      >
                        {updateStepMutation.isPending ? t("common.save") + "…" : t("common.save")}
                      </button>
                      <button
                        type="button"
                        onClick={cancelEdit}
                        className="rounded-lg border px-3 py-1 text-xs font-medium text-slate-700"
                      >
                        {t("common.cancel")}
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <span className="flex-1 text-sm text-slate-700">{step.instruction}</span>
                    <div className="flex shrink-0 items-center gap-1">
                      <button
                        type="button"
                        title={t("recipes.moveStepUp")}
                        disabled={idx === 0 || moveStepMutation.isPending}
                        onClick={() => moveStepMutation.mutate({ stepId: step.id, direction: "up" })}
                        className="rounded p-0.5 text-slate-400 hover:text-slate-700 disabled:opacity-25"
                      >
                        ↑
                      </button>
                      <button
                        type="button"
                        title={t("recipes.moveStepDown")}
                        disabled={idx === steps.length - 1 || moveStepMutation.isPending}
                        onClick={() => moveStepMutation.mutate({ stepId: step.id, direction: "down" })}
                        className="rounded p-0.5 text-slate-400 hover:text-slate-700 disabled:opacity-25"
                      >
                        ↓
                      </button>
                      <button
                        type="button"
                        onClick={() => startEdit(step.id, step.instruction)}
                        className="ml-1 text-xs text-slate-500 hover:text-slate-800"
                      >
                        {t("common.edit")}
                      </button>
                    </div>
                  </>
                )}
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

      <CookingHistorySection recipeId={data.id} />
    </div>
  );
}
