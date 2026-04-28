import { useState } from "react";
import { useTranslation } from "react-i18next";
import { StarRating } from "../../../components/ui/StarRating";
import { useRateRecipe } from "../hooks/useRateRecipe";
import { useDeleteRecipeRating } from "../hooks/useDeleteRecipeRating";
import type { RecipeRating } from "../schemas";

interface RatingSectionProps {
  recipeId: string;
  ratings: RecipeRating[];
  myRating: RecipeRating | null;
  averageStars: number | null;
  ratingCount: number;
}

export function RatingSection({ recipeId, ratings, myRating, averageStars, ratingCount }: RatingSectionProps) {
  const { t } = useTranslation();
  const [selectedStars, setSelectedStars] = useState<number | null>(myRating?.stars ?? null);
  const [comment, setComment] = useState(myRating?.comment ?? "");
  const rateMutation = useRateRecipe(recipeId);
  const deleteMutation = useDeleteRecipeRating(recipeId);

  function handleSave() {
    if (!selectedStars) return;
    rateMutation.mutate({ stars: selectedStars, comment: comment.trim() || null });
  }

  function handleDelete() {
    if (!window.confirm(t("ratings.confirmDeleteRating"))) return;
    deleteMutation.mutate();
  }

  return (
    <section className="rounded-xl border bg-white p-5 sm:p-6">
      <div className="flex items-center justify-between">
        <h3 className="text-base font-medium sm:text-lg">{t("ratings.title")}</h3>
        {averageStars != null && (
          <span className="text-sm text-slate-500">
            <StarRating value={averageStars} size="sm" />
            {" "}
            {averageStars.toFixed(1)} ({ratingCount})
          </span>
        )}
      </div>

      <div className="mt-4 space-y-3 border-t pt-4">
        <p className="text-sm font-medium text-slate-700">{t("ratings.rateThis")}</p>
        <StarRating value={selectedStars} onChange={setSelectedStars} />
        <textarea
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder={t("ratings.commentPlaceholder")}
          maxLength={500}
          rows={2}
          className="w-full rounded-lg border px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-slate-300"
        />
        <div className="flex gap-2">
          <button
            type="button"
            onClick={handleSave}
            disabled={!selectedStars || rateMutation.isPending}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            {rateMutation.isPending ? t("ratings.saving") : t("ratings.saveRating")}
          </button>
          {myRating && (
            <button
              type="button"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="rounded-lg border px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
            >
              {deleteMutation.isPending ? t("ratings.deleting") : t("ratings.deleteRating")}
            </button>
          )}
        </div>
      </div>

      {ratings.length === 0 ? (
        <p className="mt-4 text-sm text-slate-400">{t("ratings.noRatings")}</p>
      ) : (
        <ul className="mt-4 space-y-3">
          {ratings.map((r) => (
            <li key={r.id} className="border-t pt-3">
              <div className="flex items-center gap-2">
                <StarRating value={r.stars} size="sm" />
                <span className="text-xs text-slate-400">
                  {new Date(r.createdAt).toLocaleDateString()}
                </span>
              </div>
              {r.comment && <p className="mt-1 text-sm text-slate-600">{r.comment}</p>}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
