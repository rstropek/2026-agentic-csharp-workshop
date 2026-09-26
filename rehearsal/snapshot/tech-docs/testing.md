# Testing

## Strategy

Every feature gets tests for the regular case **and** at least one error case, on each layer it touches.

| Layer | Location | Tooling | What is real, what is mocked |
| --- | --- | --- | --- |
| Logic unit tests | `GraderTests/Logic` | xUnit v3 | Everything real, no database; for logic without dependencies worth mocking (e.g. translating database errors). |
| API unit tests | `GraderTests/Api` | xUnit v3, `GraderApiFactory`, NSubstitute, `ApiMcpClient` | Real HTTP pipeline (routing, binding, validation, exception handler, JSON) incl. the MCP server at `/mcp` and the assistant's endpoint; use-case service interfaces replaced by substitutes. |
| Database integration tests | `GraderTests/Data` | xUnit v3, in-memory SQLite | Data model, test data generator and use-case services against a real database with all migrations applied, private per test (see [database.md](database.md)). |
| API integration tests | `GraderTests/Integration` | xUnit v3, `GraderApiFactory`, `ApiMcpClient`, `ScriptedChatClient` | Everything real incl. an in-memory SQLite database, through the endpoints, the MCP server or the assistant (AG-UI; only the LLM is a fake). Also smoke tests that OpenAPI JSON and Scalar respond. |
| CLI tests | `GraderTests/Cli` | xUnit v3, `CliRunner`, `McpServerRunner`, `GraderApiFactory` (NSubstitute for unit tests) | Complete CLI in memory (parsing, output, exit codes) or its MCP server with an MCP client, against the API in memory; integration tests (tagged) with a real database, unit tests with mocked use-case services. |
| Angular component tests | `GraderWeb/src/**/*.spec.ts` | Vitest via `ng test` (jsdom) | Component real; generated API services mocked. |
| E2E tests | `GraderWeb/e2e` | Playwright (Chromium) | Everything real, production hosting setup. |

Design decisions:

