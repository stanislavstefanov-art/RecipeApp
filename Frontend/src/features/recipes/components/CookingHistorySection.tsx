import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useCookingHistory } from "../hooks/useCookingHistory";
import { useLogCooking } from "../hooks/useLogCooking";
import { useDeleteCookingEntry } from "../hooks/useDeleteCookingEntry";

interface CookingHistorySectionProps {
  recipeId: string;
}

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

export function CookingHistorySection({ recipeId }: CookingHistorySectionProps) {
  const { t } = useTranslation();
  const { data: entries = [] } = useCookingHistory(recipeId);
  const logMutation = useLogCooking(recipeId);
  const deleteMutation = useDeleteCookingEntry(recipeId);

  const [cookedOn, setCookedOn] = useState(todayIso);
  const [servings, setServings] = useState(1);
  const [notes, setNotes] = useState("");

  function handleLog(e: React.FormEvent) {
    e.preventDefault();
    logMutation.mutate(
      { cookedOn, servings, notes: notes.trim() || null },
      {
        onSuccess: () => {
          setCookedOn(todayIso());
          setServings(1);
          setNotes("");
        },
      },
    );
  }

  function handleDelete(id: string) {
    if (!window.confirm(t("cookingLog.confirmDelete"))) return;
    deleteMutation.mutate(id);
  }

  return (
    <section className="rounded-xl border bg-white p-5 sm:p-6">
      <h3 className="text-base font-medium sm:text-lg">{t("cookingLog.title")}</h3>

      <form onSubmit={handleLog} className="mt-4 space-y-3 border-t pt-4">
        <div className="flex flex-wrap gap-3">
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-slate-700">
              {t("cookingLog.cookedOn")}
            </label>
            <input
              type="date"
              value={cookedOn}
              max={todayIso()}
              onChange={(e) => setCookedOn(e.target.value)}
              required
              className="rounded-lg border px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-slate-300"
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-slate-700">
              {t("cookingLog.servings")}
            </label>
            <input
              type="number"
              value={servings}
              min={1}
              max={100}
              onChange={(e) => setServings(Number(e.target.value))}
              required
              className="w-20 rounded-lg border px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-slate-300"
            />
          </div>
        </div>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder={t("cookingLog.notesPlaceholder")}
          maxLength={500}
          rows={2}
          className="w-full rounded-lg border px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-slate-300"
        />
        <button
          type="submit"
          disabled={logMutation.isPending}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          {logMutation.isPending ? t("cookingLog.logging") : t("cookingLog.logCooking")}
        </button>
      </form>

      {entries.length === 0 ? (
        <p className="mt-4 text-sm text-slate-400">{t("cookingLog.noHistory")}</p>
      ) : (
        <ul className="mt-4 space-y-2">
          {entries.map((entry) => (
            <li
              key={entry.id}
              className="flex items-start justify-between gap-3 border-t pt-3"
            >
              <div className="space-y-0.5">
                <p className="text-sm font-medium">
                  {entry.cookedOn} &middot; {t("cookingLog.servingsLabel", { count: entry.servings })}
                </p>
                {entry.notes && (
                  <p className="text-sm text-slate-500">{entry.notes}</p>
                )}
              </div>
              <button
                type="button"
                onClick={() => handleDelete(entry.id)}
                disabled={deleteMutation.isPending}
                className="shrink-0 text-xs text-red-500 hover:text-red-700 disabled:opacity-50"
              >
                {t("cookingLog.deleteEntry")}
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
