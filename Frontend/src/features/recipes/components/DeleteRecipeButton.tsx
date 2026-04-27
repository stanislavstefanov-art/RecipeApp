import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useDeleteRecipe } from "../hooks/useDeleteRecipe";

type Props = {
  recipeId: string;
};

export function DeleteRecipeButton({ recipeId }: Props) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const mutation = useDeleteRecipe();

  const onDelete = async () => {
    const confirmed = window.confirm(t('recipes.confirmDelete'));
    if (!confirmed) return;

    await mutation.mutateAsync(recipeId);
    navigate("/recipes");
  };

  return (
    <button
      type="button"
      onClick={onDelete}
      disabled={mutation.isPending}
      className="rounded-lg border border-red-300 px-4 py-2 text-sm text-red-700"
    >
      {mutation.isPending ? t('recipes.deleting') : t('recipes.deleteRecipe')}
    </button>
  );
}
