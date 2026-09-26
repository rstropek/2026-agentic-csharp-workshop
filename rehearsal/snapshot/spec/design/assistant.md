## Assistant (chat)

Third area in the top navigation: **Classes · Exams · Assistant**. Message history on top, composer pinned to the bottom. No attachments.

### Layout

- One centered column, **880 px** wide, for the history and the composer
- History scrolls; it stays scrolled to the newest message while answering
- Composer area: 2 px rule on top, padding 16 px 32 px 24 px
- 24–32 px between messages, 12 px between blocks inside a message

### Messages

| Element | Style |
| --- | --- |
| Label | "You" / "Assistant", 11px caps, letter-spacing 0.08em, muted, 6 px above the content |
| User message | Surface fill, padding 12 px 16 px, body text |
| Assistant message | No fill; content rendered from markdown |
| No bubbles, no avatars, everything flush left | |

### Markdown in assistant messages

Supported: paragraphs, **bold**, *italic*, `inline code`, links, bullet lists, numbered lists, tables. Nothing else (no headings, images, code blocks, nesting).

| Element | Style |
| --- | --- |
| Paragraph | 15px / 400, line-height 1.55 |
| Inline code | Monospace 13px on surface, padding 1px 4px |
| Link | Accent text color (`#AE1800`), underlined |
| Bullet list | Native `<ul>` (disc), indent 24 px, 4 px between items |
| Numbered list | Native `<ol>` (decimal), same spacing |
| Nested lists | Not supported, render flat |
| Table | Same look as the app's tables: header 11px caps muted with a 2 px rule below; rows 14px, padding 6–8 px, 1 px rule below each row; numbers right-aligned with tabular figures; width fits content (min. 400 px), wider tables scroll horizontally |

Monospace stack: `ui-monospace, Menlo, Consolas, monospace` (system fonts, no download).

### Tool calls

Shown inside the assistant message, **before** the answer text, one block per call, in call order, 8 px apart. Build it with native `<details>` / `<summary>` — no custom toggle code.

**Block:** surface fill, 1 px divider border, radius 0.

**Summary row** (always visible, min-height 36 px, padding 0 12 px, 13px):
- Disclosure marker ▸ (collapsed) / ▾ (unfolded), muted
- Tool name in monospace 13px / 600, e.g. `get_exam_results`
- Status right-aligned

| State | Status text | Style |
| --- | --- | --- |
| Running | "Running…" | 13px / 600, error-text color (`#AE1800`, dark mode `#FF9783`); no spinner |
| Done | "Done" | 13px, muted |

**Details** (only when unfolded; 1 px rule on top, padding 10 px 12 px 12 px 34 px so the content aligns with the tool name):
- "Arguments" label (11px caps, muted), then the arguments as JSON
- "Result" label, then the result as JSON
- JSON: pretty-printed, 2-space indent, monospace 13px, line-height 1.5
- While running, the result reads "Waiting for result…" (13px, muted)
- Long results: show the first 8 lines, then "… n more"

**Default:** collapsed, in every state. Unfolding is up to the user.

### Composer

- Native `<textarea>` + primary **Send** button, both 44 px high, 12 px gap
- Placeholder "Ask about classes, exams or results…"
- Enter sends, Shift+Enter adds a new line; the field grows up to 6 lines
- While the assistant answers: field and button disabled (45% opacity), button label "Answering…"