import type { z } from "zod";
import { mealPlanSuggestionSchema } from "../schemas";
import { getMealScopeLabel, getMealTypeLabel } from "../utils";
import { MealPlanSuggestionAssignmentCard } from "./MealPlanSuggestionAssignmentCard";

type SuggestionEntry = z.infer<typeof mealPlanSuggestionSchema>["entries"][number];

type Props = {
  entry: SuggestionEntry;
};

export function MealPlanSuggestionEntryCard({ entry }: Props) {
  return (
    <section className="rounded-xl border bg-white p-6">
      <div>
        <h4 className="text-lg font-medium">Base recipe ID: {entry.baseRecipeId}</h4>
        <p className="text-sm text-slate-500">
          {getMealTypeLabel(entry.mealType)} · {getMealScopeLabel(entry.scope)}
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