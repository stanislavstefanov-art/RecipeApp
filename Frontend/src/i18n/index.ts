import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import bg from '../locales/bg.json';
import en from '../locales/en.json';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: { bg: { translation: bg }, en: { translation: en } },
    fallbackLng: 'bg',
    lng: 'bg',
    detection: { order: ['localStorage', 'htmlTag'], caches: ['localStorage'] },
    interpolation: { escapeValue: false },
  });

export function getCurrentIntlLocale(): string {
  return i18n.language === 'en' ? 'en-GB' : 'bg-BG';
}

export default i18n;
