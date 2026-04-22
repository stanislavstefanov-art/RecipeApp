import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { useRecipes } from "../../recipes/hooks/useRecipes";
import { useRecipe } from "../../recipes/hooks/useRecipe";
import {
  updateMealPlanAssignmentSchema,
  type UpdateMealPlanAssignmentInput,
  type UpdateMealPlanAssignmentData,
} from "../schemas";
import { useMealPlanUiStore } from "../store/useMealPlanUiStore";
import { useUpdateMealPlanAssignment } from "../hooks/useUpdateMealPlanAssignment";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

type Props = {
  mealPlanId: string;
};

export function EditMealPlanAssignmentModal({ mealPlanId }: Props) {
  const editingAssignment = useMealPlanUiStore((s) => s.editingAssignment);
  const closeEditAssignment = useMealPlanUiStore((s) => s.closeEditAssignment);
  const mutation = useUpdateMealPlanAssignment(mealPlanId);
  const { data: recipes = [] } = useRecipes();
  const pushToast = useToastStore((s) => s.pushToast);

  const selectedAssignedRecipeId = editingAssignment?.assignment.assignedRecipeId ?? "";
  const { data: selectedRecipe } = useRecipe(selectedAssignedRecipeId);

  const form = useForm<UpdateMealPlanAssignmentInput, unknown, UpdateMealPlanAssignmentData>({
    resolver: zodResolver(updateMealPlanAssignmentSchema),
    values: editingAssignment
      ? {
          personId: editingAssignment.assignment.personId,
          assignedRecipeId: editingAssignment.assignment.assignedRecipeId,
          recipeVariationId: editingAssignment.assignment.recipeVariationId ?? null,
          portionMultiplier: editingAssignment.assignment.portionMultiplier,
          notes: editingAssignment.assignment.notes ?? null,
        }
      : {
          personId: "",
          assignedRecipeId: "",
          recipeVariationId: null,
          portionMultiplier: 1,
          notes: null,
        },
  });

  const watchedRecipeId = form.watch("assignedRecipeId");
  const watchedSelectedRecipe = useRecipe(watchedRecipeId);
  const recipeForVariations = watchedRecipeId ? watchedSelectedRecipe.data : selectedRecipe;

  const variationOptions = useMemo(
    () => recipeForVariations?.variations ?? [],
    [recipeForVariations],
  );

  if (!editingAssignment) {
    return null;
  }

  const onSubmit = async (values: UpdateMealPlanAssignmentData) => {
    try {
      await mutation.mutateAsync({
        mealPlanEntryId: editingAssignment.mealPlanEntryId,
        input: {
          ...values,
          notes: values.notes || null,
          recipeVariationId: values.recipeVariationId || null,
        },
      });

      pushToast("success", "Meal plan assignment updated.");
      closeEditAssignment();
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to update assignment."));
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/40 sm:flex sm:items-center sm:justify-center sm:p-4">
      <div className="flex h-full w-full flex-col bg-white sm:h-auto sm:max-h-[90vh] sm:max-w-xl sm:rounded-2xl sm:shadow-xl">
        <div className="flex items-start justify-between gap-4 border-b p-4 sm:border-b-0 sm:p-6">
          <div>
            <h3 className="text-lg font-semibold sm:text-xl">Edit assignment</h3>
            <p className="mt-1 text-sm text-slate-500">
              Update recipe, variation, portion, and notes for {editingAssignment.assignment.personName}.
            </p>
          </div>

          <button
            type="button"
            onClick={closeEditAssignment}
            className="rounded-lg border px-3 py-2 text-sm"
          >
            Close
          </button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">Assigned recipe</label>
              <select
                {...form.register("assignedRecipeId")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              >
                <option value="">Select recipe</option>
                {recipes.map((recipe) => (
                  <option key={recipe.id} value={recipe.id}>
                    {recipe.name}
                  </option>
                ))}
              </select>
              {form.formState.errors.assignedRecipeId ? (
                <p className="mt-1 text-sm text-red-600">
                  {form.formState.errors.assignedRecipeId.message}
                </p>
              ) : null}
            </div>

            <div>
              <label className="text-sm font-medium">Variation</label>
              <select
                {...form.register("recipeVariationId")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              >
                <option value="">No variation</option>
                {variationOptions.map((variation) => (
                  <option key={variation.id} value={variation.id}>
                    {variation.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-sm font-medium">Portion multiplier</label>
              <input
                type="number"
                step="0.01"
                {...form.register("portionMultiplier")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
              {form.formState.errors.portionMultiplier ? (
                <p className="mt-1 text-sm text-red-600">
                  {form.formState.errors.portionMultiplier.message}
                </p>
              ) : null}
            </div>

            <div>
              <label className="text-sm font-medium">Notes</label>
              <textarea
                rows={4}
                {...form.register("notes")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
            </div>

            {mutation.isError ? (
              <p className="text-sm text-red-600">
                Failed to update assignment.
              </p>
            ) : null}
          </div>

          <div className="mt-6 flex flex-col gap-3 border-t pt-4 sm:flex-row">
            <LoadingButton
              type="submit"
              isLoading={mutation.isPending}
              loadingText="Saving..."
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
            >
              Save changes
            </LoadingButton>

            <button
              type="button"
              onClick={closeEditAssignment}
              className="rounded-lg border px-4 py-2 text-sm"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}