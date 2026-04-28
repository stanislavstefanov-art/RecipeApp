import { useTranslation } from "react-i18next";
import { useEntraLogin } from "../hooks/useEntraLogin";
import { getErrorMessage } from "../../../lib/getErrorMessage";
import { useNavigate } from "react-router-dom";

export function EntraLoginButton() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const mutation = useEntraLogin();

  const handleClick = async () => {
    try {
      await mutation.mutateAsync();
      navigate("/", { replace: true });
    } catch {
      // error shown below
    }
  };

  return (
    <div className="space-y-2">
      <button
        type="button"
        onClick={handleClick}
        disabled={mutation.isPending}
        className="flex w-full items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
      >
        <MicrosoftIcon />
        {mutation.isPending ? t("common.loading") : t("auth.signInWithMicrosoft")}
      </button>
      {mutation.error && (
        <p className="text-xs text-red-600">{getErrorMessage(mutation.error, t)}</p>
      )}
    </div>
  );
}

function MicrosoftIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 21 21" aria-hidden="true">
      <rect x="1" y="1" width="9" height="9" fill="#F25022" />
      <rect x="11" y="1" width="9" height="9" fill="#7FBA00" />
      <rect x="1" y="11" width="9" height="9" fill="#00A4EF" />
      <rect x="11" y="11" width="9" height="9" fill="#FFB900" />
    </svg>
  );
}
