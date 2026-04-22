import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createShoppingListSchema,
  type CreateShoppingListInput,
  type CreateShoppingListData,
} from "../schemas";
import { useCreateShoppingList } from "../hooks/useCreateShoppingList";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

export function CreateShoppingListForm() {
  const mutation = useCreateShoppingList();
  const pushToast = useToastStore((s) => s.pushToast);

  const form = useForm<CreateShoppingListInput, unknown, CreateShoppingListData>({
    resolver: zodResolver(createShoppingListSchema),
    defaultValues: {
      name: "",
    },
  });

  const onSubmit = async (values: CreateShoppingListData) => {
    try {
      await mutation.mutateAsync(values);
      form.reset();
      pushToast("success", "Shopping list created.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to create shopping list."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-xl border bg-white p-6">
      <div>
        <label className="text-sm font-medium">Shopping list name</label>
        <input {...form.register("name")} className="mt-1 w-full rounded-lg border px-3 py-2" />
        {form.formState.errors.name ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to create shopping list.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Creating..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Create shopping list
      </LoadingButton>
    </form>
  );
}