import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { GenerateFromMealPlanForm } from "../../features/shoppingLists/components/GenerateFromMealPlanForm";
import { PurchaseShoppingListItemModal } from "../../features/shoppingLists/components/PurchaseShoppingListItemModal";
import { ShoppingListItemRow } from "../../features/shoppingLists/components/ShoppingListItemRow";
import { useMarkShoppingListItemPending } from "../../features/shoppingLists/hooks/useMarkShoppingListItemPending";
import { useRegenerateShoppingListFromMealPlan } from "../../features/shoppingLists/hooks/useRegenerateShoppingListFromMealPlan";
import { useShoppingList } from "../../features/shoppingLists/hooks/useShoppingList";
import { useMealPlans } from "../../features/mealPlans/hooks/useMealPlans";
import { LoadingButton } from "../../components/ui/LoadingButton";
import { getErrorMessage } from "../../lib/getErrorMessage";
import { useToastStore } from "../../stores/toastStore";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function ShoppingListDetailsPage() {
  const { t } = useTranslation();
  const { shoppingListId = "" } = useParams();
  const { data, isLoading, isError, error } = useShoppingList(shoppingListId);
  const pendingMutation = useMarkShoppingListItemPending(shoppingListId);
  const regenerateMutation = useRegenerateShoppingListFromMealPlan();
  const { data: mealPlans = [] } = useMealPlans();
  const pushToast = useToastStore((s) => s.pushToast);
  const [mealPlanIdForRegeneration, setMealPlanIdForRegeneration] = useState("");

  if (isLoading) return <LoadingState title={t('shoppingLists.title')} />;

  if (isError) {
    return (
      <ErrorState
        title={t('shoppingLists.title')}
        message={error instanceof Error ? error.message : undefined}
      />
    );
  }

  if (!data) return <EmptyState title={t('shoppingLists.noShoppingLists')} />;

  const onMarkPending = async (shoppingListItemId: string) => {
    try {
      await pendingMutation.mutateAsync(shoppingListItemId);
      pushToast("success", t('shoppingLists.itemMarkedPending'));
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  const onRegenerate = async () => {
    if (!mealPlanIdForRegeneration) return;

    try {
      await regenerateMutation.mutateAsync({
        mealPlanId: mealPlanIdForRegeneration,
        shoppingListId: data.id,
      });
      pushToast("success", t('shoppingLists.listRegenerated'));
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <>
      <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
        <div className="space-y-5 sm:space-y-6">
          <Link to="/shopping-lists" className="text-sm text-slate-500">
            ← {t('common.back')}
          </Link>

          <div className="rounded-xl border bg-white p-5 sm:p-6">
            <SectionHeader
              title={data.name}
              description={t('shoppingLists.itemCountLabel', { count: data.items.length })}
            />
          </div>

          {data.items.length === 0 ? (
            <EmptyState
              title={t('shoppingLists.noItems')}
              message={t('shoppingLists.noItemsDesc')}
            />
          ) : (
            <div className="grid gap-3 sm:gap-4">
              {data.items.map((item) => (
                <ShoppingListItemRow
                  key={item.id}
                  item={item}
                  onMarkPending={onMarkPending}
                />
              ))}
            </div>
          )}
        </div>

        <div className="space-y-5 sm:space-y-6">
          <GenerateFromMealPlanForm shoppingListId={data.id} />

          <div className="rounded-xl border bg-white p-5 sm:p-6 space-y-3">
            <h3 className="text-base font-medium sm:text-lg">{t('shoppingLists.regenerateFromMealPlan')}</h3>

            <div>
              <label className="text-sm font-medium">{t('mealPlans.title')}</label>
              <select
                value={mealPlanIdForRegeneration}
                onChange={(e) => setMealPlanIdForRegeneration(e.target.value)}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              >
                <option value="">{t('shoppingLists.selectMealPlan')}</option>
                {mealPlans.map((mealPlan) => (
                  <option key={mealPlan.id} value={mealPlan.id}>
                    {mealPlan.name} ({mealPlan.householdName})
                  </option>
                ))}
              </select>
            </div>

            <LoadingButton
              type="button"
              onClick={onRegenerate}
              disabled={!mealPlanIdForRegeneration}
              isLoading={regenerateMutation.isPending}
              loadingText={t('shoppingLists.regenerating')}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white disabled:opacity-60"
            >
              {t('shoppingLists.regenerate')}
            </LoadingButton>
          </div>
        </div>
      </div>

      <PurchaseShoppingListItemModal shoppingListId={data.id} />
    </>
  );
}
