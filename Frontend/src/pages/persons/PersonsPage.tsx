import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreatePersonForm } from "../../features/persons/components/CreatePersonForm";
import { usePersons } from "../../features/persons/hooks/usePersons";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function PersonsPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = usePersons();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title={t('persons.title')}
          description={t('persons.dietaryPreferencesPlaceholder')}
        />

        {isLoading ? (
          <LoadingState title={t('persons.title')} />
        ) : isError ? (
          <ErrorState
            title={t('persons.title')}
            message={error instanceof Error ? error.message : undefined}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title={t('persons.noPersons')} />
        ) : (
          <div className="grid gap-4">
            {data.map((person) => (
              <Link
                key={person.id}
                to={`/persons/${person.id}`}
                className="rounded-xl border bg-white p-5 hover:shadow-sm"
              >
                <h3 className="font-medium">{person.name}</h3>
                <p className="mt-1 text-sm text-slate-500">
                  {t('persons.dietaryHealthCount', {
                    dietary: person.dietaryPreferences.length,
                    health: person.healthConcerns.length,
                  })}
                </p>
              </Link>
            ))}
          </div>
        )}
      </div>

      <div>
        <CreatePersonForm />
      </div>
    </div>
  );
}
