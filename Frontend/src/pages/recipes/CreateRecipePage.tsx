import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  createRecipeSchema,
  type CreateRecipeInput,
} from "../../features/recipes/schemas";
import { useCreateRecipe } from "../../features/recipes/hooks/useCreateRecipe";

export function CreateRecipePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const mutation = useCreateRecipe();

  const schema = useMemo(() => createRecipeSchema(t), [t]);
  const form = useForm<CreateRecipeInput>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (values: CreateRecipeInput) => {
    const recipe = await mutation.mutateAsync(values);
    navigate(`/recipes/${recipe.id}`);
  };

  return (
    <div className="max-w-xl space-y-6">
      <h2 className="text-2xl font-semibold">{t('recipes.createRecipe')}</h2>

      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-4 border p-6 rounded-xl bg-white"
      >
        <div>
          <label className="text-sm font-medium">{t('recipes.name')}</label>
          <input
            {...form.register("name")}
            placeholder={t('recipes.namePlaceholder')}
            className="w-full border px-3 py-2 rounded-lg mt-1"
          />
          {form.formState.errors.name && (
            <p className="text-sm text-red-600">
              {form.formState.errors.name.message}
            </p>
          )}
        </div>

        <button
          type="submit"
          className="bg-slate-900 text-white px-4 py-2 rounded-lg"
        >
          {t('common.create')}
        </button>
      </form>
    </div>
  );
}
