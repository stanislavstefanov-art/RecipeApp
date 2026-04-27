import i18n from '../../i18n';
import { getCurrentIntlLocale } from '../../i18n';

export function getExpenseCategoryLabel(value: number) {
  const key = `enums.expenseCategory.${value}`;
  const translated = i18n.t(key);
  return translated !== key ? translated : `Category ${value}`;
}

export function getExpenseSourceTypeLabel(value: number) {
  const key = `enums.expenseSourceType.${value}`;
  const translated = i18n.t(key);
  return translated !== key ? translated : `Source ${value}`;
}

export function formatCurrency(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(getCurrentIntlLocale(), {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    return `${amount} ${currency}`;
  }
}

export function formatDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(getCurrentIntlLocale(), {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}
