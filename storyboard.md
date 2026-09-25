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
* `GraderWeb` – Angular (zoneless, no SSR, plain CSS, standalone, strict, separate template/style files, routing, Vitest); Playwright + ng-openapi installed but not configured yet

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

- GraderLogic: Math class with an Add method behind an interface (for mocking). Negative parameters are not allowed → custom exception (to test error handling).
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

* Interface + implementation for `Add` + `NegativeParameterException` in GraderLogic (run 1: `IMath`/`Math`; run 2: `IMathOperations`/`MathOperations`, because `Math` clashes with `System.Math`)
* `GET /api/.../add?a=&b=` → `{ result }` or `400` ProblemDetails; endpoint handler = public static method with typed results (`Results<Ok<T>, ProblemHttpResult>`) → unit-testable without HTTP, typed results feed OpenAPI
* OpenAPI spec written to a file in `GraderApi` on build; `npm run generate:api` (ng-openapi) generates the client into `GraderWeb/src/app/api`; spec + client are committed
* `ng serve` proxies `/api` → backend; `ng build` outputs into `GraderApi/wwwroot`, backend serves it incl. SPA fallback
* Calculator component with signal forms
* Tests: ~10 .NET unit + integration (`WebApplicationFactory<Program>`), 2–3 Vitest component, 2 Playwright e2e (against the production setup: Angular build served by GraderApi)
* `tech-docs/testing.md`, `tech-docs/api-client.md`, `AGENTS.md` with run commands + tech docs overview

Things to show/discuss:

* Walk through `tech-docs/*.md`: decisions + gotchas + pointers, no code copies
* Agent decisions worth reviewing: committed generated client (pro: frontend builds without .NET; con: must regenerate), strict JSON number handling (otherwise spec types numbers as `integer | string`), `Math` vs. `System.Math` name clash
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
