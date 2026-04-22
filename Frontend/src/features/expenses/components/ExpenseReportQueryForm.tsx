import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  monthlyExpenseQuerySchema,
  type MonthlyExpenseQueryInput,
  type MonthlyExpenseQueryData,
} from "../schemas";
import { LoadingButton } from "../../../components/ui/LoadingButton";

type Props = {
  initialValues: MonthlyExpenseQueryInput;
  onSubmit: (values: MonthlyExpenseQueryData) => void;
};

export function ExpenseReportQueryForm({ initialValues, onSubmit }: Props) {
  const form = useForm<MonthlyExpenseQueryInput, unknown, MonthlyExpenseQueryData>({
    resolver: zodResolver(monthlyExpenseQuerySchema),
    values: initialValues,
  });

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="grid gap-4 rounded-xl border bg-white p-6 md:grid-cols-[1fr_1fr_auto]">
      <div>
        <label className="text-sm font-medium">Year</label>
        <input
          type="number"
          {...form.register("year")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.year ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.year.message}</p>
        ) : null}
      </div>

      <div>
        <label className="text-sm font-medium">Month</label>
        <input
          type="number"
          {...form.register("month")}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.month ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.month.message}</p>
        ) : null}
      </div>

      <div className="flex items-end">
        <LoadingButton
          type="submit"
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
        >
          Load report
        </LoadingButton>
      </div>
    </form>
  );
}