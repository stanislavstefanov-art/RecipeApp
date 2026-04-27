import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreateHouseholdForm } from "../../features/households/components/CreateHouseholdForm";
import { useHouseholds } from "../../features/households/hooks/useHouseholds";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function HouseholdsPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = useHouseholds();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title={t('households.title')}
          description={t('households.planningContext')}
        />

        {isLoading ? (
          <LoadingState title={t('households.title')} />
        ) : isError ? (
          <ErrorState
            title={t('households.title')}
            message={error instanceof Error ? error.message : undefined}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title={t('households.noHouseholds')} />
        ) : (
          <div className="grid gap-4">
            {data.map((household) => (
              <Link
                key={household.id}
                to={`/households/${household.id}`}
                className="rounded-xl border bg-white p-5 hover:shadow-sm"
              >
                <h3 className="font-medium">{household.name}</h3>
                <p className="mt-1 text-sm text-slate-500">
                  {t('households.memberCountLabel', { count: household.memberCount })}
                </p>
              </Link>
            ))}
          </div>
        )}
      </div>

      <div>
        <CreateHouseholdForm />
      </div>
    </div>
  );
}
