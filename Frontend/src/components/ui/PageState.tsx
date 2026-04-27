import { useTranslation } from 'react-i18next';

type PageStateProps = {
  title: string;
  message?: string;
};

export function LoadingState({ title, message }: PageStateProps) {
  const { t } = useTranslation();
  return (
    <div className="rounded-xl border bg-white p-5 sm:p-6">
      <h3 className="font-medium">{title}</h3>
      <p className="mt-2 text-sm text-slate-500">{message ?? t('common.loading')}</p>
    </div>
  );
}

export function ErrorState({ title, message }: PageStateProps) {
  const { t } = useTranslation();
  return (
    <div className="rounded-xl border border-red-200 bg-red-50 p-5 sm:p-6">
      <h3 className="font-medium text-red-700">{title}</h3>
      <p className="mt-2 text-sm text-red-600">{message ?? t('common.error')}</p>
    </div>
  );
}

export function EmptyState({ title, message }: PageStateProps) {
  const { t } = useTranslation();
  return (
    <div className="rounded-xl border bg-white p-5 sm:p-6">
      <h3 className="font-medium">{title}</h3>
      <p className="mt-2 text-sm text-slate-500">{message ?? t('common.noData')}</p>
    </div>
  );
}
