import { Link } from "react-router-dom";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreateHouseholdForm } from "../../features/households/components/CreateHouseholdForm";
import { useHouseholds } from "../../features/households/hooks/useHouseholds";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function HouseholdsPage() {
  const { data, isLoading, isError, error } = useHouseholds();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title="Households"
          description="Create households and group persons for meal planning."
        />

        {isLoading ? (
          <LoadingState title="Loading households" />
        ) : isError ? (
          <ErrorState
            title="Failed to load households"
            message={error instanceof Error ? error.message : "Unknown error"}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title="No households yet" message="Create your first household." />
        ) : (
          <div className="grid gap-4">
            {data.map((household) => (
              <Link
                key={household.id}
                to={`/households/${household.id}`}
                className="rounded-xl border bg-white p-5 hover:shadow-sm"
              >
                <h3 className="font-medium">{household.name}</h3>
                <p className="mt-1 text-sm text-slate-500">
                  Members: {household.memberCount}
                </p>
              </Link>
            ))}
          </div>
        )}
      </div>

      <div>
        <CreateHouseholdForm />
      </div>
    </div>
  );
}