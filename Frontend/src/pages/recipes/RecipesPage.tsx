import { Link } from "react-router-dom";
import { useRecipes } from "../../features/recipes/hooks/useRecipes";
import {
  LoadingState,
  ErrorState,
  EmptyState,
} from "../../components/ui/PageState";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function RecipesPage() {
  const { data, isLoading, isError, error } = useRecipes();

  if (isLoading) return <LoadingState title="Loading recipes" />;

  if (isError)
    return (
      <ErrorState
        title="Failed to load recipes"
        message={error instanceof Error ? error.message : "Unknown error"}
      />
    );

  if (!data || data.length === 0)
    return (
      <EmptyState
        title="No recipes yet"
        message="Create your first recipe."
      />
    );

  return (
    <div className="space-y-6">
      <SectionHeader
        title="Recipes"
        description="Browse recipes and open details."
        action={
            <Link
            to="/recipes/new"
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white"
            >
            New recipe
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
          </Link>
        ))}
      </div>
    </div>
  );
}