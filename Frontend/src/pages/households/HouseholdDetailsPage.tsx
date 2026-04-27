import { Link, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { AddHouseholdMemberForm } from "../../features/households/components/AddHouseholdMemberForm";
import { useHousehold } from "../../features/households/hooks/useHousehold";

export function HouseholdDetailsPage() {
  const { t } = useTranslation();
  const { householdId = "" } = useParams();
  const { data, isLoading, isError, error } = useHousehold(householdId);

  if (isLoading) return <LoadingState title={t('households.title')} />;

  if (isError) {
    return (
      <ErrorState
        title={t('households.title')}
        message={error instanceof Error ? error.message : undefined}
      />
    );
  }

  if (!data) return <EmptyState title={t('households.noHouseholds')} />;

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <Link to="/households" className="text-sm text-slate-500">
          ← {t('common.back')}
        </Link>

        <div className="rounded-xl border bg-white p-6">
          <h2 className="text-2xl font-semibold">{data.name}</h2>
          <p className="mt-1 text-sm text-slate-500">
            {t('households.planningContext')}
          </p>
        </div>

        <section className="rounded-xl border bg-white p-6">
          <h3 className="text-lg font-medium">{t('households.members')}</h3>

          {data.members.length === 0 ? (
            <p className="mt-4 text-sm text-slate-500">{t('households.noMembers')}</p>
          ) : (
            <div className="mt-4 grid gap-4">
              {data.members.map((member) => (
                <div key={member.personId} className="rounded-lg border p-4">
                    <h4 className="font-medium">{member.personName}</h4>

                    <div className="mt-3 grid gap-4 md:grid-cols-2">
                        <div>
                        <p className="text-sm font-medium text-slate-700">{t('persons.dietaryPreferences')}</p>
                        {member.dietaryPreferences.length === 0 ? (
                            <p className="mt-1 text-sm text-slate-500">{t('persons.none')}</p>
                        ) : (
                            <ul className="mt-1 list-disc pl-5 text-sm text-slate-700">
                            {member.dietaryPreferences.map((value, index) => (
                                <li key={`${value}-${index}`}>{t('enums.dietaryPreference.' + value)}</li>
                            ))}
                            </ul>
                        )}
                        </div>

                        <div>
                        <p className="text-sm font-medium text-slate-700">{t('persons.healthConcerns')}</p>
                        {member.healthConcerns.length === 0 ? (
                            <p className="mt-1 text-sm text-slate-500">{t('persons.none')}</p>
                        ) : (
                            <ul className="mt-1 list-disc pl-5 text-sm text-slate-700">
                            {member.healthConcerns.map((value, index) => (
                                <li key={`${value}-${index}`}>{t('enums.healthConcern.' + value)}</li>
                            ))}
                            </ul>
                        )}
                        </div>
                    </div>

                    <p className="mt-3 text-sm text-slate-700">
                        {member.notes || t('persons.noNotes')}
                    </p>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      <div>
        <AddHouseholdMemberForm
          householdId={data.id}
          existingPersonIds={data.members.map((m) => m.personId)}
        />
      </div>
    </div>
  );
}
