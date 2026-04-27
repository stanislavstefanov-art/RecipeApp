import axios from "axios";
import type { TFunction } from "i18next";

export function getErrorMessage(
  error: unknown,
  t?: TFunction,
  fallback = "Something went wrong.",
) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | {
          title?: string;
          detail?: string;
          errors?: Record<string, string[]>;
          extensions?: Record<string, unknown>;
        }
      | undefined;

    if (t) {
      const code =
        (data?.extensions?.["code"] as string | undefined) ?? data?.title;
      if (code) {
        const translated = t(`errors.${code}`, { defaultValue: "" });
        if (translated) return translated;
      }
    }

    if (data?.detail) {
      return data.detail;
    }

    if (data?.title) {
      return data.title;
    }

    const firstValidationMessage = data?.errors
      ? Object.values(data.errors).flat()[0]
      : undefined;

    if (firstValidationMessage) {
      return firstValidationMessage;
    }

    return error.message || fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}
