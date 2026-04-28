interface StarRatingProps {
  value: number | null;
  onChange?: (stars: number) => void;
  size?: "sm" | "md";
}

export function StarRating({ value, onChange, size = "md" }: StarRatingProps) {
  const isReadOnly = !onChange;
  const sizeClass = size === "sm" ? "text-sm" : "text-xl";

  return (
    <span className={`inline-flex gap-0.5 ${sizeClass}`} aria-label={value != null ? `${value} out of 5 stars` : "Not rated"}>
      {[1, 2, 3, 4, 5].map((n) => {
        const filled = value != null && n <= Math.round(value);
        if (isReadOnly) {
          return (
            <span key={n} className={filled ? "text-amber-400" : "text-slate-300"}>
              ★
            </span>
          );
        }
        return (
          <button
            key={n}
            type="button"
            aria-label={`Rate ${n} star${n !== 1 ? "s" : ""}`}
            onClick={() => onChange(n)}
            className={`leading-none ${filled ? "text-amber-400" : "text-slate-300 hover:text-amber-300"} transition-colors`}
          >
            ★
          </button>
        );
      })}
    </span>
  );
}
