import { create } from "zustand";

type UiStore = {
  isNavOpen: boolean;
  setNavOpen: (value: boolean) => void;
  toggleNav: () => void;
};

export const useUiStore = create<UiStore>((set) => ({
  isNavOpen: false,
  setNavOpen: (value) => set({ isNavOpen: value }),
  toggleNav: () => set((state) => ({ isNavOpen: !state.isNavOpen })),
}));