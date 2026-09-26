# Grader – UI mockups

Designer handoff for the Grader web UI (Angular front end, step 7 of the workshop). Desktop only, designed at **1920 × 1080 (Full HD)**.

The black numbered squares on the mockups are **annotations**, not part of the UI. The note with the same number sits in the column to the right of each mockup.

If a mockup and this readme disagree (e.g. CSS variable names in the style guide), this readme wins.

## Files

| File | Content |
| --- | --- |
| `00-style-guide.png` | Colors, type, spacing, icons, components |
| `01-classes.png` | App shell, school year picker, class grid |
| `02-class-editor.png` | Class editor: students grid with a new row and validation errors |
| `03-exam-results.png` | Exams: list filtered by class, results entry with a validation error |
| `04-loading.png` | Loading state (skeleton), busy button |
| `05-dark-mode.png` | Dark mode example (exam results, same layout as 03) |

## Visual principles

- Flat and grid-based: equal-width cells, strong 2px rules between sections and cells.
- Everything is flush left.
- **Use the browser's native controls** (`<button>`, `<input>`, `<select>`, `<input type="date">`) styled with the tokens. No custom dropdowns, date pickers or icon decorations.
- **No rounded corners** anywhere (radius 0).
- One accent color, used sparingly: primary action, current nav item, focus, errors. Everything else is ink on ground.
- No shadows.

## Colors

| Role | Hex | Suggested CSS variable | Used for |
| --- | --- | --- | --- |
| Ground | `#F3F2F2` | `--color-bg` | Page background, button text on accent |
| Surface | `#EAE9E9` | `--color-surface` | Input fields, selects |
| Ink | `#201E1D` | `--color-text` | All text and icons |
| Muted | `#605D5D` | `--color-muted` | Labels, secondary text, column headers |
| Placeholder | `#7D7979` | `--color-placeholder` | Placeholder text ("No result") |
| Divider | Ink at 40% (`color-mix(in srgb, #201E1D 40%, transparent)`) | `--color-divider` | All rules and input borders |
| Skeleton | `#D7D3D3` | `--color-skeleton` | Loading placeholders |
| Accent | `#EC3013` | `--color-accent` | Primary buttons, current nav item, focus ring, error border |
| Accent hover | `#DD2B0F` | `--color-accent-hover` | Primary button hover |
| Accent pressed | `#AE1800` | `--color-accent-pressed` | Primary button pressed |
| Accent text | `#AE1800` | `--color-error-text` | Error messages and error counts (small text) |
| Accent dark | `#7C1405` | `--color-accent-dark` | Text on the selected list row |
| Accent tint | `#FFF2EF` | `--color-accent-tint` | Selected list row |

Don't use the accent for small body text; use the accent text color (`#AE1800`) instead.

## Dark mode

Dark mode changes **only the color tokens**. Layout, type, spacing and components stay exactly the same, so every component has to take its colors from the variables (no hard-coded hex values).

The user switches the theme with a light/dark toggle in the Settings area (not shown in the mockups). **Light is the default**; the OS setting is ignored. The choice is remembered in the browser. The theme is set as an attribute on `<html>`, which switches the tokens:

| Role | Light | Dark | Notes |
| --- | --- | --- | --- |
| Ground | `#F3F2F2` | `#201E1D` | Page background, button text on accent |
| Surface | `#EAE9E9` | `#2D2B2B` | Inputs, selects; lighter than the ground |
| Ink | `#201E1D` | `#F3F2F2` | All text and icons |
| Muted | `#605D5D` | `#BAB6B6` | Labels, secondary text |
| Placeholder | `#7D7979` | `#9B9797` | "No result" |
| Divider | Ink at 40% | `#F3F2F2` at 32% | All rules and input borders |
| Skeleton | `#D7D3D3` | `#444141` | Loading placeholders |
| Accent | `#EC3013` | `#FF563C` | One step lighter to keep contrast on dark |
| Accent hover | `#DD2B0F` | `#FF9783` | On dark, hover and pressed get lighter, not darker |
| Accent pressed | `#AE1800` | `#FFC4B8` | |
| Error text | `#AE1800` | `#FF9783` | Error messages and counts |
| Accent tint | `#FFF2EF` | `#4D170E` | Selected list row |
| Accent dark | `#7C1405` | `#FFC4B8` | Text on accent tint |

```css
:root[data-theme='dark'] {
  color-scheme: dark; /* native controls (select, date picker) follow */
  --color-bg: #201e1d;
  --color-surface: #2d2b2b;
  --color-text: #f3f2f2;
  --color-muted: #bab6b6;
  --color-placeholder: #9b9797;
  --color-divider: color-mix(in srgb, #f3f2f2 32%, transparent);
  --color-skeleton: #444141;
  --color-accent: #ff563c;
  --color-accent-hover: #ff9783;
  --color-accent-pressed: #ffc4b8;
  --color-error-text: #ff9783;
  --color-accent-tint: #4d170e;
  --color-accent-dark: #ffc4b8;
}
```

## Typography

Font: **Archivo**, weights 400, 600, 800. One family for headings and body.

