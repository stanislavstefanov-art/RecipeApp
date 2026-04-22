import { Link } from "react-router-dom";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreatePersonForm } from "../../features/persons/components/CreatePersonForm";
import { usePersons } from "../../features/persons/hooks/usePersons";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function PersonsPage() {
  const { data, isLoading, isError, error } = usePersons();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title="Persons"
          description="Define household members, dietary preferences, and health concerns."
        />

        {isLoading ? (
          <LoadingState title="Loading persons" />
        ) : isError ? (
          <ErrorState
            title="Failed to load persons"
            message={error instanceof Error ? error.message : "Unknown error"}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title="No persons yet" message="Create your first person profile." />
        ) : (
          <div className="grid gap-4">
            {data.map((person) => (
              <Link
                key={person.id}
                to={`/persons/${person.id}`}
                className="rounded-xl border bg-white p-5 hover:shadow-sm"
              >
                <h3 className="font-medium">{person.name}</h3>
                <p className="mt-1 text-sm text-slate-500">
                  Dietary: {person.dietaryPreferences.length} · Health: {person.healthConcerns.length}
                </p>
              </Link>
            ))}
          </div>
        )}
      </div>

      <div>
        <CreatePersonForm />
      </div>
    </div>
  );
}