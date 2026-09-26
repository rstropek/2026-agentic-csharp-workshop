# Web UI

## Approach

The Angular app (`GraderWeb`) implements the designer handoff in `spec/design/` (`readme.md` + mockups). The mockups show the core ideas, not every screen or state; anything else is extrapolated from the design system. If a mockup and the design readme disagree, the readme wins.

- Desktop only, optimized for 1920 × 1080; no phone layout, no i18n.
- Three top-level areas: Classes, Exams, Settings (theme, demo data reset). Each area is a lazy route (`app.routes.ts`); route parameters are bound to component inputs.
- Folders: `app/shared/` (base services and components), one folder per area (`app/classes/`, `app/exams/`, `app/settings/`), global styles in `src/styles.css` + `src/styles/`.

## Design system

**Tokens** (`src/styles/tokens.css`): all colors, font sizes/weights, spacing (4/8/12/16/24/32) and rules are CSS variables. Components never hard-code colors; hover/pressed/selection tints are derived from the base tokens with `color-mix`, so they follow the theme automatically. A new color needs a light and a dark value.

**Global CSS classes** for native controls and typography (`src/styles/*.css`): `btn` + `btn-primary|secondary|ghost|icon`, `field`/`field-label`/`field-error`, `input` (text fields and selects), `table`, `page`, `section`, `title-row`, `toolbar`, `page-title`, `detail-title`, `cell-title`, `section-title`, `overline`, `muted`, `error-text`, `tabular`, `skeleton`, `visually-hidden`.
Why classes instead of wrapper components: the design mandates native controls, and Signal Forms bind `[formField]` directly to native inputs. Wrapper components would need custom form controls and hide the native element's attributes (labels, ARIA) from the templates.

**Base components** (`app/shared/`), only where there is behavior or structure to reuse:

| Component | Purpose |
| --- | --- |
| `ConfirmDialog` | Modal confirmation for destructive actions (native `<dialog>`), rendered once in the app shell |
| `LoadError` | "X could not be loaded" + message + "Try again" |
| `FieldError` | First validation error of a Signal Forms field, once touched; `ariaInvalid()` for the control |
| `Skeleton` | Flat loading placeholder of a given size |
| `SchoolYearPicker` | The global school year select in the header |
| `PercentageField` | Number input (104px, right-aligned, "No result" placeholder) + "%" + `FieldError`; bind a `FieldTree<number \| null>` and add the `percentage()` schema rules (0–100) |

