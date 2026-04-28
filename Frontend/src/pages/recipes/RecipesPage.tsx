import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useRecipes } from "../../features/recipes/hooks/useRecipes";
import {
  LoadingState,
  ErrorState,
  EmptyState,
} from "../../components/ui/PageState";
import { SectionHeader } from "../../components/ui/SectionHeader";
import { StarRating } from "../../components/ui/StarRating";

export function RecipesPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = useRecipes();

  if (isLoading) return <LoadingState title={t('recipes.title')} />;

  if (isError)
    return (
      <ErrorState
        title={t('recipes.title')}
        message={error instanceof Error ? error.message : undefined}
      />
    );

  if (!data || data.length === 0)
    return (
      <EmptyState
        title={t('recipes.noRecipes')}
        message={t('recipes.noRecipesDesc')}
      />
    );

  return (
    <div className="space-y-6">
      <SectionHeader
        title={t('recipes.title')}
        description={t('recipes.descriptionBrowse')}
        action={
            <Link
            to="/recipes/new"
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white"
            >
            {t('recipes.newRecipe')}
            </Link>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {data.map((r) => (
          <Link
            key={r.id}
            to={`/recipes/${r.id}`}
            className="border rounded-xl p-4 bg-white hover:shadow"
          >
            <h3 className="font-medium">{r.name}</h3>
            {r.averageStars != null && (
              <div className="mt-1 flex items-center gap-1 text-sm text-slate-500">
                <StarRating value={r.averageStars} size="sm" />
                <span>{r.averageStars.toFixed(1)} ({r.ratingCount})</span>
              </div>
            )}
          </Link>
        ))}
      </div>
    </div>
  );
}