Load it locally from npm (no dependency on the Google Fonts CDN or the network at runtime):

```bash
npm install @fontsource/archivo
```

In `src/styles.css`:

```css
@import '@fontsource/archivo/400.css';
@import '@fontsource/archivo/600.css';
@import '@fontsource/archivo/800.css';

body { font-family: 'Archivo', system-ui, sans-serif; }
```

| Style | Size / weight | Extra | Example |
| --- | --- | --- | --- |
| Page title | 42px / 800 | line-height 1.12, letter-spacing −0.015em | "Classes" |
| Detail title | 32px / 800 | same | "Computer Science" |
| Cell title | 32px / 800 | line-height 1.1 | "2B" |
| Section | 20px / 800 | | "Students", "Results" |
| Brand | 18px / 800 | | "Grader" |
| Body | 15px / 400 | line-height 1.55 | Student names |
| Input, button, nav | 14px / 400 (buttons 800) | | Field values |
| Small | 13px / 400 | | Breadcrumb, dates in lists |
| Label | 12px / 400, muted | | Field labels ("Class name") |
| Overline | 12px / 400, uppercase, letter-spacing 0.08em, muted | | "SCHOOL YEAR 2026/27" |
| Column header | 11px / 400, uppercase, letter-spacing 0.08em, muted | | "LAST NAME" |
| Error message | 12px / 600, accent text | | "Last name is required" |

Numbers use tabular figures (`font-variant-numeric: tabular-nums`), and percentages are right-aligned.

## Spacing and layout

- Spacing scale: **4, 8, 12, 16, 24, 32 px**
- Header: 56px high, 24px horizontal padding, 2px bottom rule
- Page content: 32px padding, uses the full width
- Exams page: 400px list column | 2px vertical rule | detail
- Class grid: 5 equal columns, cells min. 240px high with 24px padding, 2px rules between cells
- Results: two equal columns (students 1–10, 11–20), 40px gap
- Long lists scroll with the page; the header stays fixed

## Borders and elevation

- 2px rule (`--color-divider`): below the header, between page sections, between grid cells, under column headers
- 1px rule (`--color-divider`): between table/list rows, input borders
- Radius: 0 everywhere
- No shadows

## Components

All controls are native HTML elements, styled with the tokens.

**Buttons** — `<button>`, 14px / 800, padding 8px 14px, min-height 36px, text only (no icons). Width fits the label.

| Variant | Style | Used for |
| --- | --- | --- |
| Primary | Accent background, ground-colored text; hover `#DD2B0F`, pressed `#AE1800` | Save, New class, New exam |
| Secondary | Transparent, 1px divider border; hover ink at 7%, pressed ink at 14% | Cancel, Add student |
| Ghost | Transparent, accent text; hover accent at 10% | Delete class / exam |
| Icon | 36 × 36, transparent, × icon | Remove student row (the only icon in the app) |
| Disabled / busy | 45% opacity, not clickable; busy label "Saving…" | While saving |

**Text field** — `<input>`, surface background, 1px divider border, min-height 36px, padding 6px 10px, 14px text. Label above it (12px muted, 5px gap).
- Hover: border ink at 45%
- Focus: accent border + 2px accent outline
- Error: 2px accent border, error message below (4px gap), no icon

**Select** — Native `<select>` with the same styling as the text field. The browser draws the arrow and the open list.

**Date field** — Native `<input type="date">` with the same styling. The browser draws the calendar icon and picker.

**Percentage field** — `<input type="number" min="0" max="100" step="any">`, 104px wide, right-aligned value, "%" as plain text after the field (muted). Empty shows the placeholder "No result".

**Nav item** — `<a>`, 14px ink. The current item is accent, weight 600, with a 2px accent bottom border that sits on the header's rule.

**List row (exam list)** — `<a>`, padding 10px 12px, date (13px muted) above course (15px / 600), 1px rule below. Selected: accent tint background, accent dark text, course 800.

**Class cell** — `<a>`, class name 32px / 800, student count 14px muted. Hover: ink at 5% background.

**Error summary** — Text next to the Save button, e.g. "2 fields need attention", 13px / 600, error text color.

**Skeleton** — Flat `#D7D3D3` blocks the size of the real content (e.g. 56 × 32 for a class name, 96 × 12 for a count). No animation.

## Icons

Only one: × for removing a student row. Lucide `x`, 18px, stroke width 2, `currentColor`, via [@lucide/angular](https://www.npmjs.com/package/@lucide/angular) (bundled, no network at runtime; the older `lucide-angular` package does not support current Angular versions):

```bash
npm install @lucide/angular
```

```ts
import { LucideX } from '@lucide/angular';

@Component({
  imports: [LucideX],
  template: `<button class="btn btn-icon" aria-label="Remove student">
               <svg lucideX [size]="18" [strokeWidth]="2" aria-hidden="true"></svg>
             </button>`,
})
export class StudentRow {}
```

## Interaction states

- Every interactive element has a hover tint and a pressed state (see Buttons).
- Keyboard focus: `outline: 2px solid #EC3013; outline-offset: 2px` (never the browser default).
- Text selection: accent at 30%.
