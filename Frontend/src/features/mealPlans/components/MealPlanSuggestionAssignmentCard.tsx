import type { z } from "zod";
import { mealPlanSuggestionSchema } from "../schemas";

type SuggestionAssignment = z.infer<typeof mealPlanSuggestionSchema>["entries"][number]["assignments"][number];

type Props = {
  assignment: SuggestionAssignment;
};

export function MealPlanSuggestionAssignmentCard({ assignment }: Props) {
  return (
    <div className="rounded-lg border bg-slate-50 p-4">
      <p className="text-sm text-slate-700">
        Person ID: {assignment.personId}
      </p>
      <p className="mt-1 text-sm text-slate-700">
        Assigned recipe ID: {assignment.assignedRecipeId}
      </p>
      {assignment.recipeVariationId ? (
        <p className="mt-1 text-sm text-slate-700">
          Variation ID: {assignment.recipeVariationId}
        </p>
      ) : null}
      <p className="mt-1 text-sm text-slate-700">
        Portion: {assignment.portionMultiplier}
      </p>
      {assignment.notes ? (
        <p className="mt-2 text-sm text-slate-700">{assignment.notes}</p>
      ) : null}
    </div>
  );
}