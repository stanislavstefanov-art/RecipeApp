import { create } from "zustand";
import type { MealPlanAssignment } from "../schemas";

type EditingAssignmentState = {
  mealPlanEntryId: string;
  assignment: MealPlanAssignment;
} | null;

type MealPlanUiStore = {
  editingAssignment: EditingAssignmentState;
  openEditAssignment: (mealPlanEntryId: string, assignment: MealPlanAssignment) => void;
  closeEditAssignment: () => void;
};

export const useMealPlanUiStore = create<MealPlanUiStore>((set) => ({
  editingAssignment: null,
  openEditAssignment: (mealPlanEntryId, assignment) =>
    set({
      editingAssignment: {
        mealPlanEntryId,
        assignment,
      },
    }),
  closeEditAssignment: () => set({ editingAssignment: null }),
}));