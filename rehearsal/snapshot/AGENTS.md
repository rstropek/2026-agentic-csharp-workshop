# AGENTS.md

## Overview

Grader is a grading app for teachers and students. The repository contains the following projects:

- `GraderApi`: ASP.NET Core web API (OpenAPI with Scalar UI) that serves the backend.
- `GraderLogic`: class library with the core grading domain logic, used by the API.
- `GraderTests`: xUnit v3 tests (Microsoft Testing Platform, NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`) for the logic and API.
- `GraderWeb`: Angular frontend (see `GraderWeb/AGENTS.md` for Angular-specific guidelines).

Hosting: in development, the Angular dev server serves the frontend and proxies `/api` to the backend. In all other environments, the backend serves the built Angular app (from `GraderApi/wwwroot`) and the API from the same origin.

## Running the App

Development (two terminals, open http://localhost:4200):

```bash
dotnet run --project GraderApi --launch-profile http   # API on http://localhost:5100, Scalar UI at /scalar
cd GraderWeb && npm start                              # Angular dev server on http://localhost:4200, proxies /api
```

Production-like (backend serves the Angular app, open http://localhost:5200):

```bash
cd GraderWeb && npm run build && cd ..                 # builds Angular into GraderApi/wwwroot
dotnet publish GraderApi -c Release -o GraderApi/bin/publish
cd GraderApi/bin/publish && ASPNETCORE_URLS=http://localhost:5200 dotnet GraderApi.dll
```

Use `npm start`/`npm run build`/`npm test` instead of the plain `ng` commands: the npm pre-scripts regenerate the API client first. Test commands: see `tech-docs/testing.md`.

## Technology Versions

- .NET, Angular and other technologies used here might be newer than you expect. Do not rely on your training data for APIs; verify with current docs.
- We use .NET 11 (currently a release candidate, see `global.json`). NuGet packages that ship with .NET must match the SDK's major version, so prerelease versions are fine for them. Use the latest stable version for all other packages.

## Researching Documentation

Use the installed skills to research up-to-date documentation. Priority:

1. Microsoft technologies incl. .NET, ASP.NET Core, xUnit: Microsoft Learn CLI (not MCP) from the `microsoft-docs` skill. It runs as a sub-agent: always pass your question as the skill's argument.
2. Angular: `angular-developer` skill.
3. Other technologies (incl. NSubstitute): `ctx7` CLI (not MCP) from the `find-docs` skill.
4. You may clone/pull GitHub repositories of used open source libraries into `~/github/<repo>` if you need to inspect their code or docs included in the repo.

## Tech Docs

`tech-docs/` contains project-specific technical documentation. Agents are the primary audience. Read the relevant articles before working in an area.

- [testing.md](tech-docs/testing.md): testing strategy per layer (.NET unit/integration, Angular component, Playwright E2E), test commands, and testing gotchas.
- [api-client.md](tech-docs/api-client.md): how the OpenAPI spec is generated at build time and turned into the Angular API client, how to design endpoints for a good client, and generation commands.

Maintain the tech docs as part of every change:

- Update an article when you change the approach, principles or design decisions it describes, or when you discover a new gotcha. Add a new article for new cross-cutting topics and list it above (one line each).
- Do NOT repeat code or anything an agent can easily find out by reading the code (signatures, file contents, config values). Describe the approach, principles, design decisions (with the reasons), and gotchas.
- Point to the central classes, interfaces and files instead of copying them.
- Keep articles short, specific and up to date. Remove outdated content instead of adding caveats.
