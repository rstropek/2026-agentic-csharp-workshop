# Grader – Workshop Storyboard

Full-day workshop: build a grading app for teachers and students with a coding agent. .NET 11 backend, Angular frontend.

How to use this storyboard:

* Every step has a **script** (copy & run in a terminal) and/or a **prompt** (copy & paste into your coding agent).
* Start each prompt in a **fresh agent session** in the repository root (`grader/`).
* Each step ends with a git commit. If a step goes wrong: `git reset --hard && git clean -fd` and re-run it.
* Rehearsed with Claude Code (`claude -p`, Opus 5.5, effort medium). Durations below come from that rehearsal.

## Prerequisites

* .NET 11 SDK (RC 1 or newer: `dotnet --list-sdks`)
* Node.js 24 LTS + npm
* Git, bash/zsh (macOS, Linux, WSL or Git Bash)
* A coding agent that supports `AGENTS.md` and skills (e.g. Claude Code, GitHub Copilot, Codex)
* For step 7: a browser the agent can control (e.g. Chrome with [Claude in Chrome](https://claude.com/chrome), `claude --chrome`). Set the browser zoom to **100%** and make the window at least 1920 px wide – the UI is designed for Full HD, and with a different zoom the agent has to emulate the viewport to compare it with the mockups

---

## Step 1 – App skeleton

Goal: solution with class library, Web API, tests and Angular frontend – created by **deterministic tools**, not by the LLM.

Script (run in the folder that should contain `grader/`, ~1 min):

```bash
#!/usr/bin/env bash
# Step 1 – app skeleton, created with deterministic tools only
set -euo pipefail

mkdir grader && cd grader
git init -b main

# .NET: pin SDK to .NET 11 RC (rolls forward to newer RCs/GA), solution, projects
dotnet new globaljson --sdk-version 11.0.100-rc.1.26425.128 --roll-forward latestFeature
dotnet new gitignore
dotnet new sln -n Grader

dotnet new classlib -n GraderLogic -o GraderLogic
dotnet new webapi   -n GraderApi   -o GraderApi
dotnet new xunit    -n GraderTests -o GraderTests --xunit-version v3
dotnet sln add GraderLogic GraderApi GraderTests

dotnet reference add GraderLogic --project GraderApi
dotnet reference add GraderLogic GraderApi --project GraderTests

dotnet package add Scalar.AspNetCore --project GraderApi
dotnet package add Microsoft.Extensions.ApiDescription.Server --prerelease --project GraderApi
dotnet package add NSubstitute --project GraderTests
dotnet package add NSubstitute.Analyzers.CSharp --project GraderTests
dotnet package add Microsoft.AspNetCore.Mvc.Testing --prerelease --project GraderTests

# Angular: latest stable CLI, zoneless, no SSR, plain CSS, separate files, routing, vitest
npx -y @angular/cli@latest new GraderWeb \
  --skip-git --interactive=false --package-manager=npm --ai-config=none \
  --zoneless --ssr=false --style=css --routing --strict --standalone \
  --inline-style=false --inline-template=false --test-runner=vitest

cd GraderWeb
# Angular team's best practices for AI agents (overwrites an existing AGENTS.md)
curl -fsSL https://angular.dev/assets/context/best-practices.md -o AGENTS.md
npm install --save-dev @playwright/test ng-openapi
npx playwright install chromium
cd ..

dotnet build
git add -A
git commit -m "Initial project setup"
```

Resulting structure:

* `GraderLogic` – class library for logic and data access
* `GraderApi` – ASP.NET Core Web API (minimal APIs), references `GraderLogic`; Scalar for API docs, `Microsoft.Extensions.ApiDescription.Server` writes the OpenAPI spec on build (`GraderApi/obj/GraderApi.json` until configured otherwise)
* `GraderTests` – xUnit v3 on Microsoft Testing Platform (MTP), references `GraderLogic` + `GraderApi`; NSubstitute + analyzers, `Mvc.Testing` for integration tests
* `GraderWeb` – Angular (zoneless, no SSR, plain CSS, standalone, strict, separate template/style files, routing, Vitest); Playwright + ng-openapi installed but not configured yet; `GraderWeb/AGENTS.md` = the Angular team's best practices for agents (downloaded, not generated)

Check:

* `dotnet test` → 1 test passes
* `cd GraderWeb && npx ng test --watch=false` → 2 tests pass

Learnings:

* Use deterministic tools where they exist; do **not** let the LLM generate `.csproj`, `package.json`, `angular.json`, lock files, …
  * Faster, cheaper, reproducible, always the current template versions
* Use the agent to *generate calls* of deterministic tools if necessary (e.g. "write a script that creates …"), then review/run the script
* Gotchas found while preparing the script:
  * `dotnet new xunit` still defaults to xUnit **v2** → `--xunit-version v3`
  * v3 template configures MTP in `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`)
  * Packages that follow the .NET release train (`ApiDescription.Server`, `Mvc.Testing`) need `--prerelease` while .NET 11 is in RC
  * `ng new` creates its own Git repo unless `--skip-git`; `--ai-config=none` and `--interactive=false` avoid prompts
  * Instead of `--ai-config`, we download the Angular team's [best-practices.md](https://angular.dev/assets/context/best-practices.md) as `GraderWeb/AGENTS.md`: nested `AGENTS.md` files apply to their folder, so the Angular rules only load when the agent works in the frontend
  * `dotnet sln`/`dotnet new sln` creates `.slnx` (XML solution format) by default
  * .NET 10+ noun-first CLI syntax: `dotnet package add`, `dotnet reference add`

Links:

* [`dotnet new` templates](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-sdk-templates), [global.json](https://learn.microsoft.com/dotnet/core/tools/global-json)
* [`ng new` options](https://angular.dev/cli/new), [Zoneless Angular](https://angular.dev/guide/zoneless)
* [xUnit v3](https://xunit.net/docs/getting-started/v3/getting-started), [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
* [Generate OpenAPI documents at build time](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi), [Scalar](https://github.com/scalar/scalar)

---

## Step 2 – Give the agent access to up-to-date docs

Goal: .NET 11 and Angular 22 are newer than the model's training data → the agent must look up current docs.

Script (in `grader/`) – installs skills project-scoped for Claude Code, GitHub Copilot and Codex:

```bash
#!/usr/bin/env bash
# Step 2 – install docs skills (project scope) for Claude Code, GitHub Copilot and Codex
set -euo pipefail
AGENTS=(-a claude-code -a github-copilot -a codex -y)
npx -y skills@latest add https://github.com/microsoft/skills   --skill microsoft-docs    "${AGENTS[@]}"
npx -y skills@latest add https://github.com/upstash/context7   --skill find-docs         "${AGENTS[@]}"
npx -y skills@latest add https://github.com/angular/angular    --skill angular-developer "${AGENTS[@]}"
npx -y skills@latest add https://github.com/anthropics/skills  --skill skill-creator     "${AGENTS[@]}"
```

Result: skills in `.agents/skills/` (Copilot, Codex), symlinked into `.claude/skills/` (Claude Code), versions pinned in `skills-lock.json`.

Prompt:

```text
Create a brief AGENTS.md in the repository root. Content:

- One-paragraph overview: grading app for teachers and students; list the projects in this repo and their purpose.
- .NET, Angular and other technologies used here might be newer than you expect. Do not rely on your training data for APIs, verify with current docs.
- We use .NET 11 (currently a release candidate). NuGet packages that ship with .NET must match the SDK's major version, so prerelease versions are fine for them. Use the latest stable version for all other packages.
- Use the installed skills to research up-to-date documentation. Priority:
  1. Microsoft technologies incl. .NET, ASP.NET Core, xUnit: Microsoft Learn CLI (not MCP) from the microsoft-docs skill
  2. Angular: angular-developer skill
  3. Other technologies (incl. NSubstitute): ctx7 CLI (not MCP) from the find-docs skill
  4. You may clone/pull GitHub repositories of used open source libraries into ~/github/<repo> if you need to inspect their code or docs included in the repo.

Commit the changes (including installed skills) directly to main.
```

Rehearsal: ~30 s.

Learnings:

* Skills ([agentskills.io](https://agentskills.io)): folder with `SKILL.md` (name + description in front matter, instructions in body) + optional scripts/references
  * Only name + description are in context; body is loaded when relevant (progressive disclosure) → many skills are cheap
  * Open standard, supported by many agents; can also be invoked explicitly as slash commands (e.g. `/find-docs`)
* [skills.sh](https://skills.sh): directory + CLI (`npx skills add|list|update|remove`) for installing skills from GitHub repos
  * `-a` selects the agents, `-y` = non-interactive; project scope by default (`-g` for user scope)
  * Review skills before installing – they run with full agent permissions (skills.sh shows security assessments)
* Important docs skills:
  * `microsoft-docs` – Microsoft Learn (MCP server or `npx @microsoft/learn-cli search|fetch`)
  * `find-docs` – Context7 (`npx ctx7@latest library|docs`), docs for almost any open source library
  * `angular-developer` – official Angular team guidance (signals, signal forms, testing, …)
  * `skill-creator` – helps to write and evaluate your own skills
* CLI vs. MCP: CLIs need no server config, work in every agent and keep the context small
* [AGENTS.md](https://agents.md): "README for agents", loaded into context at session start → keep it brief
  * Claude Code reads `AGENTS.md` natively (built-in [agents-md](https://github.com/anthropics/claude-code/tree/main/mods/agents-md) plugin, `/config` → "Project instructions")
  * Default mode: `AGENTS.md` is loaded only if the project has **no** `CLAUDE.md` → don't add a `CLAUDE.md` next to it (or `@AGENTS.md`-import it)
* Gotcha: `microsoft-docs` prefers the Learn MCP server; our `AGENTS.md` overrides this with "CLI, not MCP"

Links:

* [agentskills.io](https://agentskills.io), [skills.sh](https://skills.sh), [agents.md](https://agents.md)
* [microsoft/skills](https://github.com/microsoft/skills), [Microsoft Learn MCP/CLI](https://learn.microsoft.com/training/support/mcp)
* [Context7](https://context7.com), [upstash/context7](https://github.com/upstash/context7)
* [Angular agent skills](https://angular.dev/ai/agent-skills), [anthropics/skills](https://github.com/anthropics/skills)
* Claude Code: [Skills](https://code.claude.com/docs/en/skills), [CLAUDE.md / memory](https://code.claude.com/docs/en/memory), [agents-md plugin](https://github.com/anthropics/claude-code/tree/main/mods/agents-md)

---

## Step 3 – Connect the projects

Goal: trivial content ("add two numbers") that touches every layer → proves that build, API, client generation, proxy, hosting and all test types work before we write real features.

Prompt:

```text
Goal: add super simple content to smoke-test the project setup end to end (nothing grading-specific yet).

- GraderLogic: Calculator class with an Add method behind an interface (for mocking). Negative parameters are not allowed → custom exception (to test error handling).
- GraderApi: expose Add via an HTTP endpoint with good OpenAPI docs (e.g. Produces for success and error). OpenAPI spec is written to a file at build time. Scalar API docs at /scalar.
- Angular ↔ API:
  - Development: Angular dev server proxies /api to the .NET backend.
  - Production: the built Angular app is served by the .NET backend.
- Angular API client generated with ng-openapi from the OpenAPI spec generated by the .NET build.
- Angular: remove the generated marketing content. Default route shows a simple signal-based form (two numbers, Calculate button) that calls the API and displays the result or the error.
- Tests, each with a regular and an error case:
  - Unit tests: Add method; Web API endpoint with mocked logic
  - Integration tests: Web API endpoint with real logic; GET of OpenAPI spec JSON and /scalar succeed (status only)
  - Angular component test with mocked API
  - Playwright e2e test of the full workflow
- Meaningful .gitignore (verify with git status).
- Tech docs in ./tech-docs: project-specific technical documentation, agents are the primary audience. Do NOT repeat code; whatever an agent can easily find out by looking at code does not belong there. Describe approach, principles, design decisions and gotchas, with pointers to important code (central classes/interfaces/files). Create:
  - testing.md: testing strategy, incl. commands to run the tests
  - api-client.md: API client generation strategy and usage, incl. commands to generate the client
- AGENTS.md: instruct agents to maintain the tech docs according to the principles above, add an overview of the tech docs articles (one line each), add commands for starting the app so agents do not have to guess.
- Verify that all tests pass and the project is in a clean state, then commit directly to main.
```

Rehearsal: 5–7.5 min in two runs, no struggles. The agent read the Angular skill references and looked up ng-openapi (ctx7) and build-time OpenAPI (Learn CLI) on its own.

What the agent built (both runs met all requirements; names, paths and details differ from run to run):

* Interface + implementation for `Add` + custom exception for negative parameters in GraderLogic
* `GET /api/.../add?a=&b=` → `{ result }` or `400` ProblemDetails; endpoint handler = public static method with typed results (`Results<Ok<T>, ProblemHttpResult>`) → unit-testable without HTTP, typed results feed OpenAPI
* OpenAPI spec written to a file in `GraderApi` on build; `npm run generate:api` (ng-openapi) generates the client into `GraderWeb/src/app/api`; spec + client are committed
* `ng serve` proxies `/api` → backend; `ng build` outputs into `GraderApi/wwwroot`, backend serves it incl. SPA fallback
* Calculator component with signal forms
* Tests: ~10 .NET unit + integration (`WebApplicationFactory<Program>`), 2–3 Vitest component, 2 Playwright e2e (against the production setup: Angular build served by GraderApi)
* `tech-docs/testing.md`, `tech-docs/api-client.md`, `AGENTS.md` with run commands + tech docs overview

Things to show/discuss:

* Walk through `tech-docs/*.md`: decisions + gotchas + pointers, no code copies
* Agent decisions worth reviewing: committed generated client (pro: frontend builds without .NET; con: must regenerate), strict JSON number handling (otherwise spec types numbers as `integer | string`)
* Gotchas the agent hit and documented (run 2): double `/api` (ng-openapi base path + spec paths), `<input type="number">` without `step="any"` blocks decimals (only the e2e test caught it)
* Non-determinism: same prompt, different names, paths, number types (`int` vs. `decimal`) and even policies (run 1 exposed `/scalar` in all environments, run 2 only in Development) → review decisions, write down the ones that matter (tech docs, prompt)
* Try it: `dotnet run --project GraderApi` + `cd GraderWeb && npm start` → http://localhost:4200, API docs at `/scalar` (backend URL, Development)

Learnings:

* Good test posture is essential: tests are the agent's feedback loop (and yours when reviewing agent code)
* Coding agents are very good at setting up test infrastructure (WebApplicationFactory, Vitest mocks, Playwright `webServer`, …) – boring for humans, cheap for agents
* Smoke-test content first: find setup problems (proxy, hosting, client generation) before they hide inside real features
* Skills vs. tech docs:
  * Skills = reusable across projects (framework knowledge, workflows, slash commands)
  * Tech docs = project-specific decisions, principles, gotchas; referenced from `AGENTS.md`, loaded on demand
* Keep `AGENTS.md` small: overview + commands + index of tech docs; details live in tech docs (progressive disclosure again)
* Give agents exact commands (start, test, generate) → no guessing, fewer wasted turns

Links:

* [ASP.NET Core OpenAPI](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview), [Using OpenAPI documents (Scalar)](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/using-openapi-documents)
* [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests), [NSubstitute](https://nsubstitute.github.io)
* [ng-openapi](https://ng-openapi.dev), [Angular dev server proxy](https://angular.dev/tools/cli/serve#proxying-to-a-backend-server)
* [Angular signal forms](https://angular.dev/guide/forms/signals/overview), [Angular testing](https://angular.dev/guide/testing), [Playwright](https://playwright.dev/docs/intro)

---

## Step 4 – QA baseline

Goal: deterministic tools (linters, formatters, analyzers, tests) give the agent feedback → agent fixes its own mistakes in a loop.

Prompt:

```text
Add a QA baseline that gives agents (and later CI) fast, deterministic feedback:

- Angular: Biome as linter and formatter (replaces Prettier).
- C#: .NET code analysis incl. IDE code-style rules (configured in .editorconfig, enforced on build), `dotnet format` as formatter, treat warnings as errors.
- Tests: .NET (unit, integration) and Angular (component, Playwright e2e).
- Helper script in ./scripts that runs the full QA baseline. Output must be easy to consume for an agent: clearly separated sections per tool, pass/fail per section, summary at the end, non-zero exit code on failure. Mention it in tech-docs/testing.md.
- AGENTS.md: at the end of each session that modified code, run the full QA script and fix all issues.
- Make the existing code compliant with the QA baseline.
- Prove that it works: temporarily add typical mistakes to C# and Angular code (lint, formatting, compiler warning), run the script, verify that all issues are detected, then revert them.
- Commit directly to main.
```

Rehearsal: 6–7 min per run, no struggles (agent looked up Biome via ctx7 and analyzer settings via Learn CLI).

What the agent built (details differ from run to run):

* `Directory.Build.props`: `AnalysisLevel` latest-recommended, `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`; rule severities + naming conventions in root `.editorconfig`
* `GraderWeb/biome.json` replaces Prettier; `npm run lint` / `npm run format`; Angular extended diagnostics (`extendedDiagnostics.defaultCategory: error`) turn template warnings into errors
* `scripts/qa.sh`: 7 checks (.NET format, .NET build + analyzers, .NET tests, Biome, Angular build, Vitest, Playwright), one section per check, tool output only on failure, dependent checks SKIPPED, summary table, exit code 1 on failure (run 1 also added full logs in `.qa-logs/` and `--skip-e2e`)
* `AGENTS.md`: "run `scripts/qa.sh` at the end of every session that modified code; fix code, don't suppress findings"; details in `tech-docs/testing.md`

Demo (works independently of the names the agent chose):

```bash
scripts/qa.sh                                                 # all green, ~15 s
printf 'using System.Text;\n\nnamespace GraderLogic;\n\npublic class Oops { public int M() { int unused = 1; return 42; } }\n' > GraderLogic/Oops.cs
echo 'var unused = "x";' >> GraderWeb/src/app/app.routes.ts   # unused variable
scripts/qa.sh                                                 # 3 checks FAIL, exit 1, file:line + rule IDs (IDE0005, CS0219, CA1822, noUnusedVariables, …)
git checkout -- . && rm GraderLogic/Oops.cs
```

Things to show/discuss:

* Output format designed for agents: `file(line,col): rule: message`, link to rule docs, only failing tools print output
  * Run 2's script repeats MSBuild errors and prints absolute paths → nice live follow-up prompt: "make the QA output more concise (deduplicate, relative paths)"
* Gotchas the agent found and documented in `testing.md`:
  * Biome's HTML support does not understand Angular control flow (`@if`, `@for`) → templates excluded from Biome, the Angular compiler (strict templates) checks them
  * Some Biome rules conflict with Angular (`useImportType` breaks DI) → disabled with a reason
  * IDE0005 (unnecessary using) needs `GenerateDocumentationFile` → XML doc comments now also flow into OpenAPI
* Agent rule: "fix code, don't suppress findings; change rules only with documented reason" – otherwise agents tend to "fix" by disabling
* Rehearsal note: without "directly to main" the agent once created a feature branch (Claude Code's default git behavior)

Learnings:

* Deterministic tools → feedback → agent fixes → repeat: the loop makes agent output reliable
* Warnings as errors: agents ignore warnings, they don't ignore a failing build
* Make important commands easily accessible (helper scripts): agents don't guess, humans use the same script, CI can reuse it
* Let the agent prove that the QA baseline detects mistakes (tests for your tests)

Links:

* [.NET code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview), [`dotnet format`](https://learn.microsoft.com/dotnet/core/tools/dotnet-format)
* [Biome](https://biomejs.dev)

---

## Step 5 – Database (spec-driven)

Goal: SQLite + EF Core data model for the grading domain. The requirements come from a **spec file**, not from the prompt: the prompt says *what to do*, the spec says *what the data looks like*.

Preparation: copy the spec from the workshop materials (next to this storyboard) into the repo:

```bash
mkdir -p spec && cp <workshop-materials>/2026-09-storyboard_erdiagram.md spec/
```

Walk through the spec (`spec/2026-09-storyboard_erdiagram.md`):

* Mermaid ER diagram (renders on GitHub/VS Code, readable for humans *and* agents): 6 entities, types, lengths, keys
* Constraints and rules in plain text: unique combinations, no cascading deletes, percentage 0–100, "all constraints must be enforced by the database"
* Test data generator: school years relative to today, classes per level, 20 Bogus students per class, 5 courses, 4 exams per course/class, normally distributed percentages, 2 missing results per exam
* Written *before* the agent session, reviewed like code (it took several review rounds to remove contradictions, e.g. 0–1 vs. 0–100, results for future exams yes/no)

Prompt:

```text
Add a database. The data model is specified in ./spec/2026-09-storyboard_erdiagram.md.

- SQLite with EF Core. The database file lives in ./data (relative to the repo root). Keep the folder in git, but not its content.
- Add Microsoft.EntityFrameworkCore.Sqlite and Microsoft.EntityFrameworkCore.Design to GraderLogic with `dotnet package add`.
- Data model and DbContext in GraderLogic according to the spec.
- Generating and applying migrations must work from the GraderLogic project.
- Test data generator (see spec) in a separate class. An integration test runs it and verifies the row counts.
- Exemplary integration tests for database constraints:
  - Adding a duplicate SchoolYear.Name fails.
  - Deleting a SchoolYear that has SchoolClass records fails.
  - Adding an ExamResult with a Percentage of 101 fails.
- AGENTS.md: ./spec contains specifications. They are historical and not always kept up to date, so they might not reflect the current state of the system if they were written a while ago. If a spec contradicts code or tech docs, code and tech docs win; ask if unsure.
- New tech docs article tech-docs/database.md (follow the tech docs rules in AGENTS.md).
- Done when the QA script is green. Then commit directly to main.
```

Rehearsal: ~5 min, no struggles, no questions (the spec has no contradictions). The agent checked the EF Core 11 breaking changes (Learn CLI) and added the EF Core packages and `dotnet-ef` with `--prerelease` (thanks to the .NET 11 note in `AGENTS.md` from step 2).

What the agent built (details differ from run to run):

* `GraderLogic/Data/`: one class per entity, `GraderDbContext` (all constraints in `OnModelCreating`), `IDesignTimeDbContextFactory` → `dotnet ef` works with GraderLogic as startup project, database path = `data/grader.db` in the repo root
* Initial migration in `GraderLogic/Migrations/`; `data/.gitkeep` + `.gitignore` for `data/*`
* Database-enforced constraints: unique indexes, `ON DELETE RESTRICT` on every FK, check constraint for `Percentage` – and check constraints for max lengths (SQLite ignores `TEXT` lengths)
* `TestDataGenerator` (Bogus, `TimeProvider` for "current year", optional seed), throws on non-empty database
* Tests in `GraderTests/Integration/Data/` against in-memory SQLite with the real migrations: exact row counts (3 school years, 30 classes, 600 students, 5 courses, 600 exams, 10,800 results), distribution sanity check, non-empty DB throws, the three constraint tests (asserting the SQLite error code)
* Local tool manifest `dotnet-tools.json` with `dotnet-ef` 11 RC; `tech-docs/database.md`; `AGENTS.md` with spec note + first-time setup `dotnet tool restore`

Demo:

```bash
dotnet tool restore
cd GraderLogic
dotnet ef migrations has-pending-model-changes   # "No changes have been made to the model since the last migration."
dotnet ef database update                          # creates ../data/grader.db
sqlite3 ../data/grader.db ".schema ExamResults"    # CHECK + ON DELETE RESTRICT in the DB schema
cd .. && git status                                # clean: database file is ignored
```

Things to show/discuss:

* Spec → non-obvious decisions: "enforced by the database" made the agent add `length(...) <= n` checks because SQLite ignores declared lengths
* Tests prove that the **database** rejects the change: violating changes are saved with a context that does not track the conflicting rows (otherwise EF Core throws before sending SQL); asserts SQLite error codes (RESTRICT is reported as 1811 `SQLITE_CONSTRAINT_TRIGGER`, not 787 – agent found out by running the test)
* Gotchas the agent found and documented:
  * Global `dotnet-ef` (10.x) does not match EF Core 11 → local tool manifest
  * Generated migrations violate analyzers → `generated_code = true` for `GraderLogic/Migrations/**.cs` in `.editorconfig` (documented reason, see step 4 rule)
* Deterministic test data: exact counts because the spec fixes all numbers; `TimeProvider` + seed make the rest reproducible
* `AGENTS.md` spec note: "specs are historical, code and tech docs win, ask if unsure" – the agent did not ask because the spec is consistent (educational: in interactive sessions the agent stops and asks)

Learnings:

* Prepare agentic tasks with artifacts, not just prompts: spec = *what* (domain, rules, data), prompt = *task* (scope, tools, done criteria), tech docs = *how* (written by the agent, maintained with the code)
* Review specs like code: contradictions in the spec become random behavior of the agent
* Specs age: code and tech docs are the source of truth; say so in `AGENTS.md` and let the agent ask if unsure
* "Enforced by the database" + exemplary tests → agent tests constraints against the real schema, not against EF Core
* Discuss: keep specs after implementation? Where (`spec/` in the repo, wiki, issue tracker)? Date-prefixed file names show their age

Links:

* [EF Core SQLite provider](https://learn.microsoft.com/ef/core/providers/sqlite/), [SQLite limitations](https://learn.microsoft.com/ef/core/providers/sqlite/limitations)
* [Migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/), [`dotnet ef` CLI](https://learn.microsoft.com/ef/core/cli/dotnet), [Design-time DbContext creation](https://learn.microsoft.com/ef/core/cli/dbcontext-creation)
* [EF Core 11 breaking changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/breaking-changes), [.NET local tools](https://learn.microsoft.com/dotnet/core/tools/local-tools-how-to-use)
* [Mermaid ER diagrams](https://mermaid.js.org/syntax/entityRelationshipDiagram.html), [Bogus](https://github.com/bchavez/Bogus), [SQLite result codes](https://sqlite.org/rescode.html)

---

## Step 6 – Web API (architecture first)

Goal: the first real use cases as a Web API. This time **we write the architecture first**: a tech docs article describes *how* we build Web APIs in this project, the prompt only describes the *task*.

Preparation: copy the architecture article from the workshop materials (next to this storyboard) into the repo:

```bash
cp <workshop-materials>/web-api.md tech-docs/
```

Walk through the article (`tech-docs/web-api.md`) – principles and patterns only, no concrete endpoints:

* Use cases, not tables: no generic CRUD, endpoints grouped by use-case area, response shapes tailored to the use case, server decides sort order
* Layers: thin endpoints → use-case service interfaces in GraderLogic (injected, mockable) → `DbContext`
* Data access: entities never leave GraderLogic, reads project into records, one request = one transaction; pattern for saving a parent with all its children (`PUT` with the complete document)
* Errors: services translate DB errors into a few meaningful exceptions with stable error codes → central handler → Problem Details with a `code` member
* Validation at the API boundary, HTTP + OpenAPI conventions (tags = use-case areas, operation names = use cases)
* Dates without time stay ISO strings (`yyyy-MM-dd`) in every client, incl. the generated Angular client – a JavaScript `Date` is midnight UTC and can show the previous day. We added this rule after a rehearsal: step 3 had configured the generator with `Date`, and the mismatch only surfaced when the exams UI (step 7b) showed dates
* "Deliberately not done": no PATCH/paging/OData, no generic repository/unit of work, no optimistic concurrency, no auth yet
* Why no repository/unit of work? The `DbContext` already is both; a generic `IRepository<T>` leads back to table thinking. The use-case service interface is the seam for mocking. Agents like to generate repositories (training data) → decide it explicitly.

Prompt:

```text
Implement the first Web API use cases. Follow the architecture in tech-docs/web-api.md.

Use cases:
- Class management:
  - Pick a school year (newest first).
  - List the classes of a school year (by name, incl. number of students).
  - Open a class incl. its students (by last name, first name).
  - Create a class incl. its students.
  - Save a class incl. its students. The full student list is always sent (users maintain all rows in one UI, later maybe via Excel upload). Removing a student who has exam results fails.
  - Delete a class incl. its students. Fails if the class has exams.
- Exams:
  - Pick a course (by name).
  - List the exams of a class (by date, incl. course).
  - Open an exam incl. the results of all students of the class (by last name, first name; no result = null).
  - Create an exam incl. its results.
  - Save an exam incl. its results. Date, course and the full result list are always sent (users enter all results of an exam in one UI). The class of an exam cannot be changed. Results only for students of the exam's class.
  - Delete an exam incl. its results.
- Demo data: one endpoint that empties the database and fills it with demo data using the test data generator (will be a button in the UI later).

Requirements:
- No UI yet (next step).
- SchoolYear and Course have no write API (a CLI and agent support will follow later).
- GraderApi uses the SQLite database from the previous step.
- Exemplary tests, each with a regular and an error case:
  - Unit tests: one data access helper (e.g. translating a DB error into an exception); one endpoint with a mocked use-case service.
  - Integration tests: saving a class with added, changed and removed students against SQLite; one endpoint end to end incl. the error `code` in the Problem Details.
- You may extend, update and refine tech-docs/web-api.md (follow the tech docs rules in AGENTS.md). If you need to deviate from its principles, ask first.
- Add web-api.md to the tech docs index in AGENTS.md.
- Done when the QA script is green. Then commit directly to main.
```

Rehearsal: ~11 min, no questions, no struggles – the longest step so far (12 use cases + demo data). The agent verified every endpoint by hand against a temporary database, found a runtime-only query problem (see gotchas) and fixed it.

What the agent built (details differ from run to run):

* `GraderLogic/<Area>/`: `IClassManagementService`, `IExamService`, `IDemoDataService` + implementations and request/response records; `GraderLogic/Errors/`: `GraderException` base with `NotFound`/`BusinessRule`/`Conflict` exceptions, all codes in one `ErrorCodes` class; `Data/DatabaseErrors.cs`: `SaveChangesTranslatingErrorsAsync` turns constraint violations into `constraint-violation` conflicts
* `GraderApi/<Area>Endpoints.cs` (thin, public static handlers, typed results), `GraderExceptionHandler` (`IExceptionHandler` → Problem Details with `code`), validation errors get `code: validation-failed`
* 13 endpoints in 3 tags (`ClassManagement`, `Exams`, `DemoData`), e.g. `PUT /api/classes/{classId}`, `PUT /api/exams/{examId}`, `POST /api/demo-data`; `GraderApi.http` with example requests
* GraderApi uses `data/grader.db` and applies migrations at startup (skipped during build-time OpenAPI generation); API integration tests now run against in-memory SQLite (`GraderApiFactory`)
* Exemplary tests: DB error translation (unit), endpoint with mocked service (unit), saving a class with added/changed/removed students against SQLite (integration), create class end to end incl. `code` (integration)
* `tech-docs/web-api.md` extended by the agent; `AGENTS.md` index, `database.md`, `testing.md` updated

Demo:

```bash
git diff HEAD~1 -- tech-docs/web-api.md     # what the agent added to our article
dotnet run --project GraderApi              # applies migrations, creates data/grader.db if needed
```

In Scalar (`/scalar` on the backend URL) or `GraderApi/GraderApi.http`:

1. `POST /api/demo-data` → 204, database filled
2. `GET /api/school-years` → newest first; classes of a school year incl. student count; a class incl. its students
3. `GET` an exam → all students of the class, `percentage: null` for missing results
4. `PUT` the class without one of its students → 409 `student-has-exam-results` (whole request rejected)
5. `DELETE` a class that has exams → 409 `class-has-exams`; `POST` a class with an empty name → 400 with field errors + `code: validation-failed`

Things to show/discuss:

* The article as a living document: our principles are unchanged, the agent added pointers, a use-case area table, examples to copy for tests and gotchas
* Architecture followed: OpenAPI schemas contain only DTOs (`ClassDetails`, `ExamRequest`, …), no entity types; tags + operation names (`SaveClass`, `GetExam`) → generated Angular services `classManagement.service.ts`, `exams.service.ts`, … read like use cases
* Agent decisions worth reviewing (all documented in `web-api.md`):
  * Unknown id in the route → 404, unknown id in the body (e.g. `courseId`) → 400
  * Exam results: `null` percentage and missing entry both mean "no result" → client can send back what it got from `GET`
  * Write endpoints return the saved document via the service's own read method → `PUT`/`POST` and `GET` responses cannot drift apart
  * Migrations at startup: convenient for the demo; discuss for production
* Gotchas the agent found and documented:
  * Request records live in GraderLogic, but the validation source generator only covers the assembly that calls `AddValidation()` → without `[ValidatableType]` requests are **silently not validated**
  * SQLite cannot translate some nested collection projections (SQL `APPLY`) → fails at runtime, not at build time; found only because the agent called every endpoint
  * SQLite sorts with binary collation: umlauts after `z` ("Özdemir" after "Zach") → documented, not fixed; good live follow-up prompt
* The prompt contains nothing about layers, error handling or conventions – and the result still follows them

Learnings:

* Architecture first: humans write principles and patterns into tech docs *before* the agent session; the agent follows them and keeps the article up to date
* Three artifacts, three jobs: spec = *what* (domain, data; step 5), tech docs = *how* (patterns, principles), prompt = *task* (scope, done criteria)
* The prompt gets shorter: layering, error handling, API conventions apply to every future feature without repeating them
* Agents default to generic CRUD, `DbContext` in endpoints and repositories (that's what their training data looks like) → an explicit architecture overrides the default
* Explicit non-goals ("deliberately not done") stop agents from over-engineering
* Stable error codes make errors testable and client-friendly
* Exemplary tests are enough to set the pattern; the article points to them as "examples to copy"
* Discuss: How detailed should architecture docs be? Tech docs vs. ADRs? Who writes them – human, agent, or agent drafts + human reviews?

Links:

* [Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api), [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457)
* [Validation in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/validation) (incl. multi-assembly apps), [Typed results](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses), [Route handlers and groups](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers)
* [OpenAPI metadata in minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata)
* [EF Core testing strategies (incl. repository pattern)](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy), [Unit of Work (Fowler)](https://martinfowler.com/eaaCatalog/unitOfWork.html)
* [Applying migrations (incl. at runtime)](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying), [Collations and case sensitivity](https://learn.microsoft.com/ef/core/miscellaneous/collations-and-case-sensitivity)
* [Architecture decision records](https://adr.github.io)

---

## Step 7 – UI from design input

Goal: the web UI for the use cases of step 6, built from a **designer handoff** (mockups + readme). Two prompts: 7a builds the UI foundation (design system, base components/services, tests, docs) plus the first use case area; 7b shows how short a follow-up prompt becomes once that foundation exists.

Preparation: copy the design from the workshop materials (next to this storyboard) into the repo. Browser for the agent: zoom **100%**, window at least 1920 px wide (see prerequisites).

```bash
mkdir -p spec && cp -r <workshop-materials>/design spec/
```

Walk through the design (`spec/design/`):

* `00-style-guide.png` + 5 mockups (class grid, class editor with validation errors, exams with results entry, loading state, dark mode); numbered annotations explain behavior, not just looks (save as one document, new rows on top, empty = no result, skeleton instead of spinner, …)
* `readme.md` = the same design as **text**: color tokens as CSS variables (light + dark), typography, spacing, components, interaction states. Exact values beat reading pixels, and the readme wins if a mockup disagrees
* Deliberately simple: desktop only (Full HD), native controls (`<select>`, `<input type="date">`), radius 0, no shadows, one icon
* Not every screen is designed (e.g. settings, confirmation dialog, empty states) → the prompt names the important gaps, the agent extrapolates from the design system

Prompt 7a (UI foundation + class management):

```text
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
```

Rehearsal 7a: ~20 min (Claude Code with `--chrome`), no questions, no real struggles – the time goes into scope, not detours. Rough split: 2 min researching current APIs (signal forms, icon package, Playwright setup order), 6–7 min writing the design system, base services, shell and class editor, 4 min in the browser comparing with the mockups, 2 min tests, the rest QA, docs and commit.

What the agent built in 7a (details differ from run to run):

* Calculator gone completely (UI, endpoint, logic, tests); `generate:api` now clears `src/app/api` first (ng-openapi does not delete files of removed services)
* `src/styles/`: `tokens.css` (all colors, type, spacing as CSS variables; dark theme = `:root[data-theme='dark']` swaps only color tokens), global classes for typography, buttons, inputs, tables, skeletons, layout
* `core/` base services: `SchoolYearService` (global context, newest by default), `ThemeService` (light default, `localStorage`, applied before Angular boots → no flash), `ConfirmService`, `NotificationService`, `api-errors.ts` (error `code` → user message, status 0 → "server cannot be reached", 5xx → generic)
* `ui/` base components: `ConfirmDialog` (native `<dialog>`), `NotificationBar`, `LoadError` (with "Try again"), `FieldError` + a directive that sets `aria-invalid` (signal forms don't)
* Classes: grid with skeleton while loading (switching the year cancels the pending request), editor with signal forms (new rows on top with focus, local remove, full-document save, "n fields need attention", focus on the first invalid field, server errors in place of the count, "name taken" at the field), delete with confirmation; Settings (theme toggle, reset demo data); Exams placeholder
* Tests: 15 component tests (confirm dialog, class editor, error messages) with the generated `ClassManagementService` mocked; 3 Playwright tests against a temporary database (`<tmpdir>/grader-e2e.db`, deleted before each run, filled by a Playwright *setup project* via `POST /api/demo-data`; every test creates its own class → independent)
* `tech-docs/ui.md` (tokens, global classes, base services, where errors are shown, page patterns, gotchas); `testing.md`, `api-client.md` updated

Prompt 7b (Exams):

```text
Implement the Exams area of the UI (the Exams endpoints of the Web API; mockups 03 and 05 in spec/design/). Reuse the design system and base components; extend them where needed.

- Exemplary component tests with a mocked API: the percentage field; entering results (save sends all results, a value out of range blocks saving).
- Playwright e2e test: enter the results of an exam.
- Inspect the result in a real browser (e.g. Claude in Chrome) at 1920×1080, in light and dark mode. Compare it with the mockups and fix the differences.
- Keep tech-docs/ui.md up to date.
- Done when the QA script is green. Then commit directly to main.
```

Rehearsal 7b: ~10 min (half of 7a) with a prompt of five lines – the agent read `ui.md` and reused tokens, classes, error handling, confirm dialog and the field error helpers.

What the agent built in 7b (details differ from run to run):

* Exams page: 400px list column (class filter, "New exam", exams by date, selected row highlighted) | editor (date, course, results in two columns, "n of m entered"); opening an exam by URL selects its class (and school year); a new exam starts with today and "No result" for every student
* `ui/percentage-field/` (0–100, decimals, empty = `null`); the "n fields need attention" count became a shared helper, now also used by the class editor; messages for the exam error codes
* Tests: percentage field + exam editor (save sends all results incl. `null`, 104 blocks saving and gets the focus); Playwright: open an exam, 104 is rejected, enter 67.5, save, still there after reload
* Dates as strings end to end (rule in `web-api.md`, see step 6); without that rule the rehearsal agent found the mismatch only here: the client generator from step 3 typed `DateOnly` as `Date`, but the API sends `yyyy-MM-dd` strings

Demo:

```bash
dotnet run --project GraderApi              # API on :5100
cd GraderWeb && npm start                   # http://localhost:4200
```

1. Settings → Reset demo data (confirmation dialog) → Classes: grid for the newest school year; switch the year → skeleton
2. Open a class: add a student (new row on top, focus in last name), clear a first name, Save → errors at the fields + count, focus on the first invalid field
3. Remove a student who has exam results, Save → server rejection in place of the error count
4. Exams: pick a class and an exam, enter 104 → "Must be 0–100"; Settings → dark mode; compare with `spec/design/05-dark-mode.png`
5. `git show --stat HEAD~1` → size of 7a vs. 7b; `tech-docs/ui.md`

Things to show/discuss:

* Design readme vs. PNGs: the agent took exact values from the text and used the images for layout and behavior; it compared screenshots with the mockups and fixed real differences (a heading-size count, row heights 53 → 49px, a 2px taller date input)
* The gaps were filled consistently with the design system: `ui.md` has a table "where each kind of error is shown"; the confirmation dialog is the native `<dialog>` (focus trap, Escape for free)
* Agent decisions worth reviewing:
  * `rxResource` with the generated (Observable-based) client instead of `httpResource` – what does "use resources" mean when the client is RxJS-based? (ng-openapi also has an httpResource plugin)
  * `lucide-angular` only supports Angular ≤ 21 → official successor `@lucide/angular` (the design readme has been corrected since)
  * Biome's `useValidAnchor` switched off for templates (`routerLink` sets `href` at runtime), reason documented in `testing.md`
  * Scope beyond the prompt: more tests than asked (3 e2e tests in 7a), notification bar, "Try again" state – good or too much?
* Gotchas the agent found and documented: signal forms don't set `aria-invalid`; jsdom has no `showModal()`; array rows in signal forms keep their state only if the item objects are reused; `npm test` regenerates the client and confuses a running `ng serve`; native date/number inputs follow the browser language ("75,5" in a German browser) even without i18n
* Browser in the loop: the agent resized, measured element positions, toggled dark mode, delayed `fetch` to see the skeleton and simulated a network failure – verification a screenshot test would not do
* Cost: 7a is the most expensive step so far (~$9 in the rehearsal, 7b ~$4)

Learnings:

* Agents can deal with design input: images for layout and behavior, a text readme for exact tokens and rules
* Design system first: reusable tokens, classes, components and services make the follow-up prompt short (7b: 5 lines, half the time)
* Designs are never complete → name the important gaps in the prompt and let the agent extrapolate from the design system
* Close the loop visually: the agent inspects its own UI in a real browser and compares it with the mockups (Claude in Chrome, Playwright MCP, …)
* Testable by design: generated client injected → mocked in component tests; e2e against a throwaway database filled by the existing demo data generator
* Discuss: Where do designs live (Figma, PNGs, text)? Who keeps design and code in sync? Visual regression tests vs. agent-in-the-browser?

Links:

* [Angular signal forms](https://angular.dev/guide/forms/signals/overview), [Resources](https://angular.dev/guide/signals/resource), [RxJS interop (`rxResource`)](https://angular.dev/ecosystem/rxjs-interop)
* [Claude in Chrome](https://code.claude.com/docs/en/chrome)
* [Playwright project dependencies (setup)](https://playwright.dev/docs/test-global-setup-teardown), [Playwright web server](https://playwright.dev/docs/test-webserver)
* [`<dialog>` element](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/dialog), [Fontsource Archivo](https://fontsource.org/fonts/archivo), [`@lucide/angular`](https://www.npmjs.com/package/@lucide/angular)


## Step 8 – CLI + skill: make the app usable for agents

Goal: a **command-line interface** for the existing application plus a **skill** that teaches agents how to use it. Afterwards, an agent does tasks a teacher would otherwise click through in the UI – in seconds and for cents. Four prompts: 8a builds the CLI, 8b creates the skill (with the `skill-creator` skill from step 2), 8c and 8d are **no coding prompts** but requests of a teacher.

Why a CLI? In step 7 the agent operated the UI through the browser: slow, expensive, fragile. A CLI is the cheapest interface for agents: discoverable (`--help`), scriptable, composable (JSON → `jq`/Python), and humans can use it too. The skill adds what `--help` can't: which workflows exist, in which order, and where the sharp edges are.

Design decisions to discuss before the prompt:

* **Generated client (Kiota) instead of reusing the DTOs from `GraderLogic`:** the DTOs live next to the `DbContext`; referencing them would pull EF Core, SQLite (incl. native binaries) and Bogus into the tool – and tempt an agent to "simplify" by calling the services or the database directly, bypassing the API. The OpenAPI spec is the contract; after the Angular client (step 3) it now feeds a second, generated client
* **Mirror the API:** one command per endpoint, but with short command names that are nice to use for humans too; ids as arguments, saving takes the complete document (same parent/child semantics as the API, see `web-api.md`), no convenience commands – the skill teaches the workflows instead
* **Generated client committed**, regenerating it is a dedicated step (not on every build) → rule in AGENTS.md: every structural API change includes regenerating the CLI client
* **.NET global tool:** easy – three csproj properties (`PackAsTool`, `ToolCommandName`, `PackageId`), `dotnet pack`, `dotnet tool install -g --add-source <folder>`; no code changes. Since .NET 10, `dnx <package>` runs a tool without installing it

Prompt 8a (CLI):

```text
Add a command-line interface (CLI) for Grader: a new .NET console project GraderCli that mirrors the Web API. Its main users are AI agents doing tasks for a teacher; humans use it too.

- One command per Web API use case, with short command names that are nice to use for humans too (e.g. `grader classes get 7`). Mirror the API: ids as arguments, saving takes the complete document as JSON (file or stdin, same shape as the API request), no convenience commands.
- Call the API through a client generated from the OpenAPI spec with Kiota; don't reference GraderLogic. The generated code is committed and regenerated only as a dedicated step, not on every build: add a rule to AGENTS.md that every structural API change includes regenerating the CLI client (and how).
- API base URL via option and environment variable, default: the local development API.
- Agent-friendly: JSON output besides readable text, errors on stderr incl. the API's error code, meaningful exit codes, never prompt (destructive commands require --yes), --help with examples.
- Package it as a .NET global tool (command `grader`).
- Exemplary tests: a command running against the Web API in memory; an API error becoming an error message and exit code.
- New tech docs article tech-docs/cli.md (incl. building, installing and updating the tool); update AGENTS.md, testing.md and api-client.md.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
```

Rehearsal 8a: ~14 min, no questions. This rehearsal ran with the old last line ("Done when … works against the running API …"), so the agent also checked everything against a live API on its own; the prompt now asks for tests + QA only to save stage time, and the numbers will change with the next rehearsal. Most of the time went into research (Kiota with the OpenAPI 3.2 spec, the System.CommandLine release matching the .NET 11 SDK, its help customization) and into trying every command against a real API on a temporary database.

What the agent built in 8a (details differ from run to run):

* `GraderCli` (System.CommandLine 3.0 RC – ships with .NET, so the AGENTS.md version rule picked the RC matching the SDK) with command groups = OpenAPI tags, commands = operation names: `grader class-management get-class 7`, `grader exams save-exam 12 --input exam.json`, `grader demo-data reset-demo-data --yes`. That rehearsal ran with a prompt that did not ask for short names – consistent and predictable for agents, but clumsy for humans. The prompt now asks for short names; the command names in this section and in the demo below are from that run and will change with the next rehearsal
* `--json` (results on stdout, errors on stderr, both JSON), error `code` from the Problem Details, exit codes 0–6 (2 = invalid input/missing `--yes`, nothing sent; 3/4/5 = HTTP 400/404/409; 6 = API not reachable), `--help` with examples and the exit codes; `--api-url` / `GRADER_API_URL` / default `http://localhost:5100`
* Kiota client in `GraderCli/ApiClient/` (committed, marked as generated code in `.editorconfig`); `scripts/generate-cli-client.sh` regenerates it, and with `--check` it is a **new QA step** that fails if the client is out of date – the agent turned the AGENTS.md rule into a check
* AGENTS.md: new section "API Changes" (both generated clients must follow every API change in the same commit; how to regenerate the CLI client; mirror new endpoints as commands)
* `scripts/install-cli.sh`: packs and installs/updates the global tool
* 2 tests: CLI in-process against the Web API in memory (`WebApplicationFactory`'s `HttpClient` passed to the Kiota adapter), API error → error code + exit code
* `tech-docs/cli.md` (principles, exit codes, structure, adding a command, Kiota, installing, testing, gotchas); `api-client.md`, `testing.md` updated

Prompt 8b (skill):

```text
Use the skill-creator skill to create a project skill `grader-cli` (in .agents/skills/, linked into .claude/skills/ like the other skills) that teaches agents to do a teacher's everyday tasks in Grader with the CLI: e.g. setting up a class from a list of names, recording exam results, answering questions about grades.

- Focus on workflows and pitfalls, don't repeat --help (e.g. finding ids, saving complete documents, the API must be running, destructive commands only when the teacher asks for them).
- Keep the evaluation light: 2–3 realistic test tasks against the running development API with demo data, no benchmark or review viewer.
- Done when the QA script is green. Then commit directly to main.
```

Before 8b: start the API in a second terminal (`dotnet run --project GraderApi`) and reset the demo data (Settings in the UI, or `grader demo-data reset-demo-data --yes`).

Rehearsal 8b: ~5 min. The agent followed the skill-creator process in a light version: explored the CLI hands-on, wrote the draft, then ran 3 test tasks **with and without the skill in parallel subagents** – the baselines against a second API instance on a copy of the database, so they could not interfere. Both variants solved all tasks; with the skill the agents were faster and needed fewer commands (without it they guessed commands like `grader classes --help`). One finding changed the skill: it had said "one decimal is plenty" and the agent rounded 98.75 % to 98.8 % → now "store the exact value". Afterwards the agent cleaned up its test data in the development database.

What the agent built in 8b:

* `.agents/skills/grader-cli/SKILL.md` (~100 lines) + link in `.claude/skills/`; a "pushy" description (triggers on "a teacher pastes a list of names or scores", even without mentioning the CLI)
* Content: find the CLI and check the API first; `--json` for the agent, tables for the teacher; ids only via navigation (school year → class → exam, never from memory – ids change with `reset-demo-data`); **complete-document saves: get → edit → save, compare before saving**; destructive commands only on explicit request, never delete to get around a rule; workflows (class from a list of names incl. name formats and duplicates, recording results incl. points → percent and "absent = no result, not 0", answering questions: percentages, not grades); table of error codes → what to do
* `tech-docs/cli.md`: when the skill must be updated

Prompt 8c (a teacher's request – run it in a **new session**):

```text
I'm taking over a new class this school year, 1D. Students: Lena Huber, Jonas Gruber, Sophie Wagner, Elias Bauer, Mia Steiner, Felix Moser, Hannah Hofer, Paul Leitner.

They wrote their first Mathematics test on 22 September 2026: Huber 78 %, Gruber 91.5 %, Wagner 64 %, Bauer 45 %, Steiner 88 %, Moser 72 %, Leitner 55 %. Hannah was sick.

Please enter all of this in Grader.
```

Rehearsal 8c: 36 s, $0.31. The agent loaded the `grader-cli` skill by itself, checked the school year and that 1D is free, created the class, mapped the names to the new student ids and created the exam – Hannah without a result instead of 0 %.

Prompt 8d (a question, new session):

```text
Which three students had the lowest average result in Mathematics in school year 2025/26? Show their class and average.
```

Rehearsal 8d: 1 min, $0.32. The agent fetched all Mathematics exams of the 10 classes of 2025/26 (`--json` into a temp folder), computed the averages with Python and answered with a table – incl. how many results each average is based on and the hint that one student only has 2 of 4 results (the skill's "say how many results an average is based on"). Checked against the database: correct.

Demo:

```bash
dotnet run --project GraderApi                       # terminal 1: API on :5100
scripts/install-cli.sh                               # terminal 2: pack + install the global tool
grader --help                                        # groups, examples, exit codes
grader class-management get-school-years            # ids change with every demo data reset
grader class-management get-classes <year-id>
grader class-management get-class <class-id> --json | jq '.students | length'
grader exams delete-exam 1; echo $?                  # confirmation-required, exit code 2
grader class-management get-class 99999; echo $?     # class-not-found, exit code 4
```

1. `grader --help`, then the same with `dnx`: `dnx GraderCli --add-source GraderCli/nupkg --prerelease --yes -- --help` (runs without installing; the package has a `-dev` prerelease version)
2. `tech-docs/cli.md` + the "API Changes" rule in AGENTS.md; `scripts/generate-cli-client.sh --check` in the QA script
3. `.agents/skills/grader-cli/SKILL.md`: what's in there that `--help` doesn't say
4. New session → prompt 8c → check class 1D in the UI (step 7)
5. New session → prompt 8d
6. Variation: paste a photo of a handwritten grade sheet instead of the text list

Things to show/discuss:

* **CLI + skill vs. MCP server:** the CLI costs no tokens until it is used, its output can be piped and filtered before it reaches the context (8d: 40 exams went through Python, only the result reached the model), and humans and scripts use the same tool. MCP shines where there is no shell (e.g. claude.ai, mobile) or for remote/authenticated access
* Command names: without a hint, the agent derived them 1:1 from tag + operation name (`grader class-management get-class`, `grader demo-data reset-demo-data`) – agents don't care, humans do. One line in the prompt fixes it; a CLI serves both audiences
* Gotchas the agent found and documented:
  * Kiota's JSON parser silently **drops values of the wrong type** (`"students": "abc"` became an empty list and the class was created!) → the CLI serializes the parsed input again and rejects it with the JSON path if anything got lost
  * `dotnet tool update` does not reinstall an installed version, and NuGet caches packages by version → `install-cli.sh` packs a unique `-dev.<timestamp>` version every time
  * Kiota can't see the `code` extension member of Problem Details (not in the schema) → read from `AdditionalData`
  * `save-*` does not require `--yes` although it removes what's missing from the document – the API contract; the skill covers it ("compare before saving")
* Permissions: allow `Bash(grader:*)` (or `Bash(dotnet run --project GraderCli:*)`) in `.claude/settings.json`, and the agent works without prompts – but mind destructive commands
* The skill's evaluation ran in parallel subagents against a **copy** of the database – the agent protected the development data without being told
* Cost: 8a ~$4.50, 8b ~$3.50, 8c/8d ~$0.30 each – the "using" prompts are cheap because the investment was made in 8a/8b

Learnings:

* A CLI is the cheapest agent interface for an existing app: discoverable, scriptable, composable – and useful for humans too
* Skill = the knowledge the CLI can't give: workflows, order of steps, pitfalls. Write it with skill-creator and test it with realistic tasks (with vs. without skill)
* Contract first pays off again: the OpenAPI spec feeds a second generated client; a QA check keeps it in sync
* .NET global tools make distribution easy; `dnx` runs them without installing
* The shift: from "agent writes code" to "agent uses the app" – the same agent is developer (8a/8b) and user (8c/8d)

Links:

* [Kiota overview](https://learn.microsoft.com/openapi/kiota/overview), [Kiota .NET quickstart](https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet)
* [System.CommandLine](https://learn.microsoft.com/dotnet/standard/commandline/)
* [.NET tools](https://learn.microsoft.com/dotnet/core/tools/global-tools), [Create a .NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools-how-to-create), [`dotnet tool exec` / `dnx`](https://learn.microsoft.com/dotnet/core/tools/dotnet-tool-exec)
* [skill-creator](https://github.com/anthropics/skills/tree/main/skills/skill-creator), [Claude Code skills](https://code.claude.com/docs/en/skills)

## Step 9 – stdio MCP server in the CLI

Goal: `grader mcp` turns the CLI from step 8 into a **stdio MCP server**. MCP over stdio is nothing more than a fancy CLI: the client starts a local process and talks JSON-RPC over stdin/stdout instead of passing arguments and reading exit codes. Three prompts: 9a builds the MCP server, 9b and 9c are the teacher requests from 8c/8d, this time fulfilled **through MCP tools only**.

Why stdio? The server runs locally as the user who started it, so it can use local resources a remote server can't reach: files on disk (in 9a, save tools read a JSON file), the intranet, devices on the local network (IoT).

Design decisions to discuss before the prompt:

* **Client-side adapter:** the MCP server lives in the CLI, reuses its code (Kiota client, error handling) and still calls the Web API, so it is just another API client. Step 10 puts MCP on the server side (`/mcp` next to the REST endpoints, calling GraderLogic directly): same tools, different adapter
* **One tool per CLI command, sharing the code:** a new command automatically becomes a tool, so there is no second implementation to keep in sync
* **Tool annotations ↔ `--yes`, tool errors ↔ exit codes/stderr:** read-only/destructive hints let the client decide when to ask the human. API errors become tool results with `isError: true` and the API's error code, so the agent can react. They are not protocol errors
* **Local file path:** save tools take the document inline or as the path of a local JSON file
* **llms.txt instead of training data:** spec revision 2026-07-28 is newer than the model's training data. It is stateless: no `initialize` handshake, a new `server/discover`, and every request carries protocol version + client capabilities in `_meta`. [llms.txt](https://modelcontextprotocol.io/llms.txt) is an index (~350 lines) of all docs and spec pages as Markdown. The agent reads the index and fetches only the pages it needs. `llms-full.txt` puts everything in one file (~2.5 MB), which is far too big for a context window. The prompt has the agent add llms.txt to "Researching Documentation" in AGENTS.md, so later sessions (step 10) use it too
* **Only the current revision:** no extra code for older revisions. Backward compatibility only if the SDK gives it for free, and it does: the C# SDK still answers the legacy `initialize` handshake
* **No registration in the repo** (no `.mcp.json`): the presenter configures the clients live

Prompt 9a (MCP server):

```text
Turn the CLI into an MCP server: `grader mcp` runs Grader as a stdio server for the Model Context Protocol, so AI agents can use it as tools.

- Research MCP with https://modelcontextprotocol.io/llms.txt and add it to "Researching Documentation" in AGENTS.md as the source for MCP concepts and the protocol. Support the current spec revision (2026-07-28); don't write extra code for older revisions.
- Use the official MCP C# SDK. One tool per CLI command, sharing the code with the commands. Tools that save a document take it inline or as a path to a local JSON file.
- Mark read-only and destructive tools; API errors become tool errors incl. the error code.
- Exemplary tests: a tool call through an MCP client against the Web API in memory; an API error becoming a tool error.
- Update tech-docs/cli.md (incl. how to register the server in an MCP client) and testing.md; mention the MCP server in the grader-cli skill.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
```

Rehearsal 9a: ~22 min, 81 turns, $5.52, no questions. This rehearsal ran with the old last line ("Done when … works against the running API …"), so the agent also checked everything against a live API on its own; the prompt now asks for tests + QA only to save stage time, and the numbers will change with the next rehearsal.

* Research: the agent read the CLI code, then fetched `llms.txt` with `curl`. From the index it downloaded the spec pages it needed: changelog, tools, stdio transport, versioning, `server/discover`, schema
* SDK: it looked up the SDK versions on NuGet and cloned the C# SDK into `~/github/csharp-sdk` (the AGENTS.md rule). It checked out the tag of the version it used and read the source and docs there
* Verification: a scripted raw JSON-RPC session against a real API on a **temporary database** (the development data stayed untouched), plus the MCP Inspector CLI
* One test run hung. The cause: the SDK's test client does not close the server's stdin. The agent fixed the test and documented this as a gotcha

What the agent built in 9a (details differ from run to run):

* **SDK:** `ModelContextProtocol.Core` 2.2.0, the low-level package (`McpServer.Create` + stdio transport, no generic host). 2.x supports 2026-07-28 since 2.0.0; 2.2.0 is the latest
* **Protocol:** `server/discover` answers `supportedVersions: ["2026-07-28"]`, and a request with `2025-11-25` in `_meta` gets error `-32022` (UnsupportedProtocolVersion). The legacy `initialize` handshake still works, for free from the SDK
* **CLI refactoring:** commands now write through an abstract `CommandContext`, either `CliCommandContext` (command line, stdout) or `McpToolContext` (tool arguments → tool result). Each command declares `CommandTraits` (read/create/save/delete), which become the annotations. The command list is the tool list: 13 commands = 13 tools, same names (`get-class`, `save-exam`, …), parameters in camelCase (`classId`)
* **Save/create tools:** they take `document` (inline) or `documentPath` (local JSON file). The `document` schema is the request schema from the OpenAPI spec, embedded in the CLI, so the agent sees types, lengths and ranges
* **Annotations:**
  * `get-*`: read-only
  * `create-*`: additive
  * `save-*`: destructive, because it removes children missing from the document (the same reason the skill says "compare before saving")
  * `delete-*` and `reset-demo-data`: destructive
* **Errors:** a tool error (`isError: true`) whose text is the `--json` error, e.g. `{"code": "class-not-found", "message": "...", "status": 404}`. An unknown tool is a JSON-RPC error (`-32602`)
* **Own `tools/list` handler:** the SDK's tool collection lists tools in hash order, but the spec asks for a deterministic order. Server instructions explain navigating by ids and saving complete documents
* **Results:** the API JSON as text, without `structuredContent`/`outputSchema`. Kiota omits `null` values, so results would fail validation against the response schemas
* **Tests (3):** a tool call through the SDK's MCP client against the in-memory Web API, an API error → tool error with code, and the annotations
* **`tech-docs/cli.md`:** new MCP section covering tools, annotations, errors, registering in clients (`claude mcp add`, JSON for other clients, MCP Inspector) and gotchas, e.g. "stdout belongs to the protocol". `testing.md` updated
* **AGENTS.md** ("Researching Documentation", new item 3):
  > Model Context Protocol (concepts, protocol, spec): start at https://modelcontextprotocol.io/llms.txt (index of all docs pages as Markdown) and use the pages of the spec revision we support, `2026-07-28` (it differs substantially from earlier revisions, e.g. no `initialize` handshake). For the official C# SDK (`ModelContextProtocol.Core`), read its docs and source in `~/github/csharp-sdk` at the tag of the version we use (see 5.).
* **Skill `grader-cli`:** the description now mentions `grader mcp`. A new section "Using the MCP server instead" covers ids as camelCase parameters, `document`/`documentPath`, no `--yes` (the client confirms) and errors as tool errors

Before 9b:

* Start the API in a second terminal (`dotnet run --project GraderApi`) and reset the demo data (Settings in the UI).
* Build the CLI (`dotnet build GraderCli`).
* Register the server in the client. Start the built dll, or `grader mcp` if the global tool is installed (`scripts/install-cli.sh`). In Claude Code (local scope, not in the repo; `claude mcp get grader` should show "Connected"):

  ```bash
  claude mcp add grader -- dotnet "$PWD/GraderCli/bin/Debug/net11.0/GraderCli.dll" mcp
  ```

* The rehearsal passed the server as a config file and disallowed the shell, so the agent could only use the MCP tools: `claude --mcp-config grader-mcp.json --strict-mcp-config --disallowedTools Bash`. The config file:

  ```json
  { "mcpServers": { "grader": { "type": "stdio", "command": "dotnet", "args": ["<repo>/GraderCli/bin/Debug/net11.0/GraderCli.dll", "mcp"] } } }
  ```

* MCPJam: `npx @mcpjam/inspector@latest` (the terminal install, because a stdio server is a local subprocess). Add a stdio server with command `dotnet` and arguments `<repo>/GraderCli/bin/Debug/net11.0/GraderCli.dll mcp`. MCPJam was not tried in the rehearsal. Should it still use the legacy `initialize` handshake, the SDK answers that too (tested with raw JSON-RPC).

Prompt 9b (a teacher's request, new session, MCP tools only):

```text
I'm taking over a new class this school year, 1D. Students: Lena Huber, Jonas Gruber, Sophie Wagner, Elias Bauer, Mia Steiner, Felix Moser, Hannah Hofer, Paul Leitner.

They wrote their first Mathematics test on 22 September 2026: Huber 78 %, Gruber 91.5 %, Wagner 64 %, Bauer 45 %, Steiner 88 %, Moser 72 %, Leitner 55 %. Hannah was sick.

Please enter all of this in Grader.
```

Rehearsal 9b: 20 s, 9 turns, $0.25.

* The agent loaded the `grader-cli` skill (its new MCP section applies), then called `get-school-years` → `get-classes` (checked that 1D is free) → `get-courses` → `create-class` (document inline) → `create-exam`
* Hannah got no result instead of 0 %
* Checked in the database: class 1D (2026/27) with 8 students; exam on 22 Sep 2026 with 7 results, none for Hannah Hofer

Prompt 9c (a question, new session, MCP tools only):

```text
Which three students had the lowest average result in Mathematics in school year 2025/26? Show their class and average.
```

Rehearsal 9c: 2 min, 8 turns, $0.99.

* The agent delegated the calculation to a subagent, which wanted to run a script. This session had no shell, so the subagent fetched the Mathematics exams of the 10 classes with 50 MCP calls (10× `get-exams`, 40× `get-exam`) and **computed the averages by hand**
* Answer (checked with SQL: correct): Elise Mills 2B 57.37 %, Marie Murray 2A 57.80 %, Nia Sipes 4A 58.30 %
* It added useful caveats: Nia Sipes has only 1 of 4 results, so it also named the runner-up with 3 results. It also said it had rechecked only the lowest five averages
* The demo data is random, so names and numbers change with every reset (the 8d rehearsal had other students)

Demo (API running; `dotnet build GraderCli`; tested in zsh and bash):

```bash
mcp() {  # send JSON-RPC requests (one per argument) to `grader mcp`; keep stdin open until the answers are out
  { printf '%s\n' "$@"; sleep 2; } | dotnet GraderCli/bin/Debug/net11.0/GraderCli.dll mcp
}
META='"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}'
mcp '{"jsonrpc":"2.0","id":1,"method":"server/discover","params":{'"$META"'}}' | jq 'del(.result.instructions)'
mcp '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{'"$META"'}}' | jq -c '.result.tools[] | {name, annotations}'
mcp '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get-class","arguments":{"classId":99999},'"$META"'}}' | jq .result
echo '{"name": "9Z", "students": [{"firstName": "Anna", "lastName": "Local"}]}' > /tmp/class-9z.json
mcp '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"create-class","arguments":{"schoolYearId":<year-id>,"documentPath":"/tmp/class-9z.json"},'"$META"'}}' | jq -r '.result.content[0].text'
mcp '{"jsonrpc":"2.0","id":5,"method":"tools/list","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2025-11-25"}}}' | jq -c .error
npx -y @modelcontextprotocol/inspector --cli dotnet "$PWD/GraderCli/bin/Debug/net11.0/GraderCli.dll" mcp --method tools/call --tool-name get-class --tool-arg classId=99999
```

1. `dotnet GraderCli/bin/Debug/net11.0/GraderCli.dll mcp --help` (with the global tool: `grader mcp --help`)
2. Raw JSON-RPC, one request after another:
   * `server/discover` as the first request, with no `initialize`: supported versions, capabilities, `serverInfo` in `_meta`
   * `tools/list`: 13 tools with their annotations
   * a tool error with `isError` and the `code`
   * `create-class` from a **local file**: take `<year-id>` from `get-school-years` (ids change with every reset)
   * an old protocol version → error `-32022`
3. MCPJam (or the MCP Inspector): browse tools, schemas (`document` vs. `documentPath`) and annotations, and call a tool
4. AGENTS.md: the llms.txt entry. Then open https://modelcontextprotocol.io/llms.txt in the browser
5. New session with only the MCP server → prompt 9b → check class 1D in the UI
6. New session → prompt 9c

Things to show/discuss:

* **stdio MCP = fancy CLI:** same process, same code, same API calls; only the interface differs (JSON-RPC over stdin/stdout instead of argv, stdout and exit code). stdout belongs to the protocol, so a single stray `Console.WriteLine` breaks the server
* **Gotcha when piping by hand:** the server stops at stdin EOF and drops requests still in flight. `printf '…' | grader mcp` prints nothing, hence the `sleep 2` in the demo
* **Local resources:** `documentPath` reads a file on the user's machine. The same pattern reaches the intranet or IoT devices, which a remote server (step 10) can't
* **Annotations are hints for the client** (e.g. ask before a destructive tool): the confirmation moves from `--yes` into the client
* **MCP vs. CLI + skill** (talking point only): in 9c there was no shell, so 40 exams went through the context and the averages were computed by hand ($0.99). In 8d the CLI output went through Python instead, and only the result reached the model ($0.32). MCP shines where there is no shell. The skill still helped: 9b loaded `grader-cli`, which now explains the MCP variant
* **Spec 2026-07-28 in the raw JSON:** `_meta` on every request, `server/discover`, `resultType`, `ttlMs`/`cacheScope`
* **llms.txt:** the index vs. `llms-full.txt`. The agent fetched the index with `curl` and then only the pages it needed. The spec revision from July 2026 was implemented correctly without a single question
* **AGENTS.md rules at work:** the agent cloned the SDK to read its source and protected the development data with a temporary database

Learnings:

* stdio MCP = a CLI with a JSON-RPC interface on stdin/stdout; it runs as the local user and can use local resources
* Tool annotations and tool errors are the MCP counterparts of `--yes`, exit codes and stderr
* One command list feeds both interfaces: no second implementation, no drift
* llms.txt is an index of Markdown pages for agents. Current docs beat training data, and one line in AGENTS.md makes it stick
* Cost: 9a ~$5.50; using the tools ~$0.25–1 per request

Links:

* [MCP llms.txt](https://modelcontextprotocol.io/llms.txt), [llms.txt proposal](https://llmstxt.org/)
* MCP spec 2026-07-28: [Key changes](https://modelcontextprotocol.io/specification/2026-07-28/changelog), [Versioning](https://modelcontextprotocol.io/specification/2026-07-28/basic/versioning), [stdio transport](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/stdio), [Discovery](https://modelcontextprotocol.io/specification/2026-07-28/server/discover), [Tools](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)
* [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), [ModelContextProtocol.Core on NuGet](https://www.nuget.org/packages/ModelContextProtocol.Core)
* [MCP Inspector](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector), [MCPJam](https://www.mcpjam.com/) ([GitHub](https://github.com/MCPJam/inspector))
* [Claude Code MCP](https://code.claude.com/docs/en/mcp)

## Step 10 – Streamable HTTP MCP server in the backend

Goal: the Web API itself becomes an MCP server. `POST /mcp` offers the same tools as `grader mcp` over **Streamable HTTP**. MCP over HTTP is nothing more than another protocol for web APIs: JSON-RPC in the body of a plain POST. Architecturally, it is **hexagonal** (ports and adapters): the layering from step 6 (thin endpoints → use-case services in GraderLogic) lets `/mcp` be a thin server-side adapter over the same services, right next to the REST endpoints. One prompt, no teacher requests. The demo runs with MCPJam and raw `curl`.

stdio (step 9) vs. Streamable HTTP (step 10):

| | stdio: `grader mcp` | Streamable HTTP: `/mcp` |
|---|---|---|
| What it is | a process the client starts; JSON-RPC over stdin/stdout | an HTTP endpoint; one POST per JSON-RPC request |
| Users | one local user, the one who started it | many users over the network |
| Reach | local resources: files (`documentPath`), intranet, IoT | only what the server can reach; deployed centrally |
| Installation | .NET SDK + global tool (or the built dll) on every machine | none: a URL in the client |
| Calls | the Web API over HTTP (Kiota client) | the GraderLogic services directly, like the endpoints |
| Auth | the local user's credentials (here: none) | none yet; OAuth in a later step |

Design decisions to discuss before the prompt:

* **Server-side adapter:** the tools call the GraderLogic services directly, like the REST endpoints do. They don't use HTTP, Kiota or the CLI. A new protocol costs one thin layer, not a second implementation of the business logic
* **Same tools as `grader mcp`:** names, descriptions and annotations are the same, and there is no `documentPath` because the server can't read the user's disk. An MCP client config can be swapped from `grader mcp` to `http://localhost:5100/mcp` and the same tools appear
* **Only spec revision 2026-07-28 (stateless):** no sessions, no GET stream, every request carries protocol version and client capabilities in `_meta`, plus the `Mcp-Method`/`Mcp-Name` headers. No extra code for older revisions
* **Tools are plain methods:** `[Description]` attributes, grouped by use-case area, with no MCP types and no MCP logic. Step 11 turns them into a shared tool catalog (`AIFunction`) for MCP and for an in-app agent
* **llms.txt is not in the prompt:** does the AGENTS.md rule from step 9 make the agent use it anyway? (See the rehearsal)
* **No auth yet:** never expose this endpoint publicly (see "Things to show/discuss")

Prompt 10:

```text
Add an MCP server to the Web API: GraderApi offers the tools of `grader mcp` over Streamable HTTP at /mcp, so agents can use Grader without installing anything.

- Same tool names, descriptions and annotations as the stdio server in GraderCli (without local file paths). The tools call the GraderLogic services directly, like the endpoints (see web-api.md).
- Only the current spec revision (2026-07-28, stateless); don't write extra code for older revisions.
- Tools are plain methods with descriptions, grouped by use-case area, without MCP-specific parameters or logic – an in-app agent will reuse them later.
- Exemplary tests: a tool call against /mcp of the Web API in memory; a business rule violation becoming a tool error.
- New tech docs article tech-docs/mcp.md (both MCP servers, how they differ, how to connect a client); update AGENTS.md and testing.md.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
```

Rehearsal 10: ~11 min, 69 turns, $3.90, no questions while working. At the end it asked one question: should `/mcp` get Origin/Host validation? This rehearsal ran with the old last line ("Done when … works against the running API …"), so the agent also checked everything against a live API on its own; the prompt now asks for tests + QA only to save stage time, and the numbers will change with the next rehearsal.

* **Research:** the agent read the CLI's MCP code, the API and the tech docs. Following the AGENTS.md rule, it read the SDK docs (stateless mode, tools) and source in `~/github/csharp-sdk`, plus the XML docs of `Microsoft.Extensions.Validation`
* **llms.txt:** used, but late and targeted. After the implementation, it fetched `llms.txt` with `curl` and from it only the Streamable HTTP page, to check `GET /mcp` (→ 405) and the `Origin` rule. The protocol details themselves came from the SDK, which already implements 2026-07-28. The AGENTS.md rule did its job without being in the prompt
* **Verification:** raw `curl` JSON-RPC against its own API instance on port 5177 with a **temporary database**, then a standalone MCP client (a file-based C# app with `#:package ModelContextProtocol.Core@2.2.0`) against the running API
* **Small detours, no hangs:** a missing `using`, a wrong `dotnet test` filter syntax, and `GET /mcp` answered by the SPA fallback (404 instead of 405). The agent's own consistency test failed once because the SDK copies the title into `annotations.title`

What the agent built (details differ from run to run):

* **SDK:** `ModelContextProtocol.AspNetCore` 2.2.0, `WithHttpTransport` with `SessionMode = Stateless`, `MapMcp("/mcp")`, excluded from the OpenAPI spec (neither generated client changed)
* **Tools (`GraderApi/Tools/`):**
  * `ClassManagementTools`, `ExamTools` and `DemoDataTools`: one method per operation (`GetClass`, `SaveExam`, …) with `[Description]`, the use-case service injected, and GraderLogic records as parameters and results. No MCP types
  * Tools are a boundary of their own, so they validate their request records themselves. `RequestValidator` runs the same data annotation validation as the minimal API endpoints
* **MCP layer (`GraderApi/Mcp/`):**
  * `GraderMcpServer` lists the 13 tools in the CLI's order with their `ToolTraits` (read/create/save/delete/reset → annotations). Names and titles come from the method names (`GetClass` → `get-class`, "Get class")
  * Its own `tools/list`/`tools/call` handlers, as in step 9, for a deterministic order
  * Arguments and results use the API's JSON options
  * `ToolErrors` is a call-tool filter that turns exceptions into the stdio server's error JSON `{"code", "message", "status", "errors"}`
  * `GET /mcp` is mapped explicitly to 405 (otherwise the SPA fallback answers)
* **Duplication CLI ↔ API:**
  * Descriptions and server instructions are **copied** as string literals (with a comment pointing to the CLI)
  * A test, `ListTools_SameToolsAsCliMcpServer`, compares name, title, description, annotation hints and order of both servers. It doesn't compare parameters: those differ on purpose (`documentPath`)
* **Tests (3):** a tool call over `/mcp` of the in-memory API (SDK `McpClient` with `HttpClientTransport`, pinned to 2026-07-28), a business rule violation (`create-exam` with an unknown course → tool error `course-not-found`, 400), and the tool list comparison with `grader mcp`. The CLI test helper moved to `CliMcpServerSession.cs`
* **Docs:**
  * New `tech-docs/mcp.md`: comparison table of both servers, what they share, tools as plain methods, the MCP layer, adding a tool, connecting clients, testing, and gotchas (GET vs. SPA fallback, no auth and no Origin validation, nullable types in schemas)
  * AGENTS.md: project list, and the "API Changes" rule now includes the MCP tool
  * testing.md, cli.md, hosting.md, web-api.md and the `grader-cli` skill updated

Checked after the rehearsal (API on :5100 with demo data):

* `server/discover` → `supportedVersions: ["2026-07-28"]`. Responses come as SSE (`event: message` + `data: {…}`) even for a single response
* `tools/list`: 13 tools. Compared with `grader mcp`:
  * **identical:** names, titles, descriptions, order, server instructions, and the four annotation hints (the HTTP server additionally has `annotations.title`)
  * **input schemas equivalent:** same parameters apart from `documentPath`, same types, `maxLength` and 0–100 range
  * **differences:**
    * `document` is required over HTTP (stdio: `document` or `documentPath`)
    * the HTTP schema has no field descriptions (stdio takes them from the OpenAPI spec)
    * the `document` descriptions differ
    * `get-exams.classId` says "see get-classes" over HTTP but "see class-management get-classes" in the CLI (a stale step 8 command name there). The comparison test doesn't cover parameter descriptions
* **Errors:**
  * unknown id → `isError` with `class-not-found`/404
  * duplicate class name → `class-name-taken`/409
  * name too long → `validation-failed` with field errors
  * `classId: "abc"` → `invalid-request`
  * unknown tool → JSON-RPC error `-32602` (a protocol error, not a tool error)
* **Protocol checks:**
  * a missing `Mcp-Method` header or an `Mcp-Name` that doesn't match the body → HTTP 400 with `-32020` (HeaderMismatch)
  * a request without `_meta` → HTTP 400 with `-32602` ("must include `_meta/io.modelcontextprotocol/protocolVersion`")
  * version `2025-11-25` in `_meta` → error `-32022` with `supported: ["2026-07-28"]`, but with HTTP 200 inside the SSE stream (the spec says 400; SDK behavior)
  * the legacy `initialize` handshake still works, for free from the SDK
  * `GET /mcp` → 405
* **MCP Inspector CLI 2.8.0:** connects with its default era (legacy: `initialize`) and with `"protocolEra": "modern"` from a config file (`server/discover`). `--strict` reports 4 portability warnings: `type: ["integer","null"]` in the nullable ids/percentages
* **Claude Code 2.1.283:** connects in the modern era (the API log shows `server/discover`, `tools/list`, `tools/call` from `claude-code`, no `initialize`). With the HTTP config, `--strict-mcp-config` and no shell, "List the classes of school year 2025/26" was answered correctly in 11 s, 4 turns, $0.13. `claude mcp add --transport http …` → `claude mcp get` shows "Connected"

Before the demo:

* Start the API (`dotnet run --project GraderApi`) and reset the demo data (Settings in the UI). The API is the MCP server; nothing else to build or install
* MCPJam: add an HTTP server with URL `http://localhost:5100/mcp`. MCPJam was **not tried** in the rehearsal. Use the terminal install from step 9 (`npx @mcpjam/inspector@latest`); whether MCPJam's hosted web app can reach a localhost API (which sends no CORS headers) is untested
* Claude Code (tested, local scope, not in the repo):

  ```bash
  claude mcp add --transport http grader-http http://localhost:5100/mcp    # claude mcp get grader-http → Connected
  claude mcp remove grader-http                                           # after the demo
  ```

* The swap: the step 9 config file with only the server entry changed (tested with `claude -p --mcp-config grader-http.json --strict-mcp-config --disallowedTools Bash`):

  ```json
  { "mcpServers": { "grader": { "type": "stdio", "command": "dotnet", "args": ["<repo>/GraderCli/bin/Debug/net11.0/GraderCli.dll", "mcp"] } } }
  { "mcpServers": { "grader": { "type": "http", "url": "http://localhost:5100/mcp" } } }
  ```

Demo (API running; tested in zsh and bash). Stateless means every call is a plain POST: no handshake, no session, no open connection. It's "just a web API":

```bash
mcp() {  # mcp <method> [<tool> <arguments-json>]: one JSON-RPC request = one plain HTTP POST
  local h=(-H "Mcp-Method: $1") p=""
  if [ -n "$2" ]; then h+=(-H "Mcp-Name: $2"); p='"name":"'"$2"'","arguments":'"$3"','; fi
  curl -s http://localhost:5100/mcp "${h[@]}" -H 'MCP-Protocol-Version: 2026-07-28' \
    -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' \
    -d '{"jsonrpc":"2.0","id":1,"method":"'"$1"'","params":{'"$p"'"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}' \
    | sed -n 's/^data: //p'
}
mcp server/discover | jq 'del(.result.instructions)'
mcp tools/list | jq -c '.result.tools[] | {name, annotations}'
mcp tools/call get-school-years '{}' | jq -r '.result.content[0].text' | jq -c .
Y=$(mcp tools/call get-school-years '{}' | jq -r '.result.content[0].text' | jq -r '.[] | select(.name=="2025/26") | .id')
mcp tools/call get-classes '{"schoolYearId": '"$Y"'}' | jq -r '.result.content[0].text' | jq -c '.[]'
mcp tools/call get-class '{"classId": 99999}' | jq -c .result
mcp tools/call create-class '{"schoolYearId": '"$Y"', "document": {"name": "1A", "students": []}}' | jq -r '.result.content[0].text'
curl -s -w ' HTTP %{http_code}\n' http://localhost:5100/mcp -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' \
  -H 'MCP-Protocol-Version: 2026-07-28' -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}'   # no Mcp-Method header → -32020, HTTP 400
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5100/mcp                                          # GET → 405
npx -y @modelcontextprotocol/inspector --cli http://localhost:5100/mcp --transport http --method tools/list
```

1. `curl -i` once (e.g. `server/discover`) to show the HTTP layer: `Content-Type: text/event-stream`, `X-Accel-Buffering: no`, the answer as one `data:` line
2. Raw JSON-RPC:
   * `server/discover`
   * `tools/list`: the same 13 tools as step 9
   * a successful call
   * a tool error (`class-not-found`)
   * a business rule violation: `1A` already exists → `class-name-taken`, 409. The same code the REST endpoint returns
   * a missing header → HeaderMismatch
3. `GraderApi/Tools/ClassManagementTools.cs` next to `ClassManagementEndpoints.cs`: two thin adapters over the same `IClassManagementService`. Then `GraderApi/Mcp/` (the only MCP-specific code)
4. MCPJam (or the MCP Inspector) with `http://localhost:5100/mcp`: browse tools and annotations, call a tool
5. The swap: the stdio config from step 9 → the HTTP config (or `claude mcp add --transport http`), then `/mcp` in Claude Code: the same tools. Ask something read-only
6. `tech-docs/mcp.md`: the comparison table and the gotchas (the agent wrote about the missing Origin validation without being asked)

Things to show/discuss:

* **MCP over HTTP = another web API protocol:** every request is a POST with JSON-RPC in the body, and the headers (`Mcp-Method`, `Mcp-Name`) mirror the body so gateways can route and inspect without parsing it. Stateless 2026-07-28 needs no sticky sessions, so any instance behind a load balancer can answer any request
* **Hexagonal architecture pays off:** REST endpoints and MCP tools are two adapters over the same use-case services. Business rules, error codes and validation are shared. The MCP-specific code (`GraderApi/Mcp/`) is ~220 lines
* **llms.txt rule effect:** the prompt didn't mention it, yet the agent went to llms.txt when it needed a spec detail the SDK doesn't answer (GET → 405, `Origin`). For everything else, the cloned SDK source was the better source. Both rules came from AGENTS.md
* **No installation for non-technical users** (talking point, no live demo): a remote MCP server is just a URL in the client (e.g. VS Code, Claude Desktop, Cursor). Real-world examples: the [Microsoft Learn MCP server](https://learn.microsoft.com/training/support/mcp) (`https://learn.microsoft.com/api/mcp`, no sign-in), the [remote GitHub MCP server](https://github.com/github/github-mcp-server) (`https://api.githubcopilot.com/mcp/`, OAuth), and the [MCP Registry](https://registry.modelcontextprotocol.io/). Compare this with step 9: .NET SDK, global tool, local config
* **Security:**
  * `/mcp` has **no auth** and no `Origin` validation (the spec's DNS rebinding MUST). Anyone who can reach the port can call `reset-demo-data`. Localhost only, never expose it publicly
  * OAuth for remote MCP servers comes in a later step
  * The agent flagged the Origin gap itself and asked instead of changing `AllowedHosts` for the whole API
* **stdio vs. HTTP:** see the table above. stdio for local resources and one user; HTTP for central deployment, many users and no installation
* **Tool granularity:** the tools mirror the API 1:1. In 9c that cost 50 tool calls (10× `get-exams`, 40× `get-exam`). Remote MCP has no shell to post-process results, so task-oriented tools (e.g. "averages per student and course") would pay off. The API shape is not automatically a good tool shape
* **Duplication CLI ↔ API:** descriptions and instructions are copied, and a test guards names, titles, descriptions and annotations. But parameter descriptions already differ, and the test doesn't catch that. Step 11 replaces the copies with one shared tool catalog
* **Protocol eras in practice:**
  * Claude Code 2.1.283 speaks 2026-07-28 (`server/discover`)
  * the MCP Inspector defaults to the legacy `initialize`, and the SDK still answers it
  * one SDK deviation: the unsupported-version error comes with HTTP 200 instead of 400

Learnings:

* Hexagonal architecture pays off: a new protocol is a thin adapter over the same logic
* stdio MCP = a local fancy CLI; HTTP MCP = a remote web API protocol (central deployment, no installation, needs auth)
* Keep tools plain (methods + descriptions) and the protocol in a thin layer. That makes the tools reusable beyond MCP
* Guard duplicated contracts with a test, or better, don't duplicate them (step 11)
* Cost: ~$3.90 for the server-side MCP server incl. tests and docs

Outlook step 11: an in-app agent as another "UI". It starts with a refactoring into a shared tool catalog based on `AIFunction` (Microsoft.Extensions.AI) that feeds both the MCP server and a Microsoft Agent Framework agent. The agent is exposed via AG-UI to a simple self-built Angular client (`@ag-ui/client`, Markdown → HTML, a bit of CSS from the design system). No CopilotKit.

Links:

* MCP spec 2026-07-28: [Streamable HTTP](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http), [Transports](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports), [Key changes](https://modelcontextprotocol.io/specification/2026-07-28/changelog), [Versioning](https://modelcontextprotocol.io/specification/2026-07-28/basic/versioning), [Discovery](https://modelcontextprotocol.io/specification/2026-07-28/server/discover), [Tools](https://modelcontextprotocol.io/specification/2026-07-28/server/tools), [Authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)
* [MCP Security Best Practices](https://modelcontextprotocol.io/docs/2026-07-28/tutorials/security/security_best_practices), [Connect to remote MCP servers](https://modelcontextprotocol.io/docs/2026-07-28/develop/connect-remote-servers), [MCP llms.txt](https://modelcontextprotocol.io/llms.txt)
* [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), [ModelContextProtocol.AspNetCore on NuGet](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
* MCP Inspector: [CLI](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector/cli), [Protocol eras](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector/protocol-eras); [MCPJam](https://www.mcpjam.com/) ([GitHub](https://github.com/MCPJam/inspector)); [Claude Code MCP](https://code.claude.com/docs/en/mcp)
* [Hexagonal architecture (Alistair Cockburn)](https://alistair.cockburn.us/hexagonal-architecture/)
* Step 11: [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai), [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/), [AG-UI](https://docs.ag-ui.com/)
