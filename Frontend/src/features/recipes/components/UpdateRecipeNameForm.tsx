import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  updateRecipeSchema,
  type UpdateRecipeInput,
  type UpdateRecipeData,
} from "../schemas";
import { useUpdateRecipe } from "../hooks/useUpdateRecipe";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

type Props = {
  recipeId: string;
  initialName: string;
};

export function UpdateRecipeNameForm({ recipeId, initialName }: Props) {
  const mutation = useUpdateRecipe(recipeId);
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<UpdateRecipeInput, unknown, UpdateRecipeData>({
    resolver: zodResolver(updateRecipeSchema),
    values: { name: initialName },
  });

  const onSubmit = async (values: UpdateRecipeData) => {
    try {
      await mutation.mutateAsync(values);
      pushToast("success", "Recipe name updated.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to update recipe name."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
      <div>
        <label className="text-sm font-medium">Recipe name</label>
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

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to update recipe.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Saving..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Save name
      </LoadingButton>
    </form>
  );
}