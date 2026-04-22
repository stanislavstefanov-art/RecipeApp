import { create } from "zustand";
import type { ShoppingListItem } from "../schemas";

type PurchaseState = {
  item: ShoppingListItem;
} | null;

type ShoppingListUiStore = {
  purchasingItem: PurchaseState;
  openPurchaseModal: (item: ShoppingListItem) => void;
  closePurchaseModal: () => void;
};

export const useShoppingListUiStore = create<ShoppingListUiStore>((set) => ({
  purchasingItem: null,
  openPurchaseModal: (item) => set({ purchasingItem: { item } }),
  closePurchaseModal: () => set({ purchasingItem: null }),
}));