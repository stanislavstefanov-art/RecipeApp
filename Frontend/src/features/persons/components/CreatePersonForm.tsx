import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createPersonSchema,
  type CreatePersonInput,
  type CreatePersonData,
} from "../schemas";
import { useCreatePerson } from "../hooks/useCreatePerson";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

const dietaryOptions = [
  { value: 1, label: "Vegetarian" },
  { value: 2, label: "Pescatarian" },
  { value: 3, label: "Vegan" },
  { value: 4, label: "High protein" },
];

const healthOptions = [
  { value: 1, label: "Diabetes" },
  { value: 2, label: "High blood pressure" },
  { value: 3, label: "Gluten intolerance" },
];

export function CreatePersonForm() {
  const mutation = useCreatePerson();
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<CreatePersonInput, unknown, CreatePersonData>({
    resolver: zodResolver(createPersonSchema),
    defaultValues: {
      name: "",
      dietaryPreferences: [],
      healthConcerns: [],
      notes: "",
    },
  });

  const onSubmit = async (values: CreatePersonData) => {
    try {
      await mutation.mutateAsync(values);
      form.reset({
        name: "",
        dietaryPreferences: [],
        healthConcerns: [],
        notes: "",
      });
      pushToast("success", "Person created.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to create person."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-xl border bg-white p-6">
      <div>
        <label className="text-sm font-medium">Name</label>
        <input {...form.register("name")} className="mt-1 w-full rounded-lg border px-3 py-2" />
        {form.formState.errors.name ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      <div>
        <label className="text-sm font-medium">Dietary preferences</label>
        <div className="mt-2 grid gap-2">
          {dietaryOptions.map((option) => (
            <label key={option.value} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                value={option.value}
                {...form.register("dietaryPreferences", { valueAsNumber: true })}
              />
              {option.label}
            </label>
          ))}
        </div>
      </div>

      <div>
        <label className="text-sm font-medium">Health concerns</label>
        <div className="mt-2 grid gap-2">
          {healthOptions.map((option) => (
            <label key={option.value} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                value={option.value}
                {...form.register("healthConcerns", { valueAsNumber: true })}
              />
              {option.label}
            </label>
          ))}
        </div>
      </div>

      <div>
        <label className="text-sm font-medium">Notes</label>
        <textarea {...form.register("notes")} rows={3} className="mt-1 w-full rounded-lg border px-3 py-2" />
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to create person.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Creating..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Create person
      </LoadingButton>
    </form>
  );
}