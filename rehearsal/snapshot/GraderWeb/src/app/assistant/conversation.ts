import { contentToText, Message } from '@ag-ui/client';

/** A message of the teacher or the assistant's answer to it, as the chat shows it. */
export type Turn =
  | { readonly role: 'user'; readonly id: string; readonly text: string }
  | {
      readonly role: 'assistant';
      readonly id: string;
      readonly toolCalls: readonly ToolCallView[];
      /** Markdown */
      readonly text: string;
    };

/** `stopped`: the run ended without a result (it failed or was aborted). */
export type ToolCallStatus = 'running' | 'done' | 'stopped';

export interface ToolCallView {
  readonly id: string;
  readonly name: string;
  /** JSON as sent by the model; incomplete while the arguments stream in. */
  readonly arguments: string;
  readonly result: string | undefined;
  readonly status: ToolCallStatus;
}

/**
 * Groups the AG-UI messages into turns: each user message, then one assistant turn with everything
 * up to the next user message. The assistant turn shows all tool calls (in call order) before the
 * answer text, so texts of several assistant messages are joined.
 */
export function toTurns(messages: readonly Message[], running: boolean): Turn[] {
  const results = new Map<string, string>();
  for (const message of messages) {
    if (message.role === 'tool') {
      results.set(message.toolCallId, message.error ?? contentToText(message.content));
    }
  }

  const turns: Turn[] = [];
  let toolCalls: ToolCallView[] = [];
  let texts: string[] = [];
  let assistantId: string | undefined;
  const endAssistantTurn = () => {
    if (assistantId !== undefined) {
      turns.push({ role: 'assistant', id: assistantId, toolCalls, text: texts.join('\n\n') });
    }
    assistantId = undefined;
    toolCalls = [];
    texts = [];
  };

  for (const message of messages) {
    if (message.role === 'user') {
      endAssistantTurn();
      turns.push({ role: 'user', id: message.id, text: contentToText(message.content) });
    } else if (message.role === 'assistant') {
      assistantId ??= message.id;
      for (const call of message.toolCalls ?? []) {
        const result = results.get(call.id);
        toolCalls.push({
          id: call.id,
          name: call.function.name,
          arguments: call.function.arguments,
          result,
          status: result !== undefined ? 'done' : running ? 'running' : 'stopped',
        });
      }
      if (message.content?.trim()) {
        texts.push(message.content);
      }
    }
  }
  endAssistantTurn();
  return turns;
}

/**
 * Pretty-prints JSON with a 2-space indent. Objects and arrays below the top level that contain only
 * primitive values stay on one line (e.g. one line per row of a result), as in the design. Text that
 * is not (yet) valid JSON is returned as it is.
 */
export function formatJson(json: string): string {
  let value: unknown;
  try {
    value = JSON.parse(json);
  } catch {
    return json;
  }
  return formatValue(value, 0);
}

function formatValue(value: unknown, depth: number): string {
  if (value === null || typeof value !== 'object') {
    return JSON.stringify(value);
  }
  const entries = Array.isArray(value)
    ? value.map((item) => ({ key: '', item }))
    : Object.entries(value).map(([key, item]) => ({ key: `${JSON.stringify(key)}: `, item }));
  const [open, close] = Array.isArray(value) ? ['[', ']'] : ['{', '}'];
  if (entries.length === 0) {
    return open + close;
  }
  const flat = entries.every(({ item }) => item === null || typeof item !== 'object');
  if (depth > 0 && flat) {
    const inner = entries.map(({ key, item }) => key + JSON.stringify(item)).join(', ');
    return Array.isArray(value) ? `[${inner}]` : `{ ${inner} }`;
  }
  const indent = '  '.repeat(depth + 1);
  const lines = entries.map(({ key, item }) => indent + key + formatValue(item, depth + 1));
  return `${open}\n${lines.join(',\n')}\n${'  '.repeat(depth)}${close}`;
}

/**
 * Cuts long text after `maxLines` lines. For a JSON array, the closing bracket stays visible and
 * the rest is counted in rows (one row per line, see `formatJson`).
 */
export function truncateLines(text: string, maxLines = 8): TruncatedText {
  const lines = text.split('\n');
  if (lines.length <= maxLines) {
    return { head: text };
  }
  if (lines[0] === '[' && lines.at(-1) === ']') {
    const shown = lines.slice(0, maxLines - 1);
    return {
      head: shown.join('\n'),
      more: `  … ${lines.length - 1 - shown.length} more`,
      tail: ']',
    };
  }
  return { head: lines.slice(0, maxLines).join('\n'), more: `… ${lines.length - maxLines} more` };
}

export interface TruncatedText {
  readonly head: string;
  /** The "… n more" line, if lines were cut */
  readonly more?: string;
  /** Lines after `more`, e.g. the closing bracket of an array */
  readonly tail?: string;
}
