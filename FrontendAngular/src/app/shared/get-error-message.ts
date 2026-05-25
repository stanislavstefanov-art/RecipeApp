import { HttpErrorResponse } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';

interface ProblemDetailsBody {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  extensions?: Record<string, unknown>;
}

export function getErrorMessage(
  error: unknown,
  translate?: TranslateService,
  fallback = 'Something went wrong.',
): string {
  if (error instanceof HttpErrorResponse) {
    const data = error.error as ProblemDetailsBody | string | null | undefined;

    if (data && typeof data === 'object') {
      if (translate) {
        const code =
          (data.extensions?.['code'] as string | undefined) ?? data.title;
        if (code) {
          const key = `errors.${code}`;
          const translated = translate.instant(key);
          if (translated && translated !== key) {
            return translated;
          }
        }
      }

      // Field-level validation errors are more specific than the generic title.
      if (data.errors) {
        const messages = Object.values(data.errors).flat();
        if (messages.length > 0) return messages.join(' ');
      }

      if (data.detail) return data.detail;
      if (data.title) return data.title;
    }

    if (typeof data === 'string' && data) return data;

    return error.message || fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}
