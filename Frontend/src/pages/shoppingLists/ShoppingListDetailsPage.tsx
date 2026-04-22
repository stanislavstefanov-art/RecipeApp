import { useState } from "react";
import { Link, useParams } from "react-router-dom";
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
  const { shoppingListId = "" } = useParams();
  const { data, isLoading, isError, error } = useShoppingList(shoppingListId);
  const pendingMutation = useMarkShoppingListItemPending(shoppingListId);
  const regenerateMutation = useRegenerateShoppingListFromMealPlan();
  const { data: mealPlans = [] } = useMealPlans();
  const pushToast = useToastStore((s) => s.pushToast);
  const [mealPlanIdForRegeneration, setMealPlanIdForRegeneration] = useState("");

  if (isLoading) return <LoadingState title="Loading shopping list" />;

  if (isError) {
    return (
      <ErrorState
        title="Failed to load shopping list"
        message={error instanceof Error ? error.message : "Unknown error"}
      />
    );
  }

  if (!data) return <EmptyState title="Shopping list not found" />;

  const onMarkPending = async (shoppingListItemId: string) => {
    try {
      await pendingMutation.mutateAsync(shoppingListItemId);
      pushToast("success", "Item marked as pending.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to update item."));
    }
  };

  const onRegenerate = async () => {
    if (!mealPlanIdForRegeneration) return;

    try {
      await regenerateMutation.mutateAsync({
        mealPlanId: mealPlanIdForRegeneration,
        shoppingListId: data.id,
      });
      pushToast("success", "Shopping list regenerated.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to regenerate shopping list."));
    }
  };

  return (
    <>
      <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
        <div className="space-y-5 sm:space-y-6">
          <Link to="/shopping-lists" className="text-sm text-slate-500">
            ← Back
          </Link>

          <div className="rounded-xl border bg-white p-5 sm:p-6">
            <SectionHeader
              title={data.name}
              description={`Items: ${data.items.length}`}
            />
          </div>

          {data.items.length === 0 ? (
            <EmptyState
              title="No shopping list items"
              message="Generate from a meal plan or add items later."
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
            <h3 className="text-base font-medium sm:text-lg">Regenerate from meal plan</h3>

            <div>
              <label className="text-sm font-medium">Meal plan</label>
              <select
                value={mealPlanIdForRegeneration}
                onChange={(e) => setMealPlanIdForRegeneration(e.target.value)}
                className="mt-1 w-full rounded-lg border px-3 py-2"
              >
                <option value="">Select meal plan</option>
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
              loadingText="Regenerating..."
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white disabled:opacity-60"
            >
              Regenerate
            </LoadingButton>
          </div>
        </div>
      </div>

      <PurchaseShoppingListItemModal shoppingListId={data.id} />
    </>
  );
}