import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
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
  const { t } = useTranslation();
  const mutation = useUpdateRecipe(recipeId);
  const pushToast = useToastStore((s) => s.pushToast);

  const schema = useMemo(() => updateRecipeSchema(t), [t]);
  const form = useForm<UpdateRecipeInput, unknown, UpdateRecipeData>({
    resolver: zodResolver(schema),
    values: { name: initialName },
  });

  const onSubmit = async (values: UpdateRecipeData) => {
    try {
      await mutation.mutateAsync(values);
      pushToast("success", t('recipes.editName'));
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
      <div>
        <label className="text-sm font-medium">{t('recipes.recipeName')}</label>
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
        <p className="text-sm text-red-600">{t('common.error')}</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText={t('recipes.saving')}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        {t('recipes.saveName')}
      </LoadingButton>
    </form>
  );
}
