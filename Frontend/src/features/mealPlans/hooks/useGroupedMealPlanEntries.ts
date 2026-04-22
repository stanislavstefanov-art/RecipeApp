import { useMemo } from "react";
import type { MealPlanEntry } from "../schemas";

type GroupedMealPlanEntries = {
  date: string;
  entries: MealPlanEntry[];
};

export function useGroupedMealPlanEntries(entries: MealPlanEntry[] | undefined) {
  return useMemo<GroupedMealPlanEntries[]>(() => {
    if (!entries || entries.length === 0) {
      return [];
    }

    const groups = new Map<string, MealPlanEntry[]>();

    for (const entry of entries) {
      const existing = groups.get(entry.plannedDate) ?? [];
      existing.push(entry);
      groups.set(entry.plannedDate, existing);
    }

    return Array.from(groups.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([date, groupedEntries]) => ({
        date,
        entries: groupedEntries.slice().sort((x, y) => x.mealType - y.mealType),
      }));
  }, [entries]);
}