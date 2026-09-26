# AGENTS.md

## Overview

Grader is a grading app for teachers and students. The repository contains the following projects:

- `GraderApi`: ASP.NET Core web API (OpenAPI with Scalar UI) that serves the backend.
- `GraderLogic`: class library with the core grading domain logic, used by the API.
- `GraderTests`: xUnit v3 tests (Microsoft Testing Platform, NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`) for the logic and API.
- `GraderWeb`: Angular frontend (see `GraderWeb/AGENTS.md` for Angular-specific guidelines).

## Technology Versions

- .NET, Angular and other technologies used here might be newer than you expect. Do not rely on your training data for APIs; verify with current docs.
- We use .NET 11 (currently a release candidate, see `global.json`). NuGet packages that ship with .NET must match the SDK's major version, so prerelease versions are fine for them. Use the latest stable version for all other packages.

## Researching Documentation

Use the installed skills to research up-to-date documentation. Priority:

1. Microsoft technologies incl. .NET, ASP.NET Core, xUnit: Microsoft Learn CLI (not MCP) from the `microsoft-docs` skill. It runs as a sub-agent: always pass your question as the skill's argument.
2. Angular: `angular-developer` skill.
3. Other technologies (incl. NSubstitute): `ctx7` CLI (not MCP) from the `find-docs` skill.
4. You may clone/pull GitHub repositories of used open source libraries into `~/github/<repo>` if you need to inspect their code or docs included in the repo.
