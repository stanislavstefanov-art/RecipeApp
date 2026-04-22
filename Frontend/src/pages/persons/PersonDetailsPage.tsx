import { Link, useParams } from "react-router-dom";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { usePerson } from "../../features/persons/hooks/usePerson";
import {
  getDietaryPreferenceLabel,
  getHealthConcernLabel,
} from "../../features/persons/utils";

export function PersonDetailsPage() {
  const { personId = "" } = useParams();
  const { data, isLoading, isError, error } = usePerson(personId);

  if (isLoading) return <LoadingState title="Loading person" />;

  if (isError) {
    return (
      <ErrorState
        title="Failed to load person"
        message={error instanceof Error ? error.message : "Unknown error"}
      />
    );
  }

  if (!data) return <EmptyState title="Person not found" />;

  return (
    <div className="space-y-6">
      <Link to="/persons" className="text-sm text-slate-500">
        ← Back
      </Link>

      <div className="rounded-xl border bg-white p-6">
        <h2 className="text-2xl font-semibold">{data.name}</h2>

        <div className="mt-6 grid gap-6 md:grid-cols-2">
          <div>
            <h3 className="font-medium">Dietary preferences</h3>
            {data.dietaryPreferences.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">None</p>
            ) : (
              <ul className="mt-2 list-disc pl-5 text-sm text-slate-700">
                {data.dietaryPreferences.map((item, index) => (
                <li key={`${item}-${index}`}>{getDietaryPreferenceLabel(item)}</li>
                ))}
              </ul>
            )}
          </div>

          <div>
            <h3 className="font-medium">Health concerns</h3>
            {data.healthConcerns.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">None</p>
            ) : (
              <ul className="mt-2 list-disc pl-5 text-sm text-slate-700">
                {data.healthConcerns.map((item, index) => (
                <li key={`${item}-${index}`}>{getHealthConcernLabel(item)}</li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div className="mt-6">
          <h3 className="font-medium">Notes</h3>
          <p className="mt-2 text-sm text-slate-700">{data.notes || "No notes"}</p>
        </div>
      </div>
    </div>
  );
}