- **API "unit" tests go through HTTP.** Minimal API handlers are thin, so the value is in the pipeline behavior (status codes, problem details incl. `code`, parameter binding, validation), not in calling handler methods directly. Mocks are injected per test via `WithWebHostBuilder` + `ConfigureTestServices` (see `GraderTests/Api/ClassManagementEndpointTests.cs`).
- **API tests use `GraderApiFactory`, never `WebApplicationFactory<Program>` directly.** It points the API to a private in-memory SQLite database, so tests never touch `data/grader.db` (the API migrates its database on startup). As a class fixture, the database is shared by all tests of a class: each test arranges its own data with unique names (e.g. its own school year) and must not rely on counts of the whole table.
- **Error cases assert the `code` of the problem details**, never `title` or `detail` (see [web-api.md](web-api.md#errors)).
- **Integration tests are tagged** with `[Trait("Category", "Integration")]` so they can be run or skipped separately. Untagged tests are unit tests.
- **CLI tests run the CLI in memory:** `CliRunner` calls `CliApp.RunAsync` with a `CliContext` of string writers/readers and an `HttpClient` of `GraderApiFactory`. They assert the exit code and the error code on stderr, like API tests assert the `code`. Stdin is "a terminal" unless the test passes input. See [cli.md](cli.md).
- **MCP server tests go through an MCP client:** `McpServerRunner` runs `grader mcp` in memory with pipes as stdin/stdout and connects the SDK's `McpClient` pinned to protocol revision 2026-07-28. Tests call tools like a host does and assert the tool result: the API's JSON, or `isError` with the error `code` in the error JSON. Disposing the runner shuts the server down like a client (closing stdin) and asserts exit code 0. See `GraderTests/Cli/McpServerTests.cs`.
- **The API's MCP server is tested the same way:** `ApiMcpClient` connects the SDK's `McpClient` (Streamable HTTP, pinned to 2026-07-28) to `/mcp` through the `HttpClient` of the test server, so mocks are injected as for endpoint tests. Unit tests (`GraderTests/Api/McpEndpointTests.cs`) mock the use-case services and also compare the tool list with `grader mcp`'s; integration tests (`GraderTests/Integration/McpEndpointIntegrationTests.cs`) use the database. The tool catalog is tested without MCP by invoking a tool as `AIFunction` with the services of a scope of the API in memory (`GraderTests/Api/ToolCatalogTests.cs`). See [mcp.md](mcp.md).
- **The assistant is tested with an AG-UI run over HTTP and a fake LLM:** `ScriptedChatClient` replaces the LLM's keyed `IChatClient` with scripted responses (e.g. a tool call, then the answer) and records the requests. The test posts the run input through the test server's `HttpClient`, parses the server-sent events and asserts the tool call, its result and the answer (`GraderTests/Integration/AssistantIntegrationTests.cs`). Tests never call a real LLM (see Gotchas). See [assistant.md](assistant.md).
- **Angular tests mock the generated API services, not `HttpClient`.** Components only depend on the typed service (e.g. `ClassManagementService`), so tests provide `{ provide: XService, useValue: { method: vi.fn() } }` and stay independent of URLs. Error cases throw an `HttpErrorResponse` whose `error` is a problem details object, as the real API does.
- **Angular tests are zoneless:** act, then `await fixture.whenStable()`, then assert. Do not use `fixture.detectChanges()`. Async actions that are not tracked as pending tasks (e.g. a Signal Forms submission awaiting a mocked API call) need one more macrotask (`await new Promise((r) => setTimeout(r))`) before the final `whenStable()`; see the `click` helper in `GraderWeb/src/app/classes/class-editor.spec.ts`.
- **Component tests drive the DOM like a user:** set input values and dispatch `input` events, click buttons found by their accessible name (text or `aria-label`), assert rendered text, `aria-invalid` and focus. Route parameters are component inputs (`withComponentInputBinding`), so tests set them with `fixture.componentRef.setInput(...)`.
- **What gets a component test:** base components with behavior (references: `ConfirmDialog`; `PercentageField` in a small host component with a real Signal Form) and editors with validation and save logic (references: `ClassEditor`, `ExamEditor`: request content, validation, server error). The assistant chat (reference: `Assistant`) gets a scripted agent (`ASSISTANT_AGENT_FACTORY`, an `AbstractAgent` whose `run()` emits the AG-UI events the test pushes), so the real client applies them; after pushing events, wait a macrotask before `whenStable()`. Components that inject a component-scoped store (`ExamsStore`) get the real store in the test providers, with the API services below it mocked. Pure layout is checked in the browser, not in component tests.
- **E2E tests run against the production setup** (Angular built into `GraderApi/wwwroot`, backend published and started in the Production environment on port 5210). This covers SPA hosting by the backend, which the dev setup (Angular dev server + proxy) does not. See `GraderWeb/playwright.config.ts`.
- **E2E tests use a temporary database, never `data/grader.db`.** The web server command deletes `<tmpdir>/grader-e2e/` and points the API to a new database there (`ConnectionStrings__Grader`). The Playwright project `setup` (`GraderWeb/e2e/demo-data.setup.ts`) runs first and fills it with demo data through the Settings page, which also covers the reset and the confirmation dialog.
- **E2E tests are independent:** each test creates its own class with a unique name or does not save anything, and never relies on counts or on other tests' data (e.g. the exam test creates a class with students, then an exam for it). Shared steps live in `GraderWeb/e2e/helpers.ts`. Tests run in parallel (`fullyParallel`).
- Locators: Playwright uses roles and labels first (this also checks accessibility); `data-testid` is used for values without a natural role (e.g. the calculation result).

## QA script

`scripts/qa.sh` (repo root) runs the full QA baseline: `dotnet format` check, .NET build (analyzers, warnings as errors), a check that the CLI's generated API client is up to date (see [cli.md](cli.md#generating-the-client)), .NET unit and integration tests, Biome, Angular build, Angular component tests and E2E tests. Linting and formatting are described in [code-quality.md](code-quality.md).

- Output: one `=== [n] <section> ===` block per tool ending in `--- PASS|FAIL|SKIP: <section>`, then `=== SUMMARY ===` and a final `QA RESULT: PASS|FAIL` line. Exit code `1` if any section failed.
- Only output of **failing** sections is printed. Full logs of all sections are in `.qa-logs/` (gitignored, recreated on every run).
- All sections run even after a failure, so a single run reports all problems. Sections that depend on a failed build are skipped.
- `--skip-e2e` skips the slowest section for quick feedback in between. The final run of a session must be the full run.
- The E2E section fails if port 5210 is already in use and shows the blocking process (see Gotchas).

## Commands

Run from the repository root unless stated otherwise.

```bash
# All .NET tests (unit + integration)
dotnet test

# .NET unit tests only / integration tests only (xUnit v3 on Microsoft Testing Platform)
dotnet test --filter-not-trait "Category=Integration"
dotnet test --filter-trait "Category=Integration"

# Angular component tests (single run; generates the API client first)
cd GraderWeb && npm test -- --watch=false

# E2E tests (builds Angular, publishes and starts the backend automatically)
cd GraderWeb && npx playwright install chromium   # once per machine
cd GraderWeb && npm run e2e
```

## Gotchas

- `dotnet test` runs on **Microsoft Testing Platform** (configured in `global.json`), not VSTest. Use the xUnit v3 MTP filter options (`--filter-trait`, `--filter-not-trait`, `--filter-method`, ...), not `--filter "Category=..."`.
- `WebApplicationFactory` uses the **Development** environment by default. OpenAPI and Scalar are only mapped in Development, and SPA hosting only outside of it (see `GraderApi/Program.cs`). Tests that depend on either must set the environment accordingly.
- Development also **loads the user secrets of `GraderApi`**, e.g. a developer's OpenRouter API key. `GraderApiFactory` overrides it with an empty value, so tests never call the real LLM and the assistant's endpoint answers `assistant-not-configured` unless a test replaces the chat client.
- Use `TestContext.Current.CancellationToken` for async calls in xUnit v3 tests (the xUnit analyzers warn otherwise).
- **The SDK's `StreamClientTransport` does not close the server's input when the client is disposed**, so `grader mcp` would wait for stdin forever and the test run hangs. `McpServerRunner` completes the pipe itself; use it instead of wiring transports in a test.
- Use `npm test`, not `ng test` directly, on a fresh clone: the `pretest` script generates the API client, which is not committed (see [api-client.md](api-client.md)). `ng test` runs in watch mode in interactive terminals.
- E2E must run the **published** backend. When the build output (`dotnet run`) runs in Production, `MapStaticAssets` answers compressed requests (`Accept-Encoding: gzip/br`, i.e. every browser request) with empty `200` responses, because the precompressed files only exist in `obj/` and are only mapped in Development. The page stays blank.
- Playwright never reuses a server that already listens on port 5210 (`reuseExistingServer: false`): it might use another database, and the tests reset all data. Stop the stale instance instead.
- **`getByLabel` does not find selects wrapped in their `<label>`** by exact text: the label text includes the option texts. Use `getByRole('combobox', { name: ... })`.
- **jsdom has no modal dialogs** (`HTMLDialogElement.showModal`/`close`). `GraderWeb/src/test-setup.ts` (registered as `setupFiles` in `angular.json`) adds a minimal polyfill incl. the `close` event. Browser behavior like the focus trap or Escape is only covered by E2E tests.
- **Signal Forms focus/typing timing:** after "Add student", the new input is focused in `afterNextRender`. E2E tests wait with `expect(...).toBeFocused()` before typing; `page.keyboard.type` right after the click would type into the button.
- Building `GraderTests` builds `GraderApi`, which regenerates `GraderApi/openapi/GraderApi.json` (see [api-client.md](api-client.md)). A changed spec after running tests means the API contract changed.
