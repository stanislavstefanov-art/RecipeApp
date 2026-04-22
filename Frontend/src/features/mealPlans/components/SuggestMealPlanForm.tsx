import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
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

const mealTypeOptions = [
  { value: 1, label: "Breakfast" },
  { value: 2, label: "Lunch" },
  { value: 3, label: "Dinner" },
];

export function SuggestMealPlanForm() {
  const navigate = useNavigate();
  const { data: households = [] } = useHouseholds();
  const mutation = useSuggestMealPlan();
  const setSuggestion = useMealPlanSuggestionStore((s) => s.setSuggestion);
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<SuggestMealPlanInput, unknown, SuggestMealPlanData>({
    resolver: zodResolver(suggestMealPlanInputSchema),
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
      pushToast("success", "Meal plan suggestion generated.");
      navigate("/meal-plans/suggest/review");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to generate meal plan suggestion."));
    }
  };

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-4 rounded-xl border bg-white p-6"
    >
      <div>
        <label className="text-sm font-medium">Plan name</label>
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
        <label className="text-sm font-medium">Household</label>
        <select
          {...form.register("householdId")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        >
          <option value="">Select household</option>
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
          <label className="text-sm font-medium">Start date</label>
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
          <label className="text-sm font-medium">Number of days</label>
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
        <label className="text-sm font-medium">Meal types</label>
        <div className="mt-2 grid gap-2">
          {mealTypeOptions.map((option) => (
            <label key={option.value} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                value={option.value}
                {...form.register("mealTypes")}
              />
              {option.label}
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
          Failed to generate meal plan suggestion.
        </p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Generating..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Suggest meal plan
      </LoadingButton>
    </form>
  );
}