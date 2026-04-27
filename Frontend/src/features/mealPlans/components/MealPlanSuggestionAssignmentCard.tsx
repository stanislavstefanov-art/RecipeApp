import { useTranslation } from "react-i18next";
import type { z } from "zod";
import { mealPlanSuggestionSchema } from "../schemas";

type SuggestionAssignment = z.infer<typeof mealPlanSuggestionSchema>["entries"][number]["assignments"][number];

type Props = {
  assignment: SuggestionAssignment;
};

export function MealPlanSuggestionAssignmentCard({ assignment }: Props) {
  const { t } = useTranslation();
  return (
    <div className="rounded-lg border bg-slate-50 p-4">
      <p className="text-sm text-slate-700">
        {t('mealPlans.assignedTo')}: {assignment.personId}
      </p>
      <p className="mt-1 text-sm text-slate-700">
        {t('mealPlans.recipe')}: {assignment.assignedRecipeId}
      </p>
      {assignment.recipeVariationId ? (
        <p className="mt-1 text-sm text-slate-700">
          {t('mealPlans.variation')}: {assignment.recipeVariationId}
        </p>
      ) : null}
      <p className="mt-1 text-sm text-slate-700">
        {t('mealPlans.portions')}: {assignment.portionMultiplier}
      </p>
      {assignment.notes ? (
        <p className="mt-2 text-sm text-slate-700">{assignment.notes}</p>
      ) : null}
    </div>
  );
}
