import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function extractApiError(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    const problem = err.error as ProblemDetails | null;
    if (problem?.errors) {
      return Object.values(problem.errors).flat().join(' ');
    }
    return problem?.detail ?? problem?.title ?? err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return 'An unexpected error occurred.';
}
