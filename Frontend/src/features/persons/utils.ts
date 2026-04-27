import i18n from '../../i18n';

export function getDietaryPreferenceLabel(value: number) {
  const key = `enums.dietaryPreference.${value}`;
  const translated = i18n.t(key);
  return translated !== key ? translated : `Preference ${value}`;
}

export function getHealthConcernLabel(value: number) {
  const key = `enums.healthConcern.${value}`;
  const translated = i18n.t(key);
  return translated !== key ? translated : `Concern ${value}`;
}
