import { Component, computed, input } from '@angular/core';
import { formatJson, ToolCallView, truncateLines } from './conversation';

/**
 * A tool call of the assistant: name and status, unfolded the arguments and the result as JSON.
 * Native `<details>`, collapsed by default; the browser keeps it open or closed while the call
 * updates.
 */
@Component({
  selector: 'app-tool-call',
  template: `
    <details class="tool-call">
      <summary>
        <span class="marker" aria-hidden="true"></span>
        <span class="name">{{ call().name }}</span>
        @switch (call().status) {
          @case ('running') {
            <span class="status running">Running…</span>
          }
          @case ('done') {
            <span class="status">Done</span>
          }
          @case ('stopped') {
            <span class="status">Stopped</span>
          }
        }
      </summary>
      <div class="details">
        <span class="column-header">Arguments</span>
        <pre class="json">{{ arguments() }}</pre>
        <span class="column-header">Result</span>
        @if (result(); as result) {
          <pre class="json">{{ result.head }}@if (result.more) {
<span class="muted">{{ result.more }}</span>}@if (result.tail) {
{{ result.tail }}}</pre>
        } @else {
          <p class="small muted">
            {{ call().status === 'running' ? 'Waiting for result…' : 'No result' }}
          </p>
        }
      </div>
    </details>
  `,
  styles: `
    .tool-call {
      border: var(--rule);
      background: var(--color-surface);
    }

    summary {
      display: flex;
      align-items: center;
      gap: 10px;
      min-height: 36px;
      padding: 0 var(--space-3);
      font-size: var(--font-size-small);
      list-style: none;
      cursor: pointer;
    }

    summary::-webkit-details-marker {
      display: none;
    }

    summary:hover {
      background: var(--color-hover);
    }

    summary:focus-visible {
      outline-offset: -2px;
    }

    /* ▸ collapsed, ▾ unfolded */
    .marker {
      width: 12px;
      color: var(--color-muted);
    }

    .marker::before {
      content: "▸";
    }

    details[open] .marker::before {
      content: "▾";
    }

    .name {
      font-family: var(--font-family-mono);
      font-weight: var(--font-weight-semibold);
    }

    .status {
      margin-left: auto;
      color: var(--color-muted);
    }

    .status.running {
      color: var(--color-error-text);
      font-weight: var(--font-weight-semibold);
    }

    /* Padding-left aligns the content with the tool name. */
    .details {
      display: flex;
      flex-direction: column;
      gap: 6px;
      padding: 10px var(--space-3) var(--space-3) 34px;
      border-top: var(--rule);
    }

    .json {
      margin: 0 0 6px;
      font-family: var(--font-family-mono);
      font-size: var(--font-size-small);
      line-height: 1.5;
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }

    .details p {
      margin-bottom: 6px;
    }
  `,
})
export class ToolCall {
  readonly call = input.required<ToolCallView>();

  // The call is a new object on every event of the run; formatting only runs when a text changes.
  private readonly rawArguments = computed(() => this.call().arguments);
  private readonly rawResult = computed(() => this.call().result);

  protected readonly arguments = computed(() => formatJson(this.rawArguments() || '{}'));
  protected readonly result = computed(() => {
    const result = this.rawResult();
    return result === undefined ? undefined : truncateLines(formatJson(result));
  });
}
