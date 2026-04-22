import { create } from "zustand";
import type { MealPlanSuggestion, SuggestMealPlanInput } from "../schemas";

type MealPlanSuggestionStore = {
  request: SuggestMealPlanInput | null;
  suggestion: MealPlanSuggestion | null;
  setSuggestion: (request: SuggestMealPlanInput, suggestion: MealPlanSuggestion) => void;
  clearSuggestion: () => void;
};

export const useMealPlanSuggestionStore = create<MealPlanSuggestionStore>((set) => ({
  request: null,
  suggestion: null,
  setSuggestion: (request, suggestion) => set({ request, suggestion }),
  clearSuggestion: () => set({ request: null, suggestion: null }),
}));