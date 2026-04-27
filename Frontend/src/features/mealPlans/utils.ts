import i18n from '../../i18n';
import { getCurrentIntlLocale } from '../../i18n';

export function getMealTypeLabel(value: number) {
  const key = `enums.mealType.${value}`;
  const translated = i18n.t(key);
  return translated !== key ? translated : `Meal type ${value}`;
}

export function getMealScopeLabel(value: number) {
  const key = `enums.mealScope.${value}`;
  const translated = i18n.t(key);
  return translated !== key ? translated : `Scope ${value}`;
}

export function formatPlannedDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(getCurrentIntlLocale(), {
    weekday: "long",
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}
