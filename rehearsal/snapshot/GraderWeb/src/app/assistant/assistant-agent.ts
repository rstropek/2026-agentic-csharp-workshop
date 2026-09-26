import {
  AbstractAgent,
  BaseEvent,
  HttpAgent,
  omitOptionalNulls,
  RunAgentInput,
} from '@ag-ui/client';
import { HttpErrorResponse } from '@angular/common/http';
import { InjectionToken } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiError, toApiError } from '../shared/api-error';

/** The AG-UI endpoint of the Web API's assistant (same origin, see tech-docs/assistant.md). */
export const assistantUrl = '/api/assistant';

/**
 * AG-UI client of the Web API's assistant. The server (AG-UI .NET SDK 0.x) writes absent optional
 * fields as `null` (e.g. `timestamp`, `metadata`, `parentRunId`), which the 1.0 client rejects and
 * which fails the run. They are omitted before the client processes the events.
 */
export class GraderAssistantAgent extends HttpAgent {
  override run(input: RunAgentInput): Observable<BaseEvent> {
    return super.run(input).pipe(map((event) => omitOptionalNulls(event, 'Event')));
  }
}

/**
 * Turns an error of a run into a user message, like the errors of the generated API client: the AG-UI
 * client rejects with an `Error` with `status` and `payload` (the response body) for an HTTP error
 * (e.g. `assistant-not-configured`), and fetch with a `TypeError` if the server is not reachable.
 */
export function toAssistantError(error: unknown): ApiError {
  if (error instanceof TypeError) {
    return toApiError(new HttpErrorResponse({ status: 0 }));
  }
  const { status, payload } = (error ?? {}) as { status?: unknown; payload?: unknown };
  if (typeof status !== 'number') {
    return toApiError(error);
  }
  return toApiError(new HttpErrorResponse({ status, error: parseJson(payload) }));
}

// Problem details (application/problem+json) arrive as text: the client only parses application/json.
function parseJson(payload: unknown): unknown {
  if (typeof payload !== 'string') {
    return payload;
  }
  try {
    return JSON.parse(payload);
  } catch {
    return payload;
  }
}

/**
 * Creates the agent of a conversation. The agent holds the messages, so a new one starts a new
 * conversation. Tests replace it with a scripted agent.
 */
export const ASSISTANT_AGENT_FACTORY = new InjectionToken<() => AbstractAgent>(
  'ASSISTANT_AGENT_FACTORY',
  {
    providedIn: 'root',
    factory: () => () => new GraderAssistantAgent({ url: assistantUrl }),
  },
);
