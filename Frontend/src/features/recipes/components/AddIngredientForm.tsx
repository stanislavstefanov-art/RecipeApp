import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  addIngredientSchema,
  type AddIngredientInput,
  type AddIngredientData,
} from "../schemas";
import { useAddIngredient } from "../hooks/useAddIngredient";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

type Props = {
  recipeId: string;
};

export function AddIngredientForm({ recipeId }: Props) {
  const mutation = useAddIngredient(recipeId);
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<AddIngredientInput, unknown, AddIngredientData>({
    resolver: zodResolver(addIngredientSchema),
    defaultValues: {
      name: "",
      quantity: 1,
      unit: "",
    },
  });

  const onSubmit = async (values: AddIngredientData) => {
    try {
      await mutation.mutateAsync(values);
      form.reset({ name: "", quantity: 1, unit: "" });
      pushToast("success", "Ingredient added.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to add ingredient."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
      <div>
        <label className="text-sm font-medium">Ingredient name</label>
        <input {...form.register("name")} className="mt-1 w-full rounded-lg border px-3 py-2" />
        {form.formState.errors.name ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="text-sm font-medium">Quantity</label>
          <input
            type="number"
            step="0.01"
            {...form.register("quantity")}
            className="mt-1 w-full rounded-lg border px-3 py-2"
          />
          {form.formState.errors.quantity ? (
            <p className="mt-1 text-sm text-red-600">{form.formState.errors.quantity.message}</p>
          ) : null}
        </div>

        <div>
          <label className="text-sm font-medium">Unit</label>
          <input {...form.register("unit")} className="mt-1 w-full rounded-lg border px-3 py-2" />
          {form.formState.errors.unit ? (
            <p className="mt-1 text-sm text-red-600">{form.formState.errors.unit.message}</p>
          ) : null}
        </div>
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to add ingredient.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Adding..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Add ingredient
      </LoadingButton>
    </form>
  );
}