import { HttpErrorResponse } from '@angular/common/http';
import { HttpValidationProblemDetails } from '../api';

/** An error of an API call, prepared for the user. */
export interface ApiError {
  /** Error code of the problem details (e.g. `class-name-taken`), if the API sent one. */
  code?: string;
  /** HTTP status code; 0 if the server was not reachable. */
  status?: number;
  /** Message to show to the user. */
  message: string;
}

type ProblemDetailsWithCode = HttpValidationProblemDetails & { code?: string };

/**
 * Turns any error of an API call into a user message. Handles problem details (RFC 9457) of the
 * Grader API, network errors and unexpected server errors. Also looks into `cause`, where Angular
 * resources put errors that are not error-like.
 */
export function toApiError(error: unknown): ApiError {
  const httpError = findHttpError(error);
  if (!httpError) {
    return { message: 'An unexpected error occurred. Please try again.' };
  }

  const { status } = httpError;
  if (status === 0) {
    return {
      status,
      message: 'The server is not reachable. Check your network connection and try again.',
    };
  }

  const problem = isProblemDetails(httpError.error) ? httpError.error : undefined;
  const code = problem?.code;
  if (code === 'validation-failed' && problem?.errors) {
    const messages = Object.values(problem.errors).flat();
    return { code, status, message: `The server rejected the input: ${messages.join(' ')}` };
  }
  if (problem?.detail) {
    return { code, status, message: problem.detail };
  }
  if (status >= 500) {
    return {
      code,
      status,
      message: `The server reported an unexpected error (${status}). Please try again later.`,
    };
  }
  if (status === 404) {
    return { code, status, message: 'The requested data does not exist (anymore).' };
  }
  return { code, status, message: problem?.title ?? `The request failed (${status}).` };
}

function findHttpError(error: unknown): HttpErrorResponse | undefined {
  for (let current = error; current; current = (current as { cause?: unknown }).cause) {
    if (current instanceof HttpErrorResponse) {
      return current;
    }
    if (typeof current !== 'object') {
      return undefined;
    }
  }
  return undefined;
}

function isProblemDetails(body: unknown): body is ProblemDetailsWithCode {
  return typeof body === 'object' && body !== null && ('status' in body || 'title' in body);
}
