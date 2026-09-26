# Grader – UI mockups

Designer handoff for the Grader web UI. Desktop only, designed at **1920 × 1080 (Full HD)**.

The black numbered squares on the mockups are **annotations**, not part of the UI. The note with the same number sits in the column to the right of each mockup.

## Files

| File                  | Content                                                              |
| --------------------- | -------------------------------------------------------------------- |
| `00-style-guide.png`  | Colors, type, spacing, icons, components                             |
| `01-classes.png`      | App shell, school year picker (open), class grid                     |
| `02-class-editor.png` | Class editor: students grid with a new row and validation errors     |
| `03-exam-results.png` | Exams: list filtered by class, results entry with a validation error |
| `04-loading.png`      | Loading state (skeleton), busy button                                |
| `05-dark-mode.png`    | Dark mode example (exam results, same layout as 03)                  |

## Visual principles

- Flat and grid-based: equal-width cells, strong 2px rules between sections and cells.
- Everything is flush left, including the labels inside buttons (a trailing icon sits on the right).
- **No rounded corners** anywhere (radius 0).
- One accent color, used sparingly: primary action, current nav item, focus, errors. Everything else is ink on ground.
- No shadows, except floating menus (the dropdown).

## Colors

| Role           | Hex                                                         | Suggested CSS variable   | Used for                                                    |
| -------------- | ----------------------------------------------------------- | ------------------------ | ----------------------------------------------------------- |
| Ground         | `#F3F2F2`                                                   | `--color-bg`             | Page background, button text on accent                      |
| Surface        | `#EAE9E9`                                                   | `--color-surface`        | Input fields, dropdown menus                                |
| Ink            | `#201E1D`                                                   | `--color-text`           | All text and icons                                          |
| Muted          | `#605D5D`                                                   | `--color-muted`          | Labels, secondary text, column headers                      |
| Placeholder    | `#7D7979`                                                   | `--color-placeholder`    | Placeholder text ("No result")                              |
| Divider        | Ink at 40% (`color-mix(in srgb, #201E1D 40%, transparent)`) | `--color-divider`        | All rules and input borders                                 |
| Skeleton       | `#D7D3D3`                                                   | `--color-skeleton`       | Loading placeholders                                        |
| Accent         | `#EC3013`                                                   | `--color-accent`         | Primary buttons, current nav item, focus ring, error border |
| Accent hover   | `#DD2B0F`                                                   | `--color-accent-hover`   | Primary button hover                                        |
| Accent pressed | `#AE1800`                                                   | `--color-accent-pressed` | Primary button pressed                                      |
| Accent text    | `#AE1800`                                                   | `--color-error-text`     | Error messages and error counts (small text)                |
| Accent dark    | `#7C1405`                                                   | `--color-accent-dark`    | Text on accent-tinted fills (selected row, tag)             |
| Accent tint    | `#FFF2EF`                                                   | `--color-accent-tint`    | Selected list row, "New" tag, open-menu selected item       |

Don't use the accent for small body text; use the accent text color (`#AE1800`) instead.

## Dark mode

Dark mode changes **only the color tokens**. Layout, type, spacing and components stay exactly the same, so every component has to take its colors from the variables (no hard-coded hex values). Follow the OS setting (`prefers-color-scheme`) by default.

| Role           | Light      | Dark                                                  | Notes                                                      |
| -------------- | ---------- | ----------------------------------------------------- | ---------------------------------------------------------- |
| Ground         | `#F3F2F2`  | `#201E1D`                                             | Page background, button text on accent                     |
| Surface        | `#EAE9E9`  | `#2D2B2B`                                             | Inputs, menus; lighter than the ground                     |
| Ink            | `#201E1D`  | `#F3F2F2`                                             | All text and icons                                         |
| Muted          | `#605D5D`  | `#BAB6B6`                                             | Labels, secondary text                                     |
| Placeholder    | `#7D7979`  | `#9B9797`                                             | "No result"                                                |
| Divider        | Ink at 40% | `#F3F2F2` at 32%                                      | All rules and input borders                                |
| Skeleton       | `#D7D3D3`  | `#444141`                                             | Loading placeholders                                       |
| Accent         | `#EC3013`  | `#FF563C`                                             | One step lighter to keep contrast on dark                  |
| Accent hover   | `#DD2B0F`  | `#FF9783`                                             | On dark, hover and pressed get lighter, not darker         |
| Accent pressed | `#AE1800`  | `#FFC4B8`                                             |                                                            |
| Error text     | `#AE1800`  | `#FF9783`                                             | Error messages and counts                                  |
| Accent tint    | `#FFF2EF`  | `#4D170E`                                             | Selected row, "New" tag                                    |
| Accent dark    | `#7C1405`  | `#FFC4B8`                                             | Text on accent tint                                        |
| Menu shadow    | ink shadow | 1px edge `#F3F2F2` at 12% + `0 6px 16px` black at 50% | Shadows are hard to see on dark, so menus also get an edge |

