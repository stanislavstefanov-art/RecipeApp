type PageStateProps = {
  title: string;
  message?: string;
};

export function LoadingState({ title, message }: PageStateProps) {
  return (
    <div className="rounded-xl border bg-white p-5 sm:p-6">
      <h3 className="font-medium">{title}</h3>
      <p className="mt-2 text-sm text-slate-500">{message ?? "Loading..."}</p>
    </div>
  );
}

export function ErrorState({ title, message }: PageStateProps) {
  return (
    <div className="rounded-xl border border-red-200 bg-red-50 p-5 sm:p-6">
      <h3 className="font-medium text-red-700">{title}</h3>
      <p className="mt-2 text-sm text-red-600">{message ?? "Something went wrong."}</p>
    </div>
  );
}

export function EmptyState({ title, message }: PageStateProps) {
  return (
    <div className="rounded-xl border bg-white p-5 sm:p-6">
      <h3 className="font-medium">{title}</h3>
      <p className="mt-2 text-sm text-slate-500">{message ?? "No items found."}</p>
    </div>
  );
}