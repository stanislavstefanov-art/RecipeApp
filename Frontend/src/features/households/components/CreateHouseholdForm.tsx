import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createHouseholdSchema,
  type CreateHouseholdInput,
} from "../schemas";
import { useCreateHousehold } from "../hooks/useCreateHousehold";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

export function CreateHouseholdForm() {
  const mutation = useCreateHousehold();
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<CreateHouseholdInput>({
    resolver: zodResolver(createHouseholdSchema),
    defaultValues: {
      name: "",
    },
  });

  const onSubmit = async (values: CreateHouseholdInput) => {
    try {
      await mutation.mutateAsync(values);
      form.reset();
      pushToast("success", "Household created.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to create household."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-xl border bg-white p-6">
      <div>
        <label className="text-sm font-medium">Household name</label>
        <input {...form.register("name")} className="mt-1 w-full rounded-lg border px-3 py-2" />
        {form.formState.errors.name ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to create household.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Creating..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Create household
      </LoadingButton>
    </form>
  );
}