import { useState } from "react";
import { usePersons } from "../../persons/hooks/usePersons";
import { useAddPersonToHousehold } from "../hooks/useAddPersonToHousehold";

type Props = {
  householdId: string;
  existingPersonIds: string[];
};

export function AddHouseholdMemberForm({ householdId, existingPersonIds }: Props) {
  const [selectedPersonId, setSelectedPersonId] = useState("");
  const { data: persons } = usePersons();
  const mutation = useAddPersonToHousehold(householdId);

  const availablePersons = (persons ?? []).filter(
    (person) => !existingPersonIds.includes(person.id),
  );

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!selectedPersonId) return;

    await mutation.mutateAsync(selectedPersonId);
    setSelectedPersonId("");
  };

  return (
    <form onSubmit={onSubmit} className="space-y-3 rounded-xl border bg-white p-6">
      <h3 className="text-lg font-medium">Add member</h3>

      <select
        value={selectedPersonId}
        onChange={(e) => setSelectedPersonId(e.target.value)}
        className="w-full rounded-lg border px-3 py-2"
      >
        <option value="">Select a person</option>
        {availablePersons.map((person) => (
          <option key={person.id} value={person.id}>
            {person.name}
          </option>
        ))}
      </select>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to add member.</p>
      ) : null}

      <button
        type="submit"
        disabled={!selectedPersonId || mutation.isPending}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white disabled:opacity-60"
      >
        {mutation.isPending ? "Adding..." : "Add member"}
      </button>
    </form>
  );
}