**Base services** (`app/shared/`): `SchoolYearStore` (global school year selection), `ThemeService` (light/dark), `ConfirmService` (opens `ConfirmDialog`, returns a promise), `toApiError` (errors → user messages, see [api-client.md](api-client.md#usage-in-angular)).

## Design decisions

- **Theme:** light is the default, the OS setting is ignored. `ThemeService` stores the choice in local storage and sets `data-theme` on `<html>`; dark mode only swaps color tokens. An inline script in `src/index.html` applies the stored theme before Angular starts, so dark mode does not flash light. It must use the same storage key as `ThemeService`.
- **No runtime downloads:** the Archivo font comes from `@fontsource/archivo` (bundled via CSS imports), the only icon (× to remove a row) from `@lucide/angular`. Do not add CDN links.
- **School year is global state, not part of the URL.** The newest year is selected by default; the selection survives reloading the list (e.g. after a demo data reset) if the year still exists. Opening a class via URL selects the class's year. A class belongs to one year, so selecting another year in the class editor goes back to the class list.
- **Loading states:** skeletons that mirror the final layout, never spinners. The shell (header, navigation, picker) stays usable while an area loads. `rxResource` cancels a pending request when its parameters change.
- **Every server or network error is shown** where the user acted: load failures via `LoadError` with retry, failed actions (save, delete, reset) as `role="alert"` text next to the action's button. No global toasts.
- **Editors save one document.** The class editor sends the class name and the complete student list in one request (see [web-api.md](web-api.md#updating-a-parent-with-its-children)). The form model is a `linkedSignal` of the loaded resource: the save response is written into the resource, which replaces the local state (sorted, with new ids). Removing a row is local until Save. Cancel goes back to the list and discards edits; there are no unsaved-changes warnings.
- **Validation (Signal Forms):** limits mirror the API; whitespace-only names count as missing and names are trimmed before sending. Errors show on blur and for all fields on Save; Save focuses the first invalid field and shows "n fields need attention"; a server error replaces that text. While saving, fields are read-only (`readonly()` rule on the form root), buttons are disabled and Save reads "Saving…". After a successful save a muted "Saved" shows until the next edit (for a new class it is passed along with the navigation to the new URL as router `info`).
- **Exams area = list + detail.** `Exams` (`app/exams/exams.ts`) shows the class filter and the exam list (400px column) and a child outlet for the exam (`/exams`, `/exams/new`, `/exams/:examId`). `ExamsStore` holds the classes of the selected year, the class filter, the exam list and the courses; it is provided by the `Exams` component (`@Service({ autoProvided: false })`), so it lives while the area is open and the editor can reload the list after saving or deleting.
- **The class filter is state, not part of the URL** (like the school year). The first class is selected by default. Opening an exam via URL selects its school year and class (the API returns both with the exam); the requested class is kept while the classes of that year load. An exam belongs to one class, so selecting another class or year closes the editor (back to `/exams`).
- **Exam editor:** date, course and one result per student of the class; empty means no result (`null`). Save sends all students, `null` included (see [web-api.md](web-api.md#updating-a-parent-with-its-children)). A new exam starts with today's date, no course and the students of the selected class (loaded via the class). Results are split into two equal columns (first half left), so Tab goes down the left column, then the right one.
- **Confirmation only for destructive server actions** (delete class, delete exam, reset demo data). The dialog focuses Cancel, so Enter never confirms by accident; Escape cancels.
- **Accessibility:** inputs in grids get an `aria-label`: the column header text, or "<column> of <row>" if a row has only one input (e.g. "Percentage of Anderson, Maya"), `aria-invalid` only once touched, errors linked via `aria-describedby`, status texts use `role="status"`, pages without a visible title get a visually hidden `<h1>`.

## Gotchas

- **`[formField]` owns the control's state attributes.** Do not bind `value`, `disabled`, `readonly`, `min`, `max` on such inputs; use schema rules. It also sets `maxlength` from `maxLength()` rules, so browsers stop input at the limit.
- **Track form array rows by field:** `@for (student of form.students; track student)`. Signal Forms keep field state (touched, errors) per array item object, so rows can be inserted on top and removed without state moving to other rows.
- **Focus after adding a row** works with `afterNextRender(..., { injector })` and `focusBoundControl()`; the input does not exist before the next render.
- **Signal Forms and nullable numbers:** unlike older guidance (and the `angular-developer` skill), `number | null` is the right model type for an optional `<input type="number">`: empty ⇄ `null`. Input the browser cannot parse (e.g. "1e") is a `parse` error without a message; `FieldError` shows its `parseMessage` for it.
- **A date input marks the form dirty when it is rendered.** Signal Forms watch the validity of date inputs via CSS animations and re-read the value on the first `animationstart`, which counts as an edit. Do not base UI on `dirty()` in forms with a date field; the exam editor shows "Saved" while the form value equals the saved document instead.
- **Selects need an `input` event in component tests.** Signal Forms listen to `input`, not `change`; browsers fire both, so tests dispatch both.
- **The router reuses the editor component** when another exam of the list is opened (same route, new `examId` input). Per-document UI state (server error, "Saved", error count) is a `linkedSignal` on the id, so it starts over.
- **Number spin buttons are hidden** in `PercentageField` (they covered the right-aligned value); arrow keys still step. Native date and number fields format values in the browser's locale (e.g. "67,5", "24.09.2026" in German), unlike the mockups.
- **Component style budget:** `angular.json` warns above 4 kB per component stylesheet. Shared styles belong in the global classes, not in component styles.
- **Comparing with the mockups at 1920 × 1080:** a maximized browser window on a larger screen cannot be resized to that size. Take screenshots with a Playwright page with a 1920 × 1080 viewport instead.
