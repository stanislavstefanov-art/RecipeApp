import { Link, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { usePerson } from "../../features/persons/hooks/usePerson";
import {
  getDietaryPreferenceLabel,
  getHealthConcernLabel,
} from "../../features/persons/utils";

export function PersonDetailsPage() {
  const { t } = useTranslation();
  const { personId = "" } = useParams();
  const { data, isLoading, isError, error } = usePerson(personId);

  if (isLoading) return <LoadingState title={t('persons.title')} />;

  if (isError) {
    return (
      <ErrorState
        title={t('persons.title')}
        message={error instanceof Error ? error.message : undefined}
      />
    );
  }

  if (!data) return <EmptyState title={t('persons.noPersons')} />;

  return (
    <div className="space-y-6">
      <Link to="/persons" className="text-sm text-slate-500">
        ← {t('common.back')}
      </Link>

      <div className="rounded-xl border bg-white p-6">
        <h2 className="text-2xl font-semibold">{data.name}</h2>

        <div className="mt-6 grid gap-6 md:grid-cols-2">
          <div>
            <h3 className="font-medium">{t('persons.dietaryPreferences')}</h3>
            {data.dietaryPreferences.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">{t('persons.none')}</p>
            ) : (
              <ul className="mt-2 list-disc pl-5 text-sm text-slate-700">
                {data.dietaryPreferences.map((item, index) => (
                <li key={`${item}-${index}`}>{getDietaryPreferenceLabel(item)}</li>
                ))}
              </ul>
            )}
          </div>

          <div>
            <h3 className="font-medium">{t('persons.healthConcerns')}</h3>
            {data.healthConcerns.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">{t('persons.none')}</p>
            ) : (
              <ul className="mt-2 list-disc pl-5 text-sm text-slate-700">
                {data.healthConcerns.map((item, index) => (
                <li key={`${item}-${index}`}>{getHealthConcernLabel(item)}</li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div className="mt-6">
          <h3 className="font-medium">{t('persons.notes')}</h3>
          <p className="mt-2 text-sm text-slate-700">{data.notes || t('persons.noNotes')}</p>
        </div>
      </div>
    </div>
  );
}
