import { useToastStore } from "../../stores/toastStore";

function toastClass(kind: "success" | "error" | "info") {
  switch (kind) {
    case "success":
      return "border-green-200 bg-green-50 text-green-800";
    case "error":
      return "border-red-200 bg-red-50 text-red-800";
    default:
      return "border-slate-200 bg-white text-slate-800";
  }
}

export function ToastViewport() {
  const { toasts, removeToast } = useToastStore();

  if (toasts.length === 0) {
    return null;
  }

  return (
    <div className="pointer-events-none fixed inset-x-4 top-4 z-[100] flex flex-col gap-3 sm:left-auto sm:right-4 sm:top-4 sm:w-full sm:max-w-sm">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`pointer-events-auto rounded-xl border p-4 shadow ${toastClass(toast.kind)}`}
        >
          <div className="flex items-start justify-between gap-3">
            <p className="text-sm">{toast.message}</p>

            <button
              type="button"
              onClick={() => removeToast(toast.id)}
              className="shrink-0 text-xs opacity-70 hover:opacity-100"
            >
              Close
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}