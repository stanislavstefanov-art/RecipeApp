import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
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
  const { t } = useTranslation();
  const purchasingItem = useShoppingListUiStore((s) => s.purchasingItem);
  const closePurchaseModal = useShoppingListUiStore((s) => s.closePurchaseModal);
  const mutation = usePurchaseShoppingListItemWithExpense(shoppingListId);
  const pushToast = useToastStore((s) => s.pushToast);

  const today = new Date().toISOString().slice(0, 10);

  const schema = useMemo(() => purchaseShoppingListItemSchema(t), [t]);
  const form = useForm<PurchaseShoppingListItemInput, unknown, PurchaseShoppingListItemData>({
    resolver: zodResolver(schema),
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

      pushToast("success", t('shoppingLists.itemPurchased'));
      closePurchaseModal();
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/40 sm:flex sm:items-center sm:justify-center sm:p-4">
      <div className="flex h-full w-full flex-col bg-white sm:h-auto sm:max-h-[90vh] sm:max-w-lg sm:rounded-2xl sm:shadow-xl">
        <div className="flex items-start justify-between gap-4 border-b p-4 sm:border-b-0 sm:p-6">
          <div>
            <h3 className="text-lg font-semibold sm:text-xl">{t('shoppingLists.purchaseItem')}</h3>
            <p className="mt-1 text-sm text-slate-500">
              {t('shoppingLists.purchaseItemDesc', { name: purchasingItem.item.productName })}
            </p>
          </div>

          <button
            type="button"
            onClick={closePurchaseModal}
            className="rounded-lg border px-3 py-2 text-sm"
          >
            {t('common.close')}
          </button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">{t('expenses.amount')}</label>
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
              <label className="text-sm font-medium">{t('expenses.currency')}</label>
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
              <label className="text-sm font-medium">{t('expenses.expenseDate')}</label>
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
              <label className="text-sm font-medium">{t('expenses.description')}</label>
              <textarea
                rows={4}
                {...form.register("description")}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              />
            </div>

            {mutation.isError ? (
              <p className="text-sm text-red-600">{t('common.error')}</p>
            ) : null}
          </div>

          <div className="mt-6 flex flex-col gap-3 border-t pt-4 sm:flex-row">
            <LoadingButton
              type="submit"
              isLoading={mutation.isPending}
              loadingText={t('recipes.saving')}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
            >
              {t('shoppingLists.markPurchased')}
            </LoadingButton>

            <button
              type="button"
              onClick={closePurchaseModal}
              className="rounded-lg border px-4 py-2 text-sm"
            >
              {t('common.cancel')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
