import { useTranslation } from "react-i18next";
import { LoginForm } from "../../features/auth/components/LoginForm";
import { EntraLoginButton } from "../../features/auth/components/EntraLoginButton";
import { LanguageSwitcher } from "../../components/ui/LanguageSwitcher";

export function LoginPage() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">RecipeApp</h1>
            <p className="mt-1 text-sm text-slate-500">{t("auth.welcomeBack")}</p>
          </div>
          <LanguageSwitcher />
        </div>

        <div className="rounded-xl border bg-white p-6 shadow-sm">
          <LoginForm />

          {import.meta.env.VITE_ENTRA_ENABLED === "true" && (
            <>
              <div className="my-4 flex items-center gap-3">
                <hr className="flex-1 border-slate-200" />
                <span className="text-xs text-slate-400">OR</span>
                <hr className="flex-1 border-slate-200" />
              </div>
              <EntraLoginButton />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
