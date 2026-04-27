import { getCurrentIntlLocale } from '../../i18n';

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
