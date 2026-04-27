import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import {
  createExpenseSchema,
  type CreateExpenseInput,
  type CreateExpenseData,
} from "../schemas";
import { useCreateExpense } from "../hooks/useCreateExpense";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

const CATEGORY_VALUES = [1, 2, 3, 4, 5, 6];

export function CreateExpenseForm() {
  const { t } = useTranslation();
  const mutation = useCreateExpense();
  const pushToast = useToastStore((s) => s.pushToast);

  const today = new Date().toISOString().slice(0, 10);

  const schema = useMemo(() => createExpenseSchema(t), [t]);
  const form = useForm<CreateExpenseInput, unknown, CreateExpenseData>({
    resolver: zodResolver(schema),
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
      pushToast("success", t('expenses.createExpense'));
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-xl border bg-white p-6">
      <div>
        <label className="text-sm font-medium">{t('expenses.amount')}</label>
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
          <label className="text-sm font-medium">{t('expenses.currency')}</label>
          <input
            {...form.register("currency")}
            className="mt-1 w-full rounded-lg border px-3 py-2"
          />
          {form.formState.errors.currency ? (
            <p className="mt-1 text-sm text-red-600">{form.formState.errors.currency.message}</p>
          ) : null}
        </div>

        <div>
          <label className="text-sm font-medium">{t('expenses.expenseDate')}</label>
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
        <label className="text-sm font-medium">{t('expenses.category')}</label>
        <select
          {...form.register("category")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        >
          {CATEGORY_VALUES.map((value) => (
            <option key={value} value={value}>
              {t('enums.expenseCategory.' + value)}
            </option>
          ))}
        </select>
        {form.formState.errors.category ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.category.message}</p>
        ) : null}
      </div>

      <div>
        <label className="text-sm font-medium">{t('expenses.description')}</label>
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
        <p className="text-sm text-red-600">{t('common.error')}</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText={t('expenses.creating')}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        {t('expenses.createExpense')}
      </LoadingButton>
    </form>
  );
}
