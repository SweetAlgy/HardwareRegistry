import { HttpErrorResponse } from '@angular/common/http';

/** Error body the API returns for every failed request. */
export interface ProblemDetails {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export function toProblemDetails(error: unknown): ProblemDetails | null {
  if (!(error instanceof HttpErrorResponse)) return null;

  const body = error.error;

  return body !== null && typeof body === 'object' && 'status' in body
    ? (body as ProblemDetails)
    : null;
}
