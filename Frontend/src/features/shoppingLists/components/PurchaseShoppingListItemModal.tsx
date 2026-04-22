import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  purchaseShoppingListItemSchema,
  type PurchaseShoppingListItemInput,
  type PurchaseShoppingListItemData,
} from "../schemas";
import { useShoppingListUiStore } from "../store/useShoppingListUiStore";
import { usePurchaseShoppingListItemWithExpense } from "../hooks/usePurchaseShoppingListItemWithExpense";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

type Props = {
  shoppingListId: string;
};

export function PurchaseShoppingListItemModal({ shoppingListId }: Props) {
  const purchasingItem = useShoppingListUiStore((s) => s.purchasingItem);
  const closePurchaseModal = useShoppingListUiStore((s) => s.closePurchaseModal);
  const mutation = usePurchaseShoppingListItemWithExpense(shoppingListId);
  const pushToast = useToastStore((s) => s.pushToast);

  const today = new Date().toISOString().slice(0, 10);

  const form = useForm<PurchaseShoppingListItemInput, unknown, PurchaseShoppingListItemData>({
    resolver: zodResolver(purchaseShoppingListItemSchema),
    values: purchasingItem
      ? {
          amount: 1,
          currency: "BGN",
          expenseDate: today,
          description: purchasingItem.item.productName,
        }
      : {
          amount: 1,
          currency: "BGN",
          expenseDate: today,
          description: "",
        },
  });

  if (!purchasingItem) {
    return null;
  }

  const onSubmit = async (values: PurchaseShoppingListItemData) => {
    try {
      await mutation.mutateAsync({
        shoppingListItemId: purchasingItem.item.id,
        input: {
          ...values,
          description: values.description || null,
        },
      });

      pushToast("success", "Item purchased and expense created.");
      closePurchaseModal();
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to purchase item."));
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/40 sm:flex sm:items-center sm:justify-center sm:p-4">
      <div className="flex h-full w-full flex-col bg-white sm:h-auto sm:max-h-[90vh] sm:max-w-lg sm:rounded-2xl sm:shadow-xl">
        <div className="flex items-start justify-between gap-4 border-b p-4 sm:border-b-0 sm:p-6">
          <div>
            <h3 className="text-lg font-semibold sm:text-xl">Purchase item</h3>
            <p className="mt-1 text-sm text-slate-500">
              Create an expense for {purchasingItem.item.productName}.
            </p>
          </div>

          <button
            type="button"
            onClick={closePurchaseModal}
            className="rounded-lg border px-3 py-2 text-sm"
          >
            Close
          </button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">Amount</label>
              <input
                type="number"
                step="0.01"
                {...form.register("amount")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
              {form.formState.errors.amount ? (
                <p className="mt-1 text-sm text-red-600">
                  {form.formState.errors.amount.message}
                </p>
              ) : null}
            </div>

            <div>
              <label className="text-sm font-medium">Currency</label>
              <input
                {...form.register("currency")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
              {form.formState.errors.currency ? (
                <p className="mt-1 text-sm text-red-600">
                  {form.formState.errors.currency.message}
                </p>
              ) : null}
            </div>

            <div>
              <label className="text-sm font-medium">Expense date</label>
              <input
                type="date"
                {...form.register("expenseDate")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
              {form.formState.errors.expenseDate ? (
                <p className="mt-1 text-sm text-red-600">
                  {form.formState.errors.expenseDate.message}
                </p>
              ) : null}
            </div>

            <div>
              <label className="text-sm font-medium">Description</label>
              <textarea
                rows={4}
                {...form.register("description")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
            </div>

            {mutation.isError ? (
              <p className="text-sm text-red-600">Failed to purchase item.</p>
            ) : null}
          </div>

          <div className="mt-6 flex flex-col gap-3 border-t pt-4 sm:flex-row">
            <LoadingButton
              type="submit"
              isLoading={mutation.isPending}
              loadingText="Saving..."
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
            >
              Purchase
            </LoadingButton>

            <button
              type="button"
              onClick={closePurchaseModal}
              className="rounded-lg border px-4 py-2 text-sm"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}