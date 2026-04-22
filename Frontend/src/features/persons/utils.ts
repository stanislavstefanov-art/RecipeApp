export function getDietaryPreferenceLabel(value: number) {
  switch (value) {
    case 1:
      return "Vegetarian";
    case 2:
      return "Pescatarian";
    case 3:
      return "Vegan";
    case 4:
      return "High protein";
    default:
      return `Preference ${value}`;
  }
}

export function getHealthConcernLabel(value: number) {
  switch (value) {
    case 1:
      return "Diabetes";
    case 2:
      return "High blood pressure";
    case 3:
      return "Gluten intolerance";
    default:
      return `Concern ${value}`;
  }
}