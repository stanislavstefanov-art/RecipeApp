import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  createExpenseSchema,
  type CreateExpenseInput,
  type CreateExpenseData,
} from "../schemas";
import { useCreateExpense } from "../hooks/useCreateExpense";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

const categoryOptions = [
  { value: 1, label: "Food" },
  { value: 2, label: "Transport" },
  { value: 3, label: "Utilities" },
  { value: 4, label: "Entertainment" },
  { value: 5, label: "Health" },
  { value: 6, label: "Other" },
];

export function CreateExpenseForm() {
  const mutation = useCreateExpense();
  const pushToast = useToastStore((s) => s.pushToast);

  const today = new Date().toISOString().slice(0, 10);

  const form = useForm<CreateExpenseInput, unknown, CreateExpenseData>({
    resolver: zodResolver(createExpenseSchema),
    defaultValues: {
      amount: 1,
      currency: "BGN",
      expenseDate: today,
      category: 1,
      description: "",
      sourceType: 1,
      sourceReferenceId: null,
    },
  });

  const onSubmit = async (values: CreateExpenseData) => {
    try {
      await mutation.mutateAsync(values);
      form.reset({
        amount: 1,
        currency: "BGN",
        expenseDate: today,
        category: 1,
        description: "",
        sourceType: 1,
        sourceReferenceId: null,
      });
      pushToast("success", "Expense created.");
    } catch (error) {
      pushToast("error", getErrorMessage(error, "Failed to create expense."));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-xl border bg-white p-6">
      <div>
        <label className="text-sm font-medium">Amount</label>
        <input
          type="number"
          step="0.01"
          {...form.register("amount")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.amount ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.amount.message}</p>
        ) : null}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <label className="text-sm font-medium">Currency</label>
          <input
            {...form.register("currency")}
            className="mt-1 w-full rounded-lg border px-3 py-2"
          />
          {form.formState.errors.currency ? (
            <p className="mt-1 text-sm text-red-600">{form.formState.errors.currency.message}</p>
          ) : null}
        </div>

        <div>
          <label className="text-sm font-medium">Expense date</label>
          <input
            type="date"
            {...form.register("expenseDate")}
            className="mt-1 w-full rounded-lg border px-3 py-2"
          />
          {form.formState.errors.expenseDate ? (
            <p className="mt-1 text-sm text-red-600">{form.formState.errors.expenseDate.message}</p>
          ) : null}
        </div>
      </div>

      <div>
        <label className="text-sm font-medium">Category</label>
        <select
          {...form.register("category")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        >
          {categoryOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        {form.formState.errors.category ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.category.message}</p>
        ) : null}
      </div>

      <div>
        <label className="text-sm font-medium">Description</label>
        <textarea
          rows={3}
          {...form.register("description")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.description ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.description.message}</p>
        ) : null}
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">Failed to create expense.</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText="Creating..."
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        Create expense
      </LoadingButton>
    </form>
  );
}