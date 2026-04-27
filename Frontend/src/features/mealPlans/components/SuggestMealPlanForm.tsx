import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useHouseholds } from "../../households/hooks/useHouseholds";
import {
  suggestMealPlanInputSchema,
  type SuggestMealPlanInput,
  type SuggestMealPlanData,
} from "../schemas";
import { useSuggestMealPlan } from "../hooks/useSuggestMealPlan";
import { useMealPlanSuggestionStore } from "../store/useMealPlanSuggestionStore";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

const MEAL_TYPE_VALUES = [1, 2, 3];

export function SuggestMealPlanForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { data: households = [] } = useHouseholds();
  const mutation = useSuggestMealPlan();
  const setSuggestion = useMealPlanSuggestionStore((s) => s.setSuggestion);
  const pushToast = useToastStore((s) => s.pushToast);

  const schema = useMemo(() => suggestMealPlanInputSchema(t), [t]);
  const form = useForm<SuggestMealPlanInput, unknown, SuggestMealPlanData>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: "",
      householdId: "",
      startDate: "",
      numberOfDays: 7,
      mealTypes: [3],
    },
  });

  const onSubmit = async (values: SuggestMealPlanData) => {
    try {
      const suggestion = await mutation.mutateAsync(values);
      setSuggestion(values, suggestion);
      pushToast("success", t('mealPlans.suggest'));
      navigate("/meal-plans/suggest/review");
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-4 rounded-xl border bg-white p-6"
    >
      <div>
        <label className="text-sm font-medium">{t('mealPlans.planName')}</label>
        <input
          {...form.register("name")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.name ? (
          <p className="mt-1 text-sm text-red-600">
            {form.formState.errors.name.message}
          </p>
        ) : null}
      </div>

      <div>
        <label className="text-sm font-medium">{t('mealPlans.householdId')}</label>
        <select
          {...form.register("householdId")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        >
          <option value="">{t('mealPlans.selectHousehold')}</option>
          {households.map((household) => (
            <option key={household.id} value={household.id}>
              {household.name}
            </option>
          ))}
        </select>
        {form.formState.errors.householdId ? (
          <p className="mt-1 text-sm text-red-600">
            {form.formState.errors.householdId.message}
          </p>
        ) : null}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <label className="text-sm font-medium">{t('mealPlans.startDate')}</label>
          <input
            type="date"
            {...form.register("startDate")}
            className="mt-1 w-full rounded-lg border px-3 py-2"
          />
          {form.formState.errors.startDate ? (
            <p className="mt-1 text-sm text-red-600">
              {form.formState.errors.startDate.message}
            </p>
          ) : null}
        </div>

        <div>
          <label className="text-sm font-medium">{t('mealPlans.numberOfDays')}</label>
          <input
            type="number"
            {...form.register("numberOfDays")}
            className="mt-1 w-full rounded-lg border px-3 py-2"
          />
          {form.formState.errors.numberOfDays ? (
            <p className="mt-1 text-sm text-red-600">
              {form.formState.errors.numberOfDays.message}
            </p>
          ) : null}
        </div>
      </div>

      <div>
        <label className="text-sm font-medium">{t('mealPlans.mealTypesLabel')}</label>
        <div className="mt-2 grid gap-2">
          {MEAL_TYPE_VALUES.map((value) => (
            <label key={value} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                value={value}
                {...form.register("mealTypes")}
              />
              {t('enums.mealType.' + value)}
            </label>
          ))}
        </div>
        {form.formState.errors.mealTypes ? (
          <p className="mt-1 text-sm text-red-600">
            {form.formState.errors.mealTypes.message as string}
          </p>
        ) : null}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">
          {t('common.error')}
        </p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText={t('mealPlans.generatingPlan')}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        {t('mealPlans.suggest')}
      </LoadingButton>
    </form>
  );
}
