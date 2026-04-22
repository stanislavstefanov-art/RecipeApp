import type { ShoppingListItem } from "../schemas";
import { useShoppingListUiStore } from "../store/useShoppingListUiStore";

type Props = {
  item: ShoppingListItem;
  onMarkPending: (shoppingListItemId: string) => void;
};

function getSourceTypeLabel(sourceType: number) {
  switch (sourceType) {
    case 1:
      return "Manual";
    case 2:
      return "Recipe";
    case 3:
      return "Meal plan";
    default:
      return `Source ${sourceType}`;
  }
}

export function ShoppingListItemRow({ item, onMarkPending }: Props) {
  const openPurchaseModal = useShoppingListUiStore((s) => s.openPurchaseModal);

  return (
    <div className="rounded-xl border bg-white p-4 sm:p-5">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div className="min-w-0">
          <h3 className={`font-medium ${item.isPurchased ? "line-through text-slate-500" : ""}`}>
            {item.productName}
          </h3>

          <p className="mt-1 text-sm text-slate-600">
            {item.quantity} {item.unit}
          </p>

          <p className="mt-1 text-sm text-slate-500">
            Source: {getSourceTypeLabel(item.sourceType)}
          </p>

          {item.notes ? (
            <p className="mt-2 text-sm text-slate-700">{item.notes}</p>
          ) : null}
        </div>

        <div className="flex w-full gap-2 md:w-auto">
          {item.isPurchased ? (
            <button
              type="button"
              onClick={() => onMarkPending(item.id)}
              className="w-full rounded-lg border px-3 py-2 text-sm md:w-auto"
            >
              Mark pending
            </button>
          ) : (
            <button
              type="button"
              onClick={() => openPurchaseModal(item)}
              className="w-full rounded-lg bg-slate-900 px-3 py-2 text-sm text-white md:w-auto"
            >
              Purchase
            </button>
          )}
        </div>
      </div>
    </div>
  );
}