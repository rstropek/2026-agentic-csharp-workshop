# BASTA! 2026 – C# Workshop: Neuigkeiten und moderne Architekturkonzepte

Material for the full-day workshop [C# Workshop – Neuigkeiten und moderne Architekturkonzepte](https://basta.net/net-framework-c/csharp-dotnet-mediator-pattern-workshop/) at BASTA! 2026.

How does software development change when AI agents don't just complete code, but actively take part in analysis, implementation, refactoring and testing? This workshop is a hands-on deep dive into agentic programming for C# developers: how a C# project should be set up so that coding agents can work with it effectively – project structure and conventions, testability, prompts, workflows and the right tooling strategy – plus a look at the new **Microsoft Agent Framework** and what it enables for modern .NET applications. It is for developers who don't just want to talk about AI coding assistants, but want to understand how agentic development works in practice.

Over the day, we build an end-to-end AI application **together with a coding agent** (Claude Code, GitHub Copilot, Codex, …): **Grader**, a small grading app for teachers. Teachers manage classes and students per school year and record exam results (percentages) per course; at the end, an AI assistant answers their questions. The stack is current: **.NET 11** (ASP.NET Core minimal APIs, EF Core with SQLite, xUnit v3, Microsoft Agent Framework) and **Angular 22** (signals, signal forms, zoneless).

The app is the vehicle. The topic is how to work with coding agents on a real .NET codebase:

* **Deterministic tools first:** scripts create the skeleton; the agent writes code, not project files
* **Up-to-date knowledge:** docs skills, `AGENTS.md`, llms.txt – .NET 11 and Angular 22 are newer than the model's training data
* **Feedback loops:** tests and a QA script (analyzers, formatters, linters, e2e tests) that the agent runs itself
* **Artifacts instead of long prompts:** spec = *what*, tech docs = *how*, prompt = *task*
* **Architecture first:** a human-written architecture article, followed by the agent
* **Design handoff:** mockups + a design readme, the agent checks its UI in a real browser
* **Make the app usable for agents:** a CLI + skill, a stdio MCP server, an HTTP MCP server
* **Another UI:** an in-app AI assistant (Microsoft Agent Framework + AG-UI) over the same tools (hexagonal architecture)

## Where to start

Open **[storyboard.md](storyboard.md)**. It is the script of the day: for every step the script or prompt to copy, the rehearsal time and cost, findings, a demo, discussion points, learnings and links. A timing table at the top shows which prompts run live, while discussing or during a break.

## Repository content

| Path | What it is |
| --- | --- |
| [storyboard.md](storyboard.md) | The workshop script, steps 1–11 plus "What's next" (auth) |
| [2026-09-storyboard_erdiagram.md](2026-09-storyboard_erdiagram.md) | Spec of the data model (Mermaid ER diagram + rules), input for step 5 |
| [web-api.md](web-api.md) | Human-written architecture article for the Web API, input for step 6 |
| [design/](design/) | Designer handoff for the web UI (mockups 00–05 + readme), input for step 7 |
| [design-assistant/](design-assistant/) | Designs of the AI assistant chat (mockups 06–08 + readme), input for step 11 |
| [rehearsal/](rehearsal/) | Every prompt of the storyboard, rehearsed with `claude -p`: prompts, scripts, the Grader repository after the last step (`snapshot/`, one commit per step), agent logs and `grader.bundle` (all steps as git tags – the fallback if something goes wrong on stage). See [rehearsal/README.md](rehearsal/README.md) |
| [.agents/skills/keycloak-auth/](.agents/skills/keycloak-auth/) | Agent skill for adding authentication at home ("What's next" in the storyboard): provisions a local Keycloak (Docker Compose + realm import) and explains MCP authorization and its gotchas. Attendees copy it into their Grader repository |

The Grader app itself is not in this repository as source you edit: attendees create it from scratch with the storyboard. The rehearsal's result is in [rehearsal/snapshot/](rehearsal/snapshot/) (latest step) and [rehearsal/grader.bundle](rehearsal/grader.bundle) (every step).

## Steps at a glance

| Step | Topic |
| --- | --- |
| 1 | App skeleton with deterministic tools (`dotnet new`, `ng new`) |
| 2 | Docs skills + `AGENTS.md` |
| 3 | Connect the projects: API, OpenAPI, generated Angular client, tests on every level |
| 4 | QA baseline: analyzers, formatters, linters, one QA script |
| 5 | Database, spec-driven (EF Core + SQLite, demo data) |
| 6 | Web API, architecture first |
| 7 | UI from a design handoff, checked in the browser (Claude in Chrome) |
| 8 | CLI (.NET global tool) + skill; the agent does a teacher's tasks |
| 9 | stdio MCP server in the CLI |
| 10 | Streamable HTTP MCP server in the backend |
| 11 | In-app AI assistant: shared tool catalog, Microsoft Agent Framework, AG-UI, Angular chat |
| What's next | Authentication for the web app and MCP (Keycloak) |

## Prerequisites

.NET 11 SDK (RC 1 or newer), Node.js 24 LTS, Git, bash/zsh, a coding agent that supports `AGENTS.md` and skills, Chrome with Claude in Chrome (steps 7 and 11) and an OpenRouter API key (step 11). Details are in the storyboard.
