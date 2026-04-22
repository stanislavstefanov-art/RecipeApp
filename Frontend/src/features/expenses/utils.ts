export function getExpenseCategoryLabel(value: number) {
  switch (value) {
    case 1:
      return "Food";
    case 2:
      return "Transport";
    case 3:
      return "Utilities";
    case 4:
      return "Entertainment";
    case 5:
      return "Health";
    case 6:
      return "Other";
    default:
      return `Category ${value}`;
  }
}

export function getExpenseSourceTypeLabel(value: number) {
  switch (value) {
    case 1:
      return "Manual";
    case 2:
      return "Shopping list item";
    case 3:
      return "Meal plan";
    default:
      return `Source ${value}`;
  }
}

export function formatCurrency(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat("en-GB", {
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

  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}