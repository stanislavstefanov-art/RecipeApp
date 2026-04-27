import { useTranslation } from 'react-i18next';

export function LanguageSwitcher() {
  const { i18n } = useTranslation();
  const active = i18n.language === 'en' ? 'en' : 'bg';

  const base = 'rounded-full px-3 py-1 text-xs font-medium transition-colors';
  const on = `${base} bg-blue-600 text-white`;
  const off = `${base} text-slate-500 hover:text-slate-900`;

  return (
    <div className="flex gap-1">
      <button type="button" className={active === 'bg' ? on : off} onClick={() => i18n.changeLanguage('bg')}>
        БГ
      </button>
      <button type="button" className={active === 'en' ? on : off} onClick={() => i18n.changeLanguage('en')}>
        EN
      </button>
    </div>
  );
}
