import { create } from "zustand";

export type ToastKind = "success" | "error" | "info";

export type ToastItem = {
  id: string;
  kind: ToastKind;
  message: string;
};

type ToastStore = {
  toasts: ToastItem[];
  pushToast: (kind: ToastKind, message: string) => void;
  removeToast: (id: string) => void;
};

function createToastId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

export const useToastStore = create<ToastStore>((set) => ({
  toasts: [],
  pushToast: (kind, message) => {
    const id = createToastId();

    set((state) => ({
      toasts: [...state.toasts, { id, kind, message }],
    }));

    window.setTimeout(() => {
      set((state) => ({
        toasts: state.toasts.filter((toast) => toast.id !== id),
      }));
    }, 3500);
  },
  removeToast: (id) =>
    set((state) => ({
      toasts: state.toasts.filter((toast) => toast.id !== id),
    })),
}));