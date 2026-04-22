import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
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
  const { data: mealPlans = [] } = useMealPlans();
  const mutation = useGenerateShoppingListFromMealPlan();
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<GenerateShoppingListFromMealPlanInput>({
    resolver: zodResolver(generateShoppingListFromMealPlanSchema),
    defaultValues: {
      mealPlanId: "",
      shoppingListId,
    },
  });

  const onSubmit = async (values: GenerateShoppingListFromMealPlanInput) => {
    try {
      await mutation.mutateAsync(values);
      pushToast("success", "Shopping list generated from meal plan.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to generate shopping list."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3 rounded-xl border bg-white p-6">
      <h3 className="text-lg font-medium">Generate from meal plan</h3>

      <div>
        <label className="text-sm font-medium">Meal plan</label>
        <select
          {...form.register("mealPlanId")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        >
          <option value="">Select meal plan</option>
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
        <p className="text-sm text-red-600">Failed to generate shopping list.</p>
      ) : null}

      <button
        type="submit"
        disabled={mutation.isPending}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        {mutation.isPending ? "Generating..." : "Generate"}
      </button>
    </form>
  );
}