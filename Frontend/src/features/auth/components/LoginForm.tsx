import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Link, useNavigate } from "react-router-dom";
import { createLoginSchema, type LoginInput } from "../schemas";
import { useLogin } from "../hooks/useLogin";
import { getErrorMessage } from "../../../lib/getErrorMessage";

export function LoginForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const mutation = useLogin();

  const schema = useMemo(() => createLoginSchema(t), [t]);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginInput>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (values: LoginInput) => {
    try {
      await mutation.mutateAsync(values);
      navigate("/", { replace: true });
    } catch {
      // error shown below via mutation.error
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-slate-700">{t("auth.email")}</label>
        <input
          type="email"
          autoComplete="email"
          {...register("email")}
          className="mt-1 w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
      </div>

      <div>
        <label className="block text-sm font-medium text-slate-700">{t("auth.password")}</label>
        <input
          type="password"
          autoComplete="current-password"
          {...register("password")}
          className="mt-1 w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        {errors.password && <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>}
      </div>

      {mutation.error && (
        <p className="text-sm text-red-600">{getErrorMessage(mutation.error, t)}</p>
      )}

      <button
        type="submit"
        disabled={isSubmitting || mutation.isPending}
        className="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
      >
        {mutation.isPending ? t("common.loading") : t("auth.signIn")}
      </button>

      <p className="text-center text-sm text-slate-600">
        {t("auth.noAccount")}{" "}
        <Link to="/register" className="font-medium text-blue-600 hover:underline">
          {t("auth.signUp")}
        </Link>
      </p>
    </form>
  );
}
