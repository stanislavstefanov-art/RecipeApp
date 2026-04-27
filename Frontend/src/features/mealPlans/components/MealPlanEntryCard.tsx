import { useTranslation } from "react-i18next";
import type { MealPlanEntry } from "../schemas";
import { MealPlanAssignmentCard } from "./MealPlanAssignmentCard";

type Props = {
  entry: MealPlanEntry;
};

export function MealPlanEntryCard({ entry }: Props) {
  const { t } = useTranslation();
  return (
    <section className="rounded-xl border bg-white p-5 sm:p-6">
      <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
        <div>
          <h4 className="text-base font-medium sm:text-lg">{entry.baseRecipeName}</h4>
          <p className="text-sm text-slate-500">
            {t('enums.mealType.' + entry.mealType)} · {t('enums.mealScope.' + entry.scope)}
          </p>
        </div>
      </div>

      <div className="mt-4 grid gap-3">
        {entry.assignments.map((assignment) => (
          <MealPlanAssignmentCard
            key={`${entry.id}-${assignment.personId}`}
            mealPlanEntryId={entry.id}
            assignment={assignment}
          />
        ))}
      </div>
    </section>
  );
}