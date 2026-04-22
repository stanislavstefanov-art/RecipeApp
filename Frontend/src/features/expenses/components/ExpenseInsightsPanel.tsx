import type { ExpenseInsight } from "../schemas";

type Props = {
  insight: ExpenseInsight;
};

export function ExpenseInsightsPanel({ insight }: Props) {
  return (
    <div className="rounded-xl border bg-white p-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-medium">Insights</h3>
          <p className="mt-1 text-sm text-slate-500">
            Confidence: {insight.confidence} · Needs review: {insight.needsReview ? "Yes" : "No"}
          </p>
        </div>
      </div>

      <p className="mt-4 text-sm text-slate-700">{insight.summary}</p>

      <div className="mt-6 grid gap-6 md:grid-cols-2">
        <div>
          <h4 className="font-medium">Key findings</h4>
          <ul className="mt-3 list-disc pl-5 text-sm text-slate-700">
            {insight.keyFindings.map((item, index) => (
              <li key={`${item}-${index}`}>{item}</li>
            ))}
          </ul>
        </div>

        <div>
          <h4 className="font-medium">Recommendations</h4>
          <ul className="mt-3 list-disc pl-5 text-sm text-slate-700">
            {insight.recommendations.map((item, index) => (
              <li key={`${item}-${index}`}>{item}</li>
            ))}
          </ul>
        </div>
      </div>

      {insight.notes ? (
        <p className="mt-6 text-sm text-slate-600">{insight.notes}</p>
      ) : null}
    </div>
  );
}