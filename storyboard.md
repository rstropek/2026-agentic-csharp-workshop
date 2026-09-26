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
