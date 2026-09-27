# Grader – Workshop Storyboard

Full-day workshop: build a grading app for teachers and students with a coding agent. .NET 11 backend, Angular frontend.

How to use this storyboard:

* Every step has a **script** (copy & run in a terminal) and/or a **prompt** (copy & paste into your coding agent).
* Start each prompt in a **fresh agent session** in the repository root (`grader/`).
* Each step ends with a git commit. If a step goes wrong: `git reset --hard && git clean -fd` and re-run it.
* Rehearsed with Claude Code (`claude -p`, Opus 5.5, effort high – the same as on stage). Durations below come from that rehearsal.

## Prerequisites

* .NET 11 SDK (RC 1 or newer: `dotnet --list-sdks`)
* Node.js 24 LTS + npm
* Git, bash/zsh (macOS, Linux, WSL or Git Bash)
* A coding agent that supports `AGENTS.md` and skills (e.g. Claude Code, GitHub Copilot, Codex)
* For steps 7 and 11c: a browser the agent can control (e.g. Chrome with [Claude in Chrome](https://claude.com/chrome), `claude --chrome`). Set the browser zoom to **100%** and make the window at least 1920 px wide. The UI is designed for Full HD, and with a different zoom the agent has to emulate the viewport to compare it with the mockups or take the exact screenshots with Playwright instead
* For step 11: an [OpenRouter](https://openrouter.ai) API key (model `z-ai/glm-5.3-flash`)

## Timing (rehearsal, Opus 5.5 effort high)

Wall-clock time per prompt in the last full rehearsal (Sept. 26th, 2026). Every step builds on the previous one, so prompts can't run in parallel with each other.

| Step | Prompt | Time | Approx. Cost* |
| --- | --- | ---: | ---: |
| 1 | script: app skeleton | ~1 min | – |
| 2 | skills script + AGENTS.md | ~1 min | >  $1 |
| 3 | connect the projects | 13 min | $4 |
| 4 | QA baseline | 9 min | $3 |
| 5 | database (spec-driven) | 9.5 min | $3 |
| 6 | Web API (architecture first) | 13.5 min | $4.5 |
| 7a | UI foundation + classes | 22 min | $10 | **break** |
| 7b | Exams UI | 19.5 min | $8 | **break** |
| 8a | CLI | 19 min | $6 | **break** |
| 8b | skill | 6.5 min | $3.5 |
| 8c / 8d | teacher prompts (CLI) | 40 s / 70 s | > $1 |
| 9a | stdio MCP server | 22 min | $6 | **break** |
| 9b / 9c | teacher prompts (MCP only) | 24 s / 1.5 min | > $1 |
| 10 | HTTP MCP server | 13.5 min | $6 |
| 11a | shared tool catalog | 6.5 min | $2.5 |
| 11b | agent + AG-UI endpoint | 10 min | $3.5 | 
| 11c | Assistant area (UI) | 20 min | $8.5 |
| | **total** | **~3 h 10 min** | **~$65-75** |  

*) Theoretical cost based on API pricing, less if you use a Claude subscription.

Fallback (e.g. no internet): the rehearsal's Grader repository incl. a tag per step is in `rehearsal/grader.bundle` – `git clone rehearsal/grader.bundle grader && cd grader && git reset --hard step7b` continues after step 7b without running its prompt (see [rehearsal/README.md](rehearsal/README.md)).

---

## Step 1 – App skeleton

Goal: solution with class library, Web API, tests and Angular frontend. Created by **deterministic tools**, not by the LLM.

> Tip: If you are not using a bash shell, use your coding agent to translate scripts into e.g. CMD, PowerShell, etc.

Script (run in the folder that should contain `grader/`):

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
dotnet new webapi   -n GraderApi   -o GraderApi   --kestrelHttpPort 5100 --kestrelHttpsPort 7100
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
* Carefully review the script. Examples for gotchas found while preparing the script:
  * `dotnet new xunit` still defaults to xUnit **v2** → `--xunit-version v3`
  * v3 template configures MTP in `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`)
  * Packages that follow the .NET release train (`ApiDescription.Server`, `Mvc.Testing`) need `--prerelease` while .NET 11 is in RC
  * `ng new` creates its own Git repo unless `--skip-git`; `--ai-config=none` and `--interactive=false` avoid prompts
  * Instead of `--ai-config`, we download the Angular team's [best-practices.md](https://angular.dev/assets/context/best-practices.md) as `GraderWeb/AGENTS.md`: nested `AGENTS.md` files apply to their folder, so the Angular rules only load when the agent works in the frontend
  * `dotnet new webapi` picks random ports → `--kestrelHttpPort`/`--kestrelHttpsPort` (not shown in `--help`) pin them, so the API is always on http://localhost:5100 and later steps (proxy, CLI, MCP) can rely on it
  * `dotnet sln`/`dotnet new sln` creates `.slnx` (XML solution format) by default
  * .NET 10+ noun-first CLI syntax: `dotnet package add`, `dotnet reference add`

Links:

* [`dotnet new` templates](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-sdk-templates), [global.json](https://learn.microsoft.com/dotnet/core/tools/global-json)
* [`ng new` options](https://angular.dev/cli/new) (note `ai-config` option), [Zoneless Angular](https://angular.dev/guide/zoneless)
* [xUnit v3](https://xunit.net/docs/getting-started/v3/getting-started), [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
* [Generate OpenAPI documents at build time](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi), [Scalar](https://github.com/scalar/scalar)

---

## Step 2 – Give the agent access to up-to-date docs

Goal: .NET 11 and Angular 22 are newer than the model's training data → the agent must look up current docs.

Script (in `grader/`) installs skills project-scoped for Claude Code, GitHub Copilot and Codex:

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

Rehearsal: 30 s (script ~1 min), $0.25.

Learnings:

* Skills ([agentskills.io](https://agentskills.io)): folder with `SKILL.md` (name + description in front matter, instructions in body) + optional scripts/references
  * Only name + description are in context; body is loaded when relevant (progressive disclosure) → many skills are cheap
  * Open standard, supported by many agents; can also be invoked explicitly as slash commands (e.g. `/find-docs`)
* [skills.sh](https://skills.sh): directory + CLI (`npx skills add|list|update|remove`) for installing skills from GitHub repos
  * Review skills before installing, they run with full agent permissions (skills.sh shows security assessments)
* Important docs skills:
  * `microsoft-docs` – Microsoft Learn (MCP server or `npx @microsoft/learn-cli search|fetch`)
  * `find-docs` – Context7 (`npx ctx7@latest library|docs`), docs for almost any open source library
  * `angular-developer` – official Angular team guidance (signals, signal forms, testing, …)
  * `skill-creator` – helps to write and evaluate your own skills
  * Start exploring skills by looking into your Agent's default skills or ([`skills.sh` topics](https://www.skills.sh/topic))
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

Advanced exercise: Inspect network traffic with VSCode's _Chat Debug View_ and/or [`claude-via-mitmproxy.sh`](https://github.com/anthropics/claude-via-mitmproxy).

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

Rehearsal: ~13 min, 55 turns, $3.84, no questions. With effort high the agent researches a lot (~25 Learn CLI searches/fetches: build-time OpenAPI, `WebApplicationFactory`, error handling) and verified from scratch at the end (deleted generated client, `wwwroot`, `bin`/`obj`, rebuilt, ran all suites).

Discussions:

* Decisions worth reviewing/discussing: Spec committed (API changes show up as diffs)? Generated Angular client committed or regenerated by `npm start/build/test`? E2e tests run against the published app in Production mode? Scalar/OpenAPI only in Development?
* Non-determinism: same prompt, different names, paths, number types and policies across runs → review decisions, write down the ones that matter (tech docs, prompt)

Things to show/discuss:

* Walk through `tech-docs/*.md`: decisions + gotchas + pointers, no code copies
* Try it: `dotnet run --project GraderApi` (http://localhost:5100, API docs at `/scalar`) + `cd GraderWeb && npm start` → http://localhost:4200

Learnings:

* Good test posture is essential: tests are the agent's feedback loop (and yours when reviewing agent code)
* Coding agents are very good at setting up test infrastructure (WebApplicationFactory, Vitest mocks, Playwright `webServer`, …); boring for humans, cheap for agents
* Smoke-test content first: find setup problems (proxy, hosting, client generation) before they hide inside real features
* Skills vs. tech docs:
  * Skills = reusable across projects (framework knowledge, workflows), convenient slash commands
  * Tech docs = project-specific decisions, principles, gotchas; referenced from `AGENTS.md`, loaded on demand
* Keep `AGENTS.md` small: overview + commands + index of tech docs; details live in tech docs (progressive disclosure again)
* Give agents exact commands (start, test, generate) → no guessing, fewer wasted turns
* If unsure, evaluate, not guess. Gut feeling is often misleading. Amount of Agent guidance depends on used model and harness.
* Effort is a model-specific setting: the [Opus 5.5 prompting guide](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/prompting-claude-opus-5-5#calibrate-effort) recommends starting at `medium` (its default) and testing other levels, because level names don't mean the same amount of thinking across models (5.5 at `medium` ≈ Opus 5 at `high` for coding). We rehearsed at `high`, which explains the extensive research above. Homework: rerun a step at `medium` and compare time, cost and result

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

Rehearsal: ~9 min, 40 turns, $2.66, no questions, no struggles. The agent proved the baseline with planted mistakes (unused using, formatting, `int` vs. `var`, CS0219, `==`, missing `alt`, a signal used without calling it); all caught, then reverted.

Demo (works independently of the names the agent chose):

```bash
scripts/qa.sh                                                 # all green, ~15 s
printf 'using System.Text;\n\nnamespace GraderLogic;\n\npublic class Oops { public int M() { int unused = 1; return 42; } }\n' > GraderLogic/Oops.cs
echo 'var unused = "x";' >> GraderWeb/src/app/app.routes.ts   # unused variable
scripts/qa.sh                                                 # several checks FAIL, exit 1, file:line + rule IDs (IDE0005, CS0219, CA1822, noUnusedVariables, …)
git checkout -- . && rm GraderLogic/Oops.cs
```

Things to show/discuss:

* Output format designed for agents; a nice live follow-up prompt if the output is too noisy: "make the QA output more concise"
* Agent rule: "fix code, don't suppress findings; change rules only with documented reason"; otherwise agents tend to "fix" by disabling
* Without "directly to main" in the prompt, Claude Code sometimes creates a feature branch (its default git behavior)
* Make calling of QA tools deterministic by using _hooks_

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

* Mermaid ER diagram (renders on GitHub/VS Code, readable for humans *and* agents)
* Constraints and rules in plain text
* Specification of generated test data
* Written *before* the agent session, supported by agent, reviewed like code (it took several review rounds to remove contradictions)

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

Rehearsal: ~9.5 min, 31 turns, $2.56, no questions, no struggles (the spec has no contradictions).

Experiment ("homework"): Add contradiction to the specification, run prompt in _Plan_ mode, observe how Agent will ask for clarification.

Findings (details differ from run to run):

* Decisions the agent flagged itself: migrations marked as generated code in `.editorconfig` (EF Core 11 migrations violate the analyzers, close to the "no suppressions" rule, so it asked for review)

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
* Tests prove that the **database** rejects the change (not EF Core)

Learnings:

* Prepare agentic tasks with artifacts, not just prompts: spec = *what* (domain, rules, data), prompt = *task* (scope, tools, done criteria), tech docs = *how* (written by the agent, maintained with the code)
* Review specs like code: contradictions in the spec become random behavior of the agent
  * Discuss skills like `brainstorming` or `grilling`
  * Discuss _Plan_ mode
* Specs age: code and tech docs are the source of truth; say so in `AGENTS.md` and let the agent ask if unsure
* Every prompt from here on ends with an explicit completion condition ("Done when the QA script is green. Then commit …"). The [Opus 5.5 prompting guide](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/prompting-claude-opus-5-5#unattended-agentic-runs) recommends stating it up front for unattended runs like `claude -p`: the model sometimes ends a turn with a progress report, and a clear condition makes "done" checkable
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

Walk through the article (`tech-docs/web-api.md`), principles and patterns only, no concrete endpoints:

* Use cases, not tables: no generic CRUD, endpoints grouped by use-case area, response shapes tailored to the use case, server decides sort order
* Layers: thin endpoints → use-case service interfaces in GraderLogic (injected, mockable) → `DbContext`
* Data access: entities never leave GraderLogic, reads project into records, one request = one transaction; pattern for saving a parent with all its children (`PUT` with the complete document)
* Errors: services translate DB errors into a few meaningful exceptions with stable error codes → central handler → Problem Details with a `code` member
* Validation at the API boundary, HTTP + OpenAPI conventions (tags = use-case areas, operation names = use cases)
* Dates without time stay ISO strings (`yyyy-MM-dd`) in every client, incl. the generated Angular client, a JavaScript `Date` is midnight UTC and can show the previous day.
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

Rehearsal: ~13.5 min, 53 turns, $4.28, no questions, no struggles, 12 use cases + demo data.

Demo:

```bash
git diff HEAD~1 -- tech-docs/web-api.md     # what the agent added to our article
dotnet run --project GraderApi              # applies migrations, creates data/grader.db if needed
```

In Scalar (`/scalar` on the backend URL) or `GraderApi/GraderApi.http`:

1. `POST /api/demo-data` → 204, database filled
2. `GET /api/school-years` → newest first; classes of a school year incl. student count; a class incl. its students
3. `GET` an exam → all students of the class, `percentage: null` for missing results
4. `PUT` the class without one of its students who has results → 409 `student-has-results` (whole request rejected)
5. `DELETE` a class that has exams → 409 `class-has-exams`; `POST` a class with an empty name → 400 with field errors + `code: validation-failed`

Things to show/discuss:

* The article as a living document: our principles are unchanged, the agent added pointers, examples to copy and gotchas (`git diff` of `web-api.md`)
* Architecture followed: OpenAPI schemas contain only DTOs, no entity types; tags + operation names → generated Angular services read like use cases
* The agent asks for review where it deviates from the article instead of silently ignoring it (the "ask first" rule in the prompt)
* The prompt contains nothing about layers, error handling or conventions, the result still follows them

Learnings:

* Architecture first: humans write principles and patterns *before* the agent session; the agent follows them and keeps the article up to date
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

Design system first: derive reusable building blocks from the design: CSS variables (colors incl. dark theme, typography, spacing), global CSS classes, base components and base services (e.g. the global school year selection, turning Problem Details into user messages). Components never hard-code colors.

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

Rehearsal 7a: ~22 min (Claude Code with `--chrome`), 118 turns, $9.36, no questions, no real struggles.

Prompt 7b (Exams):

```text
Implement the Exams area of the UI (the Exams endpoints of the Web API; mockups 03 and 05 in spec/design/). Reuse the design system and base components; extend them where needed.

- Exemplary component tests with a mocked API: the percentage field; entering results (save sends all results, a value out of range blocks saving).
- Playwright e2e test: enter the results of an exam.
- Inspect the result in a real browser (e.g. Claude in Chrome) at 1920×1080, in light and dark mode. Compare it with the mockups and fix the differences.
- Keep tech-docs/ui.md up to date.
- Done when the QA script is green. Then commit directly to main.
```

Rehearsal 7b: ~19.5 min, 97 turns, $7.98 with a prompt of five lines, the agent reads `ui.md` and reuses tokens, classes, error handling, the confirm dialog and the field error helpers.

* Agent extends the API on its own if necessary
* Agent deviates from the `angular-developer` skill if necessary and documents findings in  `ui.md`

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

* Design readme vs. PNGs: the agent took exact values from the text and used the images for layout and behavior, then compared screenshots with the mockups and fixed real differences
* The gaps were filled consistently with the design system (settings, confirmation dialog, error display)
* Agent decisions worth reviewing: `rxResource` with the Observable-based generated client vs. `httpResource`; scope beyond the prompt (more tests than asked, loading placeholders) – good or too much?
* Browser in the loop: the agent resized, measured element positions, toggled dark mode and clicked through the workflows – verification a screenshot test would not do

Learnings:

* Agents can deal with design input: images for layout and behavior, a text readme for exact tokens and rules
* Design system first: reusable tokens, classes, components and services make the follow-up prompt short
* Designs are never complete → name the important gaps in the prompt and let the agent extrapolate from the design system
* Concrete design direction beats style adjectives: without design direction, Opus 5.5 falls back on a few default styles, and "avoid a generic AI look" only swaps one default for another ([Opus 5.5 prompting guide](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/prompting-claude-opus-5-5#frontend-design-defaults)). The design readme names exact tokens and rules (radius 0, no shadows, native controls), and the prompt says to extrapolate the gaps from it
* Close the loop visually: the agent inspects its own UI in a real browser and compares it with the mockups (Claude in Chrome, Playwright, …)
  * Opus 5.5 reads screenshots and mockups far more precisely than earlier models; full-resolution images and tools to zoom and measure still add accuracy ([Opus 5.5 prompting guide](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/prompting-claude-opus-5-5#tools-for-complex-visual-inputs)) → 100 % zoom, exact 1920×1080 screenshots, element positions measured in the browser
* Testable by design: generated client injected → mocked in component tests; e2e against a throwaway database filled by the existing demo data generator
* Discuss: Where do designs live (Figma, PNGs, text)? Who keeps design and code in sync? Visual regression tests vs. agent-in-the-browser?

Links:

* [Angular signal forms](https://angular.dev/guide/forms/signals/overview), [Resources](https://angular.dev/guide/signals/resource), [RxJS interop (`rxResource`)](https://angular.dev/ecosystem/rxjs-interop)
* [Claude in Chrome](https://code.claude.com/docs/en/chrome)
* [Playwright project dependencies (setup)](https://playwright.dev/docs/test-global-setup-teardown), [Playwright web server](https://playwright.dev/docs/test-webserver)
* [`<dialog>` element](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/dialog), [Fontsource Archivo](https://fontsource.org/fonts/archivo), [`@lucide/angular`](https://www.npmjs.com/package/@lucide/angular)


## Step 8 – CLI + skill: make the app usable for agents

Goal: a **command-line interface** for the existing application plus a **skill** that teaches agents how to use it. Afterwards, an agent does tasks a teacher would otherwise click through in the UI. Four prompts: 8a builds the CLI, 8b creates the skill (with the `skill-creator` skill from step 2), 8c and 8d are **no coding prompts** but requests of a teacher.

Why a CLI? In step 7 the agent operated the UI through the browser, which is slow, expensive, fragile. A CLI is the cheapest interface for agents: discoverable (`--help`), scriptable, composable (JSON → `jq`/Python), and humans can use it too. The skill adds what `--help` can't: which workflows exist, in which order, and where the sharp edges are.

Design decisions to discuss before the prompt:

* **Generated client (Kiota) instead of reusing the DTOs from `GraderLogic`:** the DTOs live next to the `DbContext`; referencing them would pull EF Core, SQLite (incl. native binaries) and Bogus into the tool and tempt an agent to "simplify" by calling the services or the database directly, bypassing the API. The OpenAPI spec is the contract; after the Angular client (step 3) it now feeds a second, generated client
* **Mirror the API:** one command per endpoint, but with short command names that are nice to use for humans too; ids as arguments, saving takes the complete document (same parent/child semantics as the API, see `web-api.md`), no convenience commands – the skill teaches the workflows instead
* **Generated client committed**, regenerating it is a dedicated step (not on every build) → rule in AGENTS.md: every structural API change includes regenerating the CLI client
* **.NET global tool**

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

Rehearsal 8a: ~19 min, 44 turns, $5.65, no questions. Most of the time goes into research (Kiota, the System.CommandLine release matching the .NET 11 SDK) and tests.

Before 8b: start the API in a second terminal (`dotnet run --project GraderApi`) and reset the demo data (Settings in the UI, or `grader demo-data replace --yes`).

Prompt 8b (skill):

```text
Use the skill-creator skill to create a project skill `grader-cli` (in .agents/skills/, linked into .claude/skills/ like the other skills) that teaches agents to do a teacher's everyday tasks in Grader with the CLI: e.g. setting up a class from a list of names, recording exam results, answering questions about grades.

- Focus on workflows and pitfalls, don't repeat --help (e.g. finding ids, saving complete documents, the API must be running, destructive commands only when the teacher asks for them).
- Keep the evaluation light: 2–3 realistic test tasks against the running development API with demo data, no benchmark or review viewer.
- Done when the QA script is green. Then commit directly to main.
```

Rehearsal 8b: ~6.5 min, $3.26.

* `.agents/skills/grader-cli/SKILL.md` + link in `.claude/skills/`: find ids by name (never from memory as ids change with every demo data reset), get → edit → save complete documents, `"id": null` for new students, missing result ≠ 0, destructive commands only on explicit request, averages without missing results and "say how many results an average is based on"

Prompt 8c (a teacher's request – run it in a **new session**):

```text
I'm taking over a new class this school year, 1D. Students: Lena Huber, Jonas Gruber, Sophie Wagner, Elias Bauer, Mia Steiner, Felix Moser, Hannah Hofer, Paul Leitner.

They wrote their first Mathematics test on 22 September 2026: Huber 78 %, Gruber 91.5 %, Wagner 64 %, Bauer 45 %, Steiner 88 %, Moser 72 %, Leitner 55 %. Hannah was sick.

Please enter all of this in Grader.
```

Rehearsal 8c: 40 s, $0.30.

Prompt 8d (a question, new session):

```text
Which three students had the lowest average result in Mathematics in school year 2025/26? Show their class and average.
```

Rehearsal 8d: 70 s, $0.29.

Demo:

```bash
dotnet run --project GraderApi                                          # terminal 1: API on :5100
dotnet pack GraderCli -c Release                                        # terminal 2
dotnet tool install --global --add-source GraderCli/nupkg GraderCli     # update: uninstall + install (same version)
grader --help                                                           # commands, output, exit codes
grader school-years list                                                # ids change with every demo data reset
grader classes list <year-id>
grader classes get <class-id> --json | jq '.students | length'
grader exams delete 1; echo $?                                          # --yes missing, exit code 2
grader classes get 99999; echo $?                                       # class-not-found, exit code 4
```

1. `grader --help`, then the same with `dnx` (runs without installing): `dnx GraderCli --add-source GraderCli/nupkg --yes -- --help`
2. `tech-docs/cli.md` + the API-change rule in AGENTS.md; `scripts/generate-cli-client.sh` as a QA check
3. `.agents/skills/grader-cli/SKILL.md`: what's in there that `--help` doesn't say
4. New session → prompt 8c → check class 1D in the UI (step 7)
5. New session → prompt 8d
6. Variation: paste a photo of a handwritten grade sheet instead of the text list

Things to show/discuss:

* **CLI + skill vs. MCP server:** the CLI costs no tokens until it is used, its output can be piped and filtered before it reaches the context, and humans and scripts use the same tool. MCP shines where there is no shell (e.g. claude.ai, mobile) or for remote/authenticated access (steps 9 and 10)
* Command names: without a hint, agents derive them 1:1 from tag + operation name (`grader class-management get-class`). One line in the prompt fixes it
* Permissions: allow `Bash(grader:*)` in `.claude/settings.json`, and the agent works without prompts, but mind destructive commands

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

Rehearsal 9a: ~22 min, 82 turns, $5.66, no questions, no manual checks against a running API.

Before 9b:

* Start the API in a second terminal (`dotnet run --project GraderApi`) and reset the demo data (Settings in the UI).
* Build the CLI (`dotnet build GraderCli`).
* Register the server in the client. Start the built dll, or `grader mcp` if the global tool is installed. In Claude Code (local scope, not in the repo; `claude mcp get grader` should show "Connected"):

  ```bash
  claude mcp add grader -- dotnet "$PWD/GraderCli/bin/Debug/net11.0/GraderCli.dll" mcp
  ```

* The rehearsal passed the server as a config file and disallowed the shell, so the agent could only use the MCP tools: `claude --mcp-config grader-mcp.json --strict-mcp-config --disallowedTools Bash`. The config file:

  ```json
  { "mcpServers": { "grader": { "type": "stdio", "command": "dotnet", "args": ["<repo>/GraderCli/bin/Debug/net11.0/GraderCli.dll", "mcp"] } } }
  ```

* MCPJam: `npx @mcpjam/inspector@latest` (the terminal install, because a stdio server is a local subprocess). Add a stdio server with command `dotnet` and arguments `<repo>/GraderCli/bin/Debug/net11.0/GraderCli.dll mcp`.

Prompt 9b (a teacher's request, new session, MCP tools only):

```text
I'm taking over a new class this school year, 1D. Students: Lena Huber, Jonas Gruber, Sophie Wagner, Elias Bauer, Mia Steiner, Felix Moser, Hannah Hofer, Paul Leitner.

They wrote their first Mathematics test on 22 September 2026: Huber 78 %, Gruber 91.5 %, Wagner 64 %, Bauer 45 %, Steiner 88 %, Moser 72 %, Leitner 55 %. Hannah was sick.

Please enter all of this in Grader.
```

Rehearsal 9b: 24 s, 9 turns, $0.27.

Prompt 9c (a question, new session, MCP tools only):

```text
Which three students had the lowest average result in Mathematics in school year 2025/26? Show their class and average.
```

Rehearsal 9c: 87 s, 57 turns, $0.80.

Demo (API running; `dotnet build GraderCli`; tested in zsh and bash):

```bash
mcp() {  # send JSON-RPC requests (one per argument) to `grader mcp`; keep stdin open until the answers are out
  { printf '%s\n' "$@"; sleep 2; } | dotnet GraderCli/bin/Debug/net11.0/GraderCli.dll mcp
}
META='"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}'
mcp '{"jsonrpc":"2.0","id":1,"method":"server/discover","params":{'"$META"'}}' | jq 'del(.result.instructions)'
mcp '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{'"$META"'}}' | jq -c '.result.tools[] | {name, annotations}'
mcp '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"classes_get","arguments":{"classId":99999},'"$META"'}}' | jq .result
echo '{"name": "9Z", "students": [{"id": null, "firstName": "Anna", "lastName": "Local"}]}' > /tmp/class-9z.json
mcp '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"classes_create","arguments":{"schoolYearId":<year-id>,"file":"/tmp/class-9z.json"},'"$META"'}}' | jq -r '.result.content[0].text'
mcp '{"jsonrpc":"2.0","id":5,"method":"tools/list","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2025-11-25"}}}' | jq -c .error
npx -y @modelcontextprotocol/inspector --cli dotnet "$PWD/GraderCli/bin/Debug/net11.0/GraderCli.dll" mcp --method tools/call --tool-name classes_get --tool-arg classId=99999
```

1. `dotnet GraderCli/bin/Debug/net11.0/GraderCli.dll mcp --help` (with the global tool: `grader mcp --help`)
2. Raw JSON-RPC, one request after another:
   * `server/discover` as the first request, with no `initialize`: supported versions, capabilities, `serverInfo` in `_meta`
   * `tools/list`: 13 tools with their annotations
   * a tool error with `isError` and the `code`
   * `classes_create` from a **local file**: take `<year-id>` from `school_years_list` (ids change with every reset)
   * an old protocol version in `_meta` → error `-32022` ("requires the initialize handshake")
3. MCPJam (or the MCP Inspector): browse tools, schemas (`document` vs. `file`) and annotations, and call a tool
4. AGENTS.md: the llms.txt entry. Then open https://modelcontextprotocol.io/llms.txt in the browser
5. New session with only the MCP server → prompt 9b → check class 1D in the UI
6. New session → prompt 9c

Things to show/discuss:

* **stdio MCP = fancy CLI:** same process, same use case definitions, same API calls; only the interface differs (JSON-RPC over stdin/stdout instead of argv, stdout and exit code). stdout belongs to the protocol, so a single stray `Console.WriteLine` breaks the server
* **Gotcha when piping by hand:** the server stops at stdin EOF and drops requests still in flight. `printf '…' | grader mcp` prints nothing, hence the `sleep 2` in the demo
* **Local resources:** `file` reads a file on the user's machine. The same pattern reaches the intranet or IoT devices, which a remote server (step 10) can't
* **Annotations are hints for the client** (e.g. ask before a destructive tool): the confirmation moves from `--yes` into the client
* **MCP vs. CLI + skill** (talking point only): in 9c there was no shell, so 40 exams went through the context and the averages were computed by the model (52 tool calls, $0.80). In 8d the CLI output went through a script instead, and only the result reached the model ($0.29). MCP shines where there is no shell
* **Spec 2026-07-28 in the raw JSON:** `_meta` on every request, `server/discover`, `resultType`, `ttlMs`/`cacheScope`
* **llms.txt:** the index (~350 lines) vs. `llms-full.txt` (~2.5 MB, far too big for a context window). The agent fetched the index with `curl` and then only the pages it needed – the spec revision from July 2026 was implemented without a single question

Learnings:

* stdio MCP = a CLI with a JSON-RPC interface on stdin/stdout; it runs as the local user and can use local resources
* Tool annotations and tool errors are the MCP counterparts of `--yes`, exit codes and stderr
* One command list feeds both interfaces: no second implementation, no drift
* llms.txt is an index of Markdown pages for agents. Current docs beat training data, and one line in AGENTS.md makes it stick

Links:

* [MCP llms.txt](https://modelcontextprotocol.io/llms.txt), [llms.txt proposal](https://llmstxt.org/)
* MCP spec 2026-07-28: [Key changes](https://modelcontextprotocol.io/specification/2026-07-28/changelog), [Versioning](https://modelcontextprotocol.io/specification/2026-07-28/basic/versioning), [stdio transport](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/stdio), [Discovery](https://modelcontextprotocol.io/specification/2026-07-28/server/discover), [Tools](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)
* [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), [ModelContextProtocol.Core on NuGet](https://www.nuget.org/packages/ModelContextProtocol.Core)
* [MCP Inspector](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector), [MCPJam](https://www.mcpjam.com/) ([GitHub](https://github.com/MCPJam/inspector))
* [Claude Code MCP](https://code.claude.com/docs/en/mcp)

## Step 10 – Streamable HTTP MCP server in the backend

Goal: the Web API itself becomes an MCP server. `POST /mcp` offers the same tools as `grader mcp` over **Streamable HTTP**. MCP over HTTP is nothing more than another protocol for web APIs: JSON-RPC in the body of a plain POST. Architecturally, it is **hexagonal** (ports and adapters): the layering from step 6 (thin endpoints → use-case services in GraderLogic) lets `/mcp` be a thin server-side adapter over the same services, right next to the REST endpoints. The demo runs with MCPJam and raw `curl`.

stdio (step 9) vs. Streamable HTTP (step 10):

| | stdio: `grader mcp` | Streamable HTTP: `/mcp` |
|---|---|---|
| What it is | a process the client starts; JSON-RPC over stdin/stdout | an HTTP endpoint; one POST per JSON-RPC request |
| Users | one local user, the one who started it | many users over the network |
| Reach | local resources: files (`file`), intranet, IoT | only what the server can reach; deployed centrally |
| Installation | .NET SDK + global tool (or the built dll) on every machine | none: a URL in the client |
| Calls | the Web API over HTTP (Kiota client) | the GraderLogic services directly, like the endpoints |
| Auth | the local user's credentials (here: none) | none yet; OAuth in a later step |

Design decisions to discuss before the prompt:

* **Server-side adapter:** the tools call the GraderLogic services directly, like the REST endpoints do. They don't use HTTP, Kiota or the CLI. A new protocol costs one thin layer, not a second implementation of the business logic
* **Same tools as `grader mcp`:** names, descriptions and annotations are the same, and there is no `file` parameter because the server can't read the user's disk. An MCP client config can be swapped from `grader mcp` to `http://localhost:5100/mcp` and the same tools appear
* **Only spec revision 2026-07-28 (stateless):** no sessions, no GET stream, every request carries protocol version and client capabilities in `_meta`, plus the `Mcp-Method`/`Mcp-Name` headers. No extra code for older revisions
* **Tools are plain methods:** `[Description]` attributes, grouped by use-case area, with no MCP types and no MCP logic. Step 11 turns them into a shared tool catalog (`AIFunction`) for MCP and for an in-app agent
* **llms.txt is not in the prompt:** does the AGENTS.md rule from step 9 make the agent use it anyway?
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

Rehearsal 10: ~13.5 min, 79 turns, $5.27, no questions.

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
mcp tools/call school_years_list '{}' | jq -r '.result.content[0].text' | jq -c .
Y=$(mcp tools/call school_years_list '{}' | jq -r '.result.content[0].text' | jq -r '.[] | select(.name=="2025/26") | .id')
mcp tools/call classes_list '{"schoolYearId": '"$Y"'}' | jq -r '.result.content[0].text' | jq -c '.[]'
mcp tools/call classes_get '{"classId": 99999}' | jq -c .result
mcp tools/call classes_create '{"schoolYearId": '"$Y"', "document": {"name": "1A", "students": []}}' | jq -r '.result.content[0].text'
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
3. `GraderApi/ClassManagement/ClassManagementTools.cs` next to the class management endpoints: two thin adapters over the same service. Then `GraderApi/Mcp/` (the only MCP-specific code, ~200 lines)
4. MCPJam (or the MCP Inspector) with `http://localhost:5100/mcp`: browse tools and annotations, call a tool
5. The swap: the stdio config from step 9 → the HTTP config (or `claude mcp add --transport http`), then `/mcp` in Claude Code: the same tools. Ask something read-only
6. `tech-docs/mcp.md`: both servers, how they differ, the gotchas (no auth, `GET /mcp` outside Development)

Things to show/discuss:

* **MCP over HTTP = another web API protocol:** every request is a POST with JSON-RPC in the body, and the headers (`Mcp-Method`, `Mcp-Name`) mirror the body so gateways can route and inspect without parsing it. Stateless 2026-07-28 needs no sticky sessions, so any instance behind a load balancer can answer any request
* **Hexagonal architecture pays off:** REST endpoints and MCP tools are two adapters over the same use-case services. Business rules, error codes and validation are shared. The MCP-specific code (`GraderApi/Mcp/`) is ~200 lines
* **llms.txt rule effect:** the prompt didn't mention it. The agent used llms.txt only when it needed a spec detail the SDK doesn't answer (an earlier rehearsal: `GET /mcp`, `Origin`); in the last rehearsal the cloned SDK docs and source answered everything. Both rules came from AGENTS.md – the agent picks the source that fits
* **No installation for non-technical users** (talking point, no live demo): a remote MCP server is just a URL in the client (e.g. VS Code, Claude Desktop, Cursor). Real-world examples: the [Microsoft Learn MCP server](https://learn.microsoft.com/training/support/mcp) (`https://learn.microsoft.com/api/mcp`, no sign-in), the [remote GitHub MCP server](https://github.com/github/github-mcp-server) (`https://api.githubcopilot.com/mcp/`, OAuth), and the [MCP Registry](https://registry.modelcontextprotocol.io/). Compare this with step 9: .NET SDK, global tool, local config
* **Security:**
  * `/mcp` has **no auth** and no `Origin` validation (the spec's DNS rebinding MUST). Anyone who can reach the port can call `demo_data_replace`. Localhost only, never expose it publicly
  * OAuth for remote MCP servers: see "What's next" at the end
* **stdio vs. HTTP:** see the table above. stdio for local resources and one user; HTTP for central deployment, many users and no installation
* **Tool granularity:** the tools mirror the API 1:1. In 9c that cost 52 tool calls (10× `exams_list`, 40× `exams_get`). Remote MCP has no shell to post-process results, so task-oriented tools (e.g. "averages per student and course") would pay off. The API shape is not automatically a good tool shape
* **Duplication CLI ↔ API:** descriptions and instructions are copied, and a test guards names, descriptions and annotations. Parameter descriptions are not covered. Step 11 turns the API's tools into one shared catalog for MCP and the in-app agent
* **Protocol eras in practice:**
  * Claude Code 2.1.283 speaks 2026-07-28 (`server/discover`)
  * the MCP Inspector defaults to the legacy `initialize`, and the SDK still answers it
* Next steps: **MCP Apps**

Learnings:

* Hexagonal architecture pays off: a new protocol is a thin adapter over the same logic
* stdio MCP = a local fancy CLI; HTTP MCP = a remote web API protocol (central deployment, no installation, needs auth)
* Keep tools plain (methods + descriptions) and the protocol in a thin layer. That makes the tools reusable beyond MCP
* Guard duplicated contracts with a test, or better, don't duplicate them (step 11)

Next (step 11): an in-app agent as another "UI", fed by the same tools – turned into a shared `AIFunction` catalog first.

Links:

* MCP spec 2026-07-28: [Streamable HTTP](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http), [Transports](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports), [Key changes](https://modelcontextprotocol.io/specification/2026-07-28/changelog), [Versioning](https://modelcontextprotocol.io/specification/2026-07-28/basic/versioning), [Discovery](https://modelcontextprotocol.io/specification/2026-07-28/server/discover), [Tools](https://modelcontextprotocol.io/specification/2026-07-28/server/tools), [Authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)
* [MCP Security Best Practices](https://modelcontextprotocol.io/docs/2026-07-28/tutorials/security/security_best_practices), [Connect to remote MCP servers](https://modelcontextprotocol.io/docs/2026-07-28/develop/connect-remote-servers), [MCP llms.txt](https://modelcontextprotocol.io/llms.txt)
* [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), [ModelContextProtocol.AspNetCore on NuGet](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
* MCP Inspector: [CLI](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector/cli), [Protocol eras](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector/protocol-eras); [MCPJam](https://www.mcpjam.com/) ([GitHub](https://github.com/MCPJam/inspector)); [Claude Code MCP](https://code.claude.com/docs/en/mcp)
* [Hexagonal architecture (Alistair Cockburn)](https://alistair.cockburn.us/hexagonal-architecture/)
* [MCP Apps](https://modelcontextprotocol.io/extensions/apps/overview)
* Step 11: [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai), [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/), [AG-UI](https://docs.ag-ui.com/)

## Step 11 – In-app AI assistant (Microsoft Agent Framework + AG-UI)

Goal: an AI assistant **inside** Grader: a teacher asks questions in a chat, an agent answers with the tools of step 10. The agent is one more adapter of the same hexagon: REST (step 6), CLI (step 8), stdio MCP (step 9), HTTP MCP (step 10), and now an agent behind the **AG-UI** protocol with a lightweight self-built Angular chat. Three prompts: 11a refactors the tools into a shared catalog, 11b adds the agent and the AG-UI endpoint, 11c the Assistant area in the UI.

Design decisions to discuss before the prompts:

* **One tool catalog, several adapters:** the tools become [`AIFunction`s (Microsoft.Extensions.AI)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.aifunction), the common abstraction of the MCP C# SDK and the Microsoft Agent Framework. The MCP server adapts the catalog (annotations, errors), the agent gets the same functions
* **Read-only agent:** the agent only gets the read tools; write tools stay MCP-only. Approval of destructive tools (`ApprovalRequiredAIFunction` → an AG-UI interrupt the UI must confirm) is a talking point, not part of the demo
* **AG-UI:** an open event protocol between agent backend and UI (`RUN_STARTED`, `TEXT_MESSAGE_CONTENT`, `TOOL_CALL_START/ARGS/END/RESULT`, `RUN_FINISHED` as server-sent events). The client sends the full conversation with every run, so the backend is stateless
* **No chat UI framework:** `@ag-ui/client` + `marked` + CSS from the design system. [CopilotKit](https://www.copilotkit.ai/) would offer ready-made chat components (React, and meanwhile also Angular)
* **LLM via OpenRouter** (`z-ai/glm-5.3-flash`, cheap and fast) over the OpenAI-compatible **Chat Completions** API: the most straightforward code. OpenRouter's Responses API is stateless and rejects `previous_response_id`, which the Agent Framework's Responses client relies on by default. The key comes from configuration (`OpenRouter:ApiKey`), never from the repo
* **llms.txt again:** 11b has the agent research AG-UI with https://docs.ag-ui.com/llms.txt – the second llms.txt after MCP (step 9)
* **Designs:** mockups 06–08 + `assistant.md` (chat layout, tool call rows with Running…/Done, the markdown subset, the composer)

Prompt 11a (refactoring):

```text
Prepare an in-app AI agent with a refactoring: the tools of the Web API's MCP server become a shared tool catalog based on Microsoft.Extensions.AI (`AIFunction`), independent of MCP. The MCP server at /mcp uses it now, the agent of the next session will too.

- Each tool is defined in one place: name, description, parameters and traits (read-only, destructive). The MCP server only adapts the catalog to MCP (annotations, errors) and has no tool definitions of its own.
- Pure refactoring: /mcp offers and runs exactly the same tools as before, the existing tests stay green.
- Exemplary test: a read-only tool of the catalog invoked as `AIFunction`, without MCP.
- Update tech-docs/mcp.md.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
```

Rehearsal 11a: ~6.5 min, 35 turns, $2.19, no questions.

Before 11b:

* Copy the assistant design from the workshop materials and **commit it** (a rehearsal agent left uncommitted design files alone, "they looked like your work in progress"):

  ```bash
  cp <workshop-materials>/design-assistant/*.png spec/design/
  cp <workshop-materials>/design-assistant/readme.md spec/design/assistant.md
  git add spec/design && git commit -m "Add assistant design"
  ```

* Provide the OpenRouter key: `dotnet user-secrets set OpenRouter:ApiKey <key> --project GraderApi` (the agent enables user secrets in 11b – run it afterwards) or the environment variable `OpenRouter__ApiKey`

Prompt 11b (agent + AG-UI endpoint):

```text
Add an AI assistant to the Web API: an agent built with Microsoft Agent Framework that answers a teacher's questions about classes, exams and results. The web UI will talk to it via the AG-UI protocol.

- Research AG-UI with https://docs.ag-ui.com/llms.txt and add it to "Researching Documentation" in AGENTS.md.
- The agent uses the read-only tools of the shared tool catalog; it never changes data.
- LLM via OpenRouter's OpenAI-compatible Chat Completions API. API key from the configuration value `OpenRouter:ApiKey` (user secrets or environment variable, never in the repo), model `z-ai/glm-5.3-flash` in appsettings.json. Without a key, the app still starts and only the assistant reports an error.
- Instructions: short answers in the markdown subset of spec/design/assistant.md; the newest school year unless the teacher names one; never invent data.
- Exemplary test: an AG-UI run against the Web API in memory with a fake chat client that calls a tool; the tool call, its result and the answer appear in the event stream.
- New tech docs article tech-docs/assistant.md (architecture, configuration, running it); update AGENTS.md and testing.md.
- Done when the tests and the QA script are green (no manual checks against a running API or a real LLM). Then commit directly to main.
```

Rehearsal 11b: ~10 min, 41 turns, $3.45, no questions.

Before 11c: API with the key running (second terminal) and demo data reset.

Prompt 11c (Assistant area):

```text
Add the Assistant area to the web UI: a chat with the AI assistant of the Web API (mockups 06–08 and assistant.md in spec/design/).

- Talk to the AG-UI endpoint with `@ag-ui/client` and render the markdown answers with `marked`; no chat UI framework. Answer text and tool calls appear while they stream in.
- The conversation lasts until the teacher leaves the Assistant area.
- Exemplary component test with a mocked agent: a tool call shows "Running…", then "Done", then the answer renders as markdown.
- Inspect the result in a real browser (e.g. Claude in Chrome) at 1920×1080, in light and dark mode, with a real question to the assistant. Compare it with the mockups and fix the differences.
- Keep tech-docs/ui.md up to date.
- Done when the QA script is green. Then commit directly to main.
```

Rehearsal 11c: ~20 min (Claude Code with `--chrome`), 123 turns, $8.11, no questions.

Demo:

```bash
dotnet run --project GraderApi                  # terminal 1: API incl. assistant (key from user secrets)
cd GraderWeb && npm start                       # terminal 2: http://localhost:4200/assistant
curl -N http://localhost:5100/api/assistant -H 'Content-Type: application/json' -d '{
  "threadId": "t1", "runId": "r1", "tools": [], "context": [], "forwardedProps": {},
  "messages": [{ "id": "m1", "role": "user", "content": "Which classes are there?" }] }'
```

1. `curl -N`: the raw AG-UI event stream (`RUN_STARTED`, `TOOL_CALL_*`, `TEXT_MESSAGE_CONTENT` deltas, `RUN_FINISHED`)
2. Assistant area: "Which students in 2B scored below 70 % in the latest Computer Science exam?" → tool call rows (unfold one: arguments + result), then the table; follow-up "Compare this with 2A."
3. "Please delete class 2B." → the agent can't (read-only tools) and points to the Classes page
4. Code: `GraderApi/Tools/ToolCatalog.cs` (one catalog), `GraderApi/Mcp/McpServerSetup.cs` and `GraderApi/Assistant/AssistantSetup.cs` (two adapters)
5. `spec/design/assistant.md` vs. `GraderWeb/src/app/assistant/markdown.ts`: the design's markdown subset, enforced in code and in the agent's instructions

Things to show/discuss:

* **One catalog, several adapters:** the same `AIFunction`s feed the MCP server (external agents) and the in-app agent (AG-UI). A new tool is one method with a description
* **AG-UI:** what REST is for CRUD, AG-UI is for agents and UIs – a standard event stream instead of a home-grown WebSocket protocol. The mismatch between the .NET SDK 0.x and the 1.0 client shows how young the ecosystem is (preview packages, renamed methods like `MapAGUI` → `MapAGUIServer`)
* **Build vs. buy the chat UI:** self-built = ~800 lines of Angular (plus ~300 lines of tests) and full control over the design; CopilotKit (React, meanwhile Angular) = ready-made components, generative UI, shared state, human-in-the-loop out of the box
* **Human in the loop:** write tools with `ApprovalRequiredAIFunction` → the run stops with an interrupt, the UI asks the teacher, the next run continues. The step 9/10 annotations (read-only, destructive) are the information you need for that decision
* **Tool granularity again:** "Which three students had the lowest average result in Mathematics in 2025/26?" needs ~50 tool calls through 1:1 API tools. A small model like `glm-5.3-flash` got that question wrong in an earlier rehearsal (it lost track across 40 exam results) – task-oriented tools ("averages per student and course") would help more than a bigger model
* **Model choice and cost:** a flash model is fast and cheap for the typical questions; the agent's instructions and read-only tools limit the damage when it's wrong. "Never invent data" in the instructions is not a guarantee
* **llms.txt as a pattern:** MCP (step 9) and AG-UI (11b) – the agent reads the index first and fetches only the pages it needs

Learnings:

* The hexagon holds: an in-app agent is another adapter over the same tools and use-case services, like REST, CLI and MCP
* Share tools, not protocols: `AIFunction` is the common denominator of MCP and the Agent Framework
* AG-UI standardizes the agent ↔ UI stream; a lightweight client is enough for a workshop-sized chat
* Read-only first; destructive tools need a human in the loop (AG-UI interrupts)
* Cost: 11a ~$2.20, 11b ~$3.50, 11c ~$8.10; the assistant itself costs fractions of a cent per question with a flash model

Links:

* [AG-UI docs](https://docs.ag-ui.com/) ([llms.txt](https://docs.ag-ui.com/llms.txt)), [`@ag-ui/client`](https://www.npmjs.com/package/@ag-ui/client), [marked](https://marked.js.org/)
* [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/), [AG-UI integration](https://learn.microsoft.com/agent-framework/integrations/by-component/ui/ag-ui/getting-started), [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
* [OpenRouter](https://openrouter.ai/docs) ([llms.txt](https://openrouter.ai/docs/llms.txt)), [CopilotKit](https://docs.copilotkit.ai/)

## What's next – authentication and authorization (experiments at home)

Grader has no login yet: every endpoint, `/mcp` and the assistant are open to anyone who can reach the API. That was fine for a workshop on localhost, and it's the obvious next topic. It isn't part of the workshop day and it isn't rehearsed; here are ideas for your own experiments, in a sensible order.

**1. Install the `keycloak-auth` skill and let it provision Keycloak.** The workshop material contains the skill [.agents/skills/keycloak-auth](.agents/skills/keycloak-auth/). Copy it into the Grader repository (`.agents/skills/`, linked into `.claude/skills/` like the other skills), it is all you need. It bundles a Docker Compose file and a realm import for Keycloak 26.7.4 (realm `workshop`, user `demo`/`demo`, scope `mcp:tools`, experimental `cimd` and `resource-indicators` features), copies them into the project and starts Keycloak. It also points the agent to the MCP authorization spec pages (via llms.txt) and to the gotchas that cost time in the preparation: keeping the resource URL aligned in several realm settings, PKCE as its own policy, CIMD trust for MCPJam, refresh rotation, and that a realm import doesn't update an existing realm. A prompt can be as short as "Use the keycloak-auth skill to start Keycloak for Grader". The interesting part for MCP: MCPJam isn't registered in Keycloak anywhere. It identifies itself with a Client ID Metadata Document (CIMD) that Keycloak fetches from `www.mcpjam.com`, and resource indicators bind the tokens to exactly one MCP server. The setup runs on plain HTTP, which the spec only tolerates for local development.

**2. Protect Grader's `/mcp`.** Let the agent add what the MCP authorization spec (2026-07-28) asks of a server: a `401` with a `WWW-Authenticate` header pointing to the protected resource metadata (RFC 9728), JWT validation (issuer, audience, expiry), a required scope and a check of the browser `Origin` (which also answers the question an agent asked in a step 10 rehearsal). In ASP.NET Core that's `AddJwtBearer`, the MCP SDK's `AddMcp` authentication handler for the resource metadata and `MapMcp("/mcp").RequireAuthorization(...)`. The resource URL is the part that is easy to get wrong: the skill's realm uses `http://localhost:7071/mcp`, Grader's is `http://localhost:5100/mcp` – the skill knows which realm settings must change together. Test it with MCPJam (CIMD, scope `mcp:tools`). A good prompt also asks for a tech docs article `auth.md` and an exemplary test with a self-issued test token, so the tests don't need a running Keycloak.

**3. Protect the web app and the REST API.** The Angular app needs its own login: a public client in the realm (e.g. `grader-web`) with authorization code + PKCE, and GraderApi validates the tokens on `/api/*` and on the assistant endpoint. Two options worth comparing: the SPA holds the tokens itself (e.g. with an OIDC library for Angular), or a backend-for-frontend where GraderApi runs the login and the browser only gets a secure cookie. The latter keeps tokens out of the browser and is today's recommendation for SPAs. The REST API and `/mcp` are different resources: think about whether one token may be used for both or whether each gets its own audience.

**4. The CLI and `grader mcp`.** The MCP authorization spec deliberately doesn't apply to stdio: a stdio server gets its credentials from the environment. So it's the CLI that has to log in when it calls the Web API. The OAuth device authorization grant (`grader login` shows a code, you confirm it in the browser, the CLI stores the token) fits command-line tools well, and Keycloak supports it. The Kiota client can attach the token through its authentication provider.

**5. From authentication to authorization.** Once users are known, the traits of the shared tool catalog from step 11 pay off again: read-only tools could require `grader:read`, destructive ones `grader:write`, and `tools/list` could show each user only the tools they may call. The in-app agent calls the tools on behalf of the logged-in teacher, so it must never be able to do more than that teacher. A further step, "teachers only see their own classes", needs a change to the data model and is a good exercise in spec-driven development (step 5): spec first, then the prompt.

**How to approach it with an agent:** the same way as in the workshop, and with the skill as the source of truth for the Keycloak side: research first (the MCP authorization pages via [llms.txt](https://modelcontextprotocol.io/llms.txt) – the skill links the relevant ones), write down the decisions in a tech docs article, then give the agent short prompts with exemplary tests, one step at a time.

Links:

* MCP 2026-07-28: [Authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization), [Security best practices](https://modelcontextprotocol.io/docs/2026-07-28/tutorials/security/security_best_practices)
* [keycloak-auth skill](.agents/skills/keycloak-auth/SKILL.md), [Keycloak MCP authorization server guide](https://github.com/keycloak/keycloak/blob/26.7.4/docs/guides/securing-apps/mcp-authz-server.adoc), [C# SDK ProtectedMcpServer sample](https://github.com/modelcontextprotocol/csharp-sdk/tree/v2.2.0/samples/ProtectedMcpServer), [MCPJam](https://www.mcpjam.com/)
* RFCs: [9728 Protected Resource Metadata](https://www.rfc-editor.org/rfc/rfc9728), [8707 Resource Indicators](https://www.rfc-editor.org/rfc/rfc8707), [8628 Device Authorization Grant](https://www.rfc-editor.org/rfc/rfc8628); [OAuth 2.0 for Browser-Based Applications](https://datatracker.ietf.org/doc/draft-ietf-oauth-browser-based-apps/) (backend-for-frontend)
