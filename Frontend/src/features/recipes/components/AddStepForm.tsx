import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  addStepSchema,
  type AddStepInput,
  type AddStepData,
} from "../schemas";
import { useAddStep } from "../hooks/useAddStep";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

type Props = {
  recipeId: string;
};

export function AddStepForm({ recipeId }: Props) {
  const mutation = useAddStep(recipeId);
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<AddStepInput, unknown, AddStepData>({
    resolver: zodResolver(addStepSchema),
    defaultValues: {
      instruction: "",
    },
  });

  const onSubmit = async (values: AddStepData) => {
    try {
      await mutation.mutateAsync(values);
      form.reset({ instruction: "" });
      pushToast("success", "Step added.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to add step."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
      <div>
        <label className="text-sm font-medium">Instruction</label>
        <textarea
          {...form.register("instruction")}
          rows={4}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.instruction ? (
          <p className="mt-1 text-sm text-red-600">
            {form.formState.errors.instruction.message}
          </p>
        ) : null}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to add step.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Adding..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Add step
      </LoadingButton>
    </form>
  );
}