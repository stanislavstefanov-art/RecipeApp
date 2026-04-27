import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import { useMealPlans } from "../../mealPlans/hooks/useMealPlans";
import {
  generateShoppingListFromMealPlanSchema,
  type GenerateShoppingListFromMealPlanInput,
} from "../schemas";
import { useGenerateShoppingListFromMealPlan } from "../hooks/useGenerateShoppingListFromMealPlan";
import { useToastStore } from "../../../stores/toastStore";
import { getErrorMessage } from "../../../lib/getErrorMessage";

type Props = {
  shoppingListId: string;
};

export function GenerateFromMealPlanForm({ shoppingListId }: Props) {
  const { t } = useTranslation();
  const { data: mealPlans = [] } = useMealPlans();
  const mutation = useGenerateShoppingListFromMealPlan();
  const pushToast = useToastStore((s) => s.pushToast);

  const schema = useMemo(() => generateShoppingListFromMealPlanSchema(t), [t]);
  const form = useForm<GenerateShoppingListFromMealPlanInput>({
    resolver: zodResolver(schema),
    defaultValues: {
      mealPlanId: "",
      shoppingListId,
    },
  });

  const onSubmit = async (values: GenerateShoppingListFromMealPlanInput) => {
    try {
      await mutation.mutateAsync(values);
      pushToast("success", t('shoppingLists.generate'));
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3 rounded-xl border bg-white p-6">
      <h3 className="text-lg font-medium">{t('shoppingLists.generate')}</h3>

      <div>
        <label className="text-sm font-medium">{t('mealPlans.title')}</label>
        <select
          {...form.register("mealPlanId")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        >
          <option value="">{t('shoppingLists.selectMealPlan')}</option>
          {mealPlans.map((mealPlan) => (
            <option key={mealPlan.id} value={mealPlan.id}>
              {mealPlan.name} ({mealPlan.householdName})
            </option>
          ))}
        </select>
        {form.formState.errors.mealPlanId ? (
          <p className="mt-1 text-sm text-red-600">
            {form.formState.errors.mealPlanId.message}
          </p>
        ) : null}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">{t('common.error')}</p>
      ) : null}

      <button
        type="submit"
        disabled={mutation.isPending}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        {mutation.isPending ? t('common.generate') + '…' : t('common.generate')}
      </button>
    </form>
  );
}