```css
@media (prefers-color-scheme: dark) {
  :root {
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
}
```

## Typography

Font: **Archivo** (Google Fonts), weights 400, 600, 800. One family for headings and body.

```html
<link
  href="https://fonts.googleapis.com/css2?family=Archivo:wght@400;600;800&display=swap"
  rel="stylesheet"
/>
```

| Style              | Size / weight                                       | Extra                                     | Example                     |
| ------------------ | --------------------------------------------------- | ----------------------------------------- | --------------------------- |
| Page title         | 42px / 800                                          | line-height 1.12, letter-spacing −0.015em | "Classes"                   |
| Detail title       | 32px / 800                                          | same                                      | "Computer Science"          |
| Cell title         | 32px / 800                                          | line-height 1.1                           | "2B"                        |
| Section            | 20px / 800                                          |                                           | "Students", "Results"       |
| Brand              | 18px / 800                                          |                                           | "Grader"                    |
| Body               | 15px / 400                                          | line-height 1.55                          | Student names               |
| Input, button, nav | 14px / 400 (buttons 800)                            |                                           | Field values                |
| Small              | 13px / 400                                          |                                           | Breadcrumb, dates in lists  |
| Label              | 12px / 400, muted                                   |                                           | Field labels ("Class name") |
| Overline           | 12px / 400, uppercase, letter-spacing 0.08em, muted |                                           | "SCHOOL YEAR 2026/27"       |
| Column header      | 11px / 400, uppercase, letter-spacing 0.08em, muted |                                           | "LAST NAME"                 |
| Error message      | 12px / 600, accent text                             | 14px icon before it                       | "Last name is required"     |

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
- Menu shadow (dropdown only): `0 3px 10px color-mix(in srgb, #2D2B2B 16%, transparent)`

## Components

**Buttons** — 14px / 800, padding 8px 14px, min-height 36px, label flush left, optional trailing 16–18px icon pushed to the right edge.

| Variant         | Style                                                                      |
| --------------- | -------------------------------------------------------------------------- |
| Primary         | Accent background, ground-colored text; hover `#DD2B0F`, pressed `#AE1800` |
| Secondary       | Transparent, 1px divider border; hover ink at 7%, pressed ink at 14%       |
| Ghost           | Transparent, accent text, leading icon; hover accent at 10%                |
| Icon            | 36 × 36, transparent, 18px icon (e.g. remove row)                          |
| Disabled / busy | 45% opacity, not clickable; busy label "Saving…"                           |

**Text field** — Surface background, 1px divider border, min-height 36px, padding 6px 10px, 14px text. Label above it (12px muted, 5px gap).

- Hover: border ink at 45%
- Focus: accent border + 2px accent outline, accent caret
- Error: 2px accent border, error message below (4px gap)

**Select / date field** — Same as text field with a trailing chevron-down / calendar icon (18px).

**Percentage field** — Text field 104px wide, right-aligned value, "%" suffix outside the field (muted). Empty shows the placeholder "No result". Accepts decimals, range 0–100.

**Dropdown menu** — Surface background, 1px divider border, menu shadow. Items 40px high, 1px rules between them. The selected item is bold on the accent tint, with a check icon on the right.

**Tag** — 11px / 600, padding 3px 10px, accent tint background, accent dark text. Used for "New" on unsaved rows.

**Nav item** — 14px ink. The current item is accent, weight 600, with a 2px accent bar that sits on the header's bottom rule.

**List row (exam list)** — Padding 10px 12px, date (13px muted) above course (15px / 600), 1px rule below. Selected: accent tint background, accent dark text, course 800.

**Class cell** — Class name 32px / 800, student count 14px muted, arrow-right icon at the bottom. The whole cell is a link. Hover: ink at 5% background, accent arrow.

**Error summary** — Next to the Save button: circle-alert icon + "2 fields need attention", 13px / 600, accent text.

**Skeleton** — Flat `#D7D3D3` blocks with the size of the real content (e.g. 56 × 32 for a class name, 96 × 12 for a count). No animation required.

## Icons

[Lucide](https://lucide.dev), 20px, stroke width 2, `currentColor`.
Used: `menu`, `chevron-down`, `chevron-right`, `plus`, `x`, `trash-2`, `circle-alert`, `arrow-right`, `check`, `calendar`.

## Interaction states

- Every interactive element has a hover tint and a pressed state (see Buttons).
- Keyboard focus: `outline: 2px solid #EC3013; outline-offset: 2px` (never the browser default).
- Text selection: accent at 30%.
