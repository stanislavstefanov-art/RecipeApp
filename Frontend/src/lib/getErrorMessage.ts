import axios from "axios";

export function getErrorMessage(error: unknown, fallback = "Something went wrong.") {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | { title?: string; detail?: string; errors?: Record<string, string[]> }
      | undefined;

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