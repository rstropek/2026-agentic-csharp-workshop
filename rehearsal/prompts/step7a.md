Build the web UI of Grader. It replaces the calculator smoke test from the beginning (remove it completely: UI, endpoint, logic, tests).

The designer handoff is in spec/design/ (readme.md + mockups). It shows the core design ideas, not every screen or state. For anything not shown, extrapolate from the design system and stay consistent.

This session: the UI foundation and the first use case area, class management (the ClassManagement endpoints of the Web API). Exams follow in the next session; until then the Exams area is a placeholder.

Design system first: derive reusable building blocks from the design – CSS variables (colors incl. dark theme, typography, spacing), global CSS classes, base components and base services (e.g. the global school year selection, turning Problem Details into user messages). Components never hard-code colors.

Not in the design, but required:
- A third top-level area "Settings" next to Classes and Exams: a button that resets the demo data (Web API), and a light/dark toggle (default: light).
- A confirmation dialog before destructive actions (reset demo data, delete). No warnings about unsaved changes.
- Server and network errors are shown to the user.

Requirements:
- Desktop only, optimized for Full HD. No phone layout, no i18n.
- No fonts or icons from the internet at runtime (see the design readme).
- Current Angular: signals, signal forms, resources.
- All API access goes through the generated client (no hand-written URLs), in a way that can be mocked in component tests.
- Exemplary component tests with a mocked API: one base component with behavior (e.g. the confirmation dialog); the class editor (save sends the full student list, validation, server error shown).
- Playwright e2e test: add and edit students of a class. E2E tests run against a temporary SQLite database filled by the demo data generator, never against data/grader.db. Tests must not depend on each other. Update the QA script if needed.
- When the UI works, fill the development database with demo data and inspect the UI in a real browser (e.g. Claude in Chrome) at 1920×1080, in light and dark mode. Compare it with the mockups and fix the differences.
- New tech docs article tech-docs/ui.md with the UI design decisions (follow the tech docs rules in AGENTS.md); update testing.md and api-client.md; add ui.md to the tech docs index in AGENTS.md.
- Done when the UI works and the QA script is green. Then commit directly to main.
