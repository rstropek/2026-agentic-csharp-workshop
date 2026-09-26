Add a command-line interface (CLI) for Grader: a new .NET console project GraderCli that mirrors the Web API. Its main users are AI agents doing tasks for a teacher; humans use it too.

- One command per Web API use case, with short command names that are nice to use for humans too (e.g. `grader classes get 7`). Mirror the API: ids as arguments, saving takes the complete document as JSON (file or stdin, same shape as the API request), no convenience commands.
- Call the API through a client generated from the OpenAPI spec with Kiota; don't reference GraderLogic. The generated code is committed and regenerated only as a dedicated step, not on every build: add a rule to AGENTS.md that every structural API change includes regenerating the CLI client (and how).
- API base URL via option and environment variable, default: the local development API.
- Agent-friendly: JSON output besides readable text, errors on stderr incl. the API's error code, meaningful exit codes, never prompt (destructive commands require --yes), --help with examples.
- Package it as a .NET global tool (command `grader`).
- Exemplary tests: a command running against the Web API in memory; an API error becoming an error message and exit code.
- New tech docs article tech-docs/cli.md (incl. building, installing and updating the tool); update AGENTS.md, testing.md and api-client.md.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
