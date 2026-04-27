import { useTranslation } from "react-i18next";
import { SuggestMealPlanForm } from "../../features/mealPlans/components/SuggestMealPlanForm";

export function SuggestMealPlanPage() {
  const { t } = useTranslation();
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold">{t('mealPlans.suggest')}</h2>
        <p className="text-sm text-slate-500">
          {t('mealPlans.suggestPlanDesc')}
        </p>
      </div>

      <SuggestMealPlanForm />
    </div>
  );
}
