import { useTranslation } from "react-i18next";
import { RegisterForm } from "../../features/auth/components/RegisterForm";
import { LanguageSwitcher } from "../../components/ui/LanguageSwitcher";

export function RegisterPage() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">RecipeApp</h1>
            <p className="mt-1 text-sm text-slate-500">{t("auth.welcome")}</p>
          </div>
          <LanguageSwitcher />
        </div>

        <div className="rounded-xl border bg-white p-6 shadow-sm">
          <RegisterForm />
        </div>
      </div>
    </div>
  );
}
