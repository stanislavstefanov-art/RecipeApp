import type { MealPlanAssignment } from "../schemas";
import { useMealPlanUiStore } from "../store/useMealPlanUiStore";

type Props = {
  mealPlanEntryId: string;
  assignment: MealPlanAssignment;
};

export function MealPlanAssignmentCard({ mealPlanEntryId, assignment }: Props) {
  const openEditAssignment = useMealPlanUiStore((s) => s.openEditAssignment);

  return (
    <div className="rounded-lg border bg-slate-50 p-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h5 className="font-medium">{assignment.personName}</h5>
          <p className="mt-1 text-sm text-slate-600">
            Recipe: {assignment.assignedRecipeName}
          </p>

          {assignment.recipeVariationName ? (
            <p className="mt-1 text-sm text-slate-600">
              Variation: {assignment.recipeVariationName}
            </p>
          ) : null}

          <p className="mt-1 text-sm text-slate-600">
            Portion: {assignment.portionMultiplier}
          </p>

          {assignment.notes ? (
            <p className="mt-2 text-sm text-slate-700">{assignment.notes}</p>
          ) : null}
        </div>

        <button
          type="button"
          onClick={() => openEditAssignment(mealPlanEntryId, assignment)}
          className="rounded-lg border px-3 py-2 text-sm text-slate-700 hover:bg-white"
        >
          Edit
        </button>
      </div>
    </div>
  );
}