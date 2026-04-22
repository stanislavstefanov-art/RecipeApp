export function getMealTypeLabel(value: number) {
  switch (value) {
    case 1:
      return "Breakfast";
    case 2:
      return "Lunch";
    case 3:
      return "Dinner";
    default:
      return `Meal type ${value}`;
  }
}

export function getMealScopeLabel(value: number) {
  switch (value) {
    case 1:
      return "Shared";
    case 2:
      return "Shared with variations";
    case 3:
      return "Individual";
    default:
      return `Scope ${value}`;
  }
}

export function formatPlannedDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-GB", {
    weekday: "long",
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}