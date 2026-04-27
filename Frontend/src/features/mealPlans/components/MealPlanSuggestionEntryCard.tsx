import { useTranslation } from "react-i18next";
import type { z } from "zod";
import { mealPlanSuggestionSchema } from "../schemas";
import { MealPlanSuggestionAssignmentCard } from "./MealPlanSuggestionAssignmentCard";

type SuggestionEntry = z.infer<typeof mealPlanSuggestionSchema>["entries"][number];

type Props = {
  entry: SuggestionEntry;
};

export function MealPlanSuggestionEntryCard({ entry }: Props) {
  const { t } = useTranslation();
  return (
    <section className="rounded-xl border bg-white p-6">
      <div>
        <h4 className="text-lg font-medium">{t('mealPlans.baseRecipeId', { id: entry.baseRecipeId })}</h4>
        <p className="text-sm text-slate-500">
          {t('enums.mealType.' + entry.mealType)} · {t('enums.mealScope.' + entry.scope)}
        </p>
      </div>

      <div className="mt-4 grid gap-3">
        {entry.assignments.map((assignment, index) => (
          <MealPlanSuggestionAssignmentCard
            key={`${entry.baseRecipeId}-${assignment.personId}-${index}`}
            assignment={assignment}
          />
        ))}
      </div>
    </section>
  );
}