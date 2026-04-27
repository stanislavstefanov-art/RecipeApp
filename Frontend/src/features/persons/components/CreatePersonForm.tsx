import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import {
  createPersonSchema,
  type CreatePersonInput,
  type CreatePersonData,
} from "../schemas";
import { useCreatePerson } from "../hooks/useCreatePerson";
import { LoadingButton } from "../../../components/ui/LoadingButton";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useToastStore } from "../../../stores/toastStore";

const DIETARY_VALUES = [1, 2, 3, 4];
const HEALTH_VALUES = [1, 2, 3];

export function CreatePersonForm() {
  const { t } = useTranslation();
  const mutation = useCreatePerson();
  const pushToast = useToastStore((s) => s.pushToast);

  const schema = useMemo(() => createPersonSchema(t), [t]);
  const form = useForm<CreatePersonInput, unknown, CreatePersonData>({
    resolver: zodResolver(schema),
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
      pushToast("success", t('persons.createPerson'));
    } catch (error) {
      pushToast("error", getErrorMessage(error, t));
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-xl border bg-white p-6">
      <div>
        <label className="text-sm font-medium">{t('persons.name')}</label>
        <input
          {...form.register("name")}
          placeholder={t('persons.namePlaceholder')}
          className="mt-1 w-full rounded-lg border px-3 py-2"
        />
        {form.formState.errors.name ? (
          <p className="mt-1 text-sm text-red-600">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      <div>
        <label className="text-sm font-medium">{t('persons.dietaryPreferences')}</label>
        <div className="mt-2 grid gap-2">
          {DIETARY_VALUES.map((value) => (
            <label key={value} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                value={value}
                {...form.register("dietaryPreferences")}
              />
              {t('enums.dietaryPreference.' + value)}
            </label>
          ))}
        </div>
      </div>

      <div>
        <label className="text-sm font-medium">{t('persons.healthConcerns')}</label>
        <div className="mt-2 grid gap-2">
          {HEALTH_VALUES.map((value) => (
            <label key={value} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                value={value}
                {...form.register("healthConcerns")}
              />
              {t('enums.healthConcern.' + value)}
            </label>
          ))}
        </div>
      </div>

      <div>
        <label className="text-sm font-medium">{t('persons.notes')}</label>
        <textarea {...form.register("notes")} rows={3} className="mt-1 w-full rounded-lg border px-3 py-2" />
      </div>

      {mutation.isError ? (
        <p className="text-sm text-red-600">{t('common.error')}</p>
      ) : null}

      <LoadingButton
        type="submit"
        isLoading={mutation.isPending}
        loadingText={t('common.create') + '…'}
        className="rounded-lg bg-slate-900 px-4 py-2 text-sm text-white"
      >
        {t('persons.createPerson')}
      </LoadingButton>
    </form>
  );
}
