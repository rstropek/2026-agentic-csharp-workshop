import { Component, computed, input, ViewEncapsulation } from '@angular/core';
import { Marked, Tokens } from 'marked';

/**
 * Markdown of the assistant's answers. Only the subset of the design is rendered as such
 * (paragraphs, bold, italic, inline code, links, flat lists, tables); everything else becomes text:
 * headings and code blocks are paragraphs, nested list items are flattened into the outer list,
 * raw HTML is escaped. Angular's sanitizer (`[innerHTML]`) removes anything unsafe on top.
 */
const markdown = new Marked({
  gfm: true,
  async: false,
  renderer: {
    heading({ tokens }) {
      return `<p>${this.parser.parseInline(tokens)}</p>\n`;
    },
    code({ text }) {
      return `<p><code>${escapeHtml(text)}</code></p>\n`;
    },
    blockquote({ tokens }) {
      return this.parser.parse(tokens);
    },
    hr() {
      return '';
    },
    html({ text }) {
      return escapeHtml(text);
    },
    image({ text }) {
      return escapeHtml(text);
    },
    checkbox() {
      return '';
    },
    del({ tokens }) {
      return this.parser.parseInline(tokens);
    },
    link({ href, tokens }) {
      // A new tab: leaving the app would end the conversation.
      const text = this.parser.parseInline(tokens);
      return `<a href="${escapeHtml(href)}" target="_blank" rel="noopener noreferrer">${text}</a>`;
    },
    list(token) {
      const tag = token.ordered ? 'ol' : 'ul';
      const start =
        token.ordered && token.start !== 1 && token.start !== '' ? ` start="${token.start}"` : '';
      const items = flattenItems(token.items).map((item) => this.listitem(item));
      return `<${tag}${start}>\n${items.join('')}</${tag}>\n`;
    },
    table(token) {
      const numeric = token.header.map((_, column) => isNumericColumn(token, column));
      const cell = (tag: 'th' | 'td', cell: Tokens.TableCell, column: number) =>
        `<${tag}${numeric[column] ? ' class="num"' : ''}>${this.parser.parseInline(cell.tokens)}</${tag}>`;
      const head = token.header.map((header, column) => cell('th', header, column)).join('');
      const rows = token.rows
        .map((row) => `<tr>${row.map((data, column) => cell('td', data, column)).join('')}</tr>\n`)
        .join('');
      // The wrapper scrolls wide tables horizontally.
      return `<div class="table-scroll"><table><thead><tr>${head}</tr></thead><tbody>${rows}</tbody></table></div>\n`;
    },
  },
});

/** Items of nested lists follow their parent item in the same (outer) list. */
function flattenItems(items: readonly Tokens.ListItem[]): Tokens.ListItem[] {
  return items.flatMap((item) => {
    const nested = item.tokens.filter((token): token is Tokens.List => token.type === 'list');
    const own = { ...item, tokens: item.tokens.filter((token) => token.type !== 'list') };
    return [own, ...flattenItems(nested.flatMap((list) => list.items))];
  });
}

/** Numbers and percentages ("67.5 %", "~69.6 %"); empty cells and dashes (no result) do not count against it. */
const numberPattern = /^[~≈<>≤≥]?\s?[-−+]?\d[\d,.]*\s?%?$/;
const emptyPattern = /^(|-|–|—)$/;

function isNumericColumn(table: Tokens.Table, column: number): boolean {
  if (table.align[column] === 'right') {
    return true;
  }
  const texts = table.rows.map((row) => row[column]?.text.trim() ?? '');
  return (
    texts.some((text) => numberPattern.test(text)) &&
    texts.every((text) => numberPattern.test(text) || emptyPattern.test(text))
  );
}

function escapeHtml(text: string): string {
  return text
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

export function renderMarkdown(text: string): string {
  return markdown.parse(text) as string;
}

/** Renders markdown of the assistant, again whenever the text changes (while it streams in). */
@Component({
  selector: 'app-markdown',
  template: `<div class="markdown" [innerHTML]="html()"></div>`,
  // The rendered HTML does not get the attributes of emulated encapsulation; the styles are
  // scoped by the `app-markdown` selector instead.
  encapsulation: ViewEncapsulation.None,
  styles: `
    app-markdown {
      display: block;
    }

    app-markdown .markdown {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }

    app-markdown ul,
    app-markdown ol {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
      margin: 0;
      padding-left: var(--space-6);
    }

    app-markdown li > p {
      margin: 0;
    }

    app-markdown code {
      padding: 1px 4px;
      background: var(--color-surface);
      font-family: var(--font-family-mono);
      font-size: var(--font-size-small);
    }

    app-markdown a {
      color: var(--color-error-text);
      text-decoration: underline;
    }

    app-markdown .table-scroll {
      overflow-x: auto;
    }

    app-markdown table {
      min-width: 400px;
      border-collapse: collapse;
      font-size: var(--font-size-control);
    }

    app-markdown th,
    app-markdown td {
      padding: 6px var(--space-2);
      border-bottom: var(--rule);
      text-align: left;
      vertical-align: top;
    }

    app-markdown th {
      border-bottom: var(--rule-strong);
      color: var(--color-muted);
      font-size: var(--font-size-column-header);
      font-weight: var(--font-weight-regular);
      text-transform: uppercase;
      letter-spacing: var(--letter-spacing-caps);
    }

    app-markdown .num {
      text-align: right;
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class Markdown {
  readonly text = input.required<string>();

  protected readonly html = computed(() => renderMarkdown(this.text()));
}
