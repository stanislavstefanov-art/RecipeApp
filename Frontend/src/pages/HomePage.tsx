import { useTranslation } from 'react-i18next';

export function HomePage() {
  const { t } = useTranslation();
  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-semibold">{t('nav.home')}</h2>
      <p className="text-slate-600">
        {t('recipes.descriptionBrowse')}
      </p>
    </div>
  );
}
