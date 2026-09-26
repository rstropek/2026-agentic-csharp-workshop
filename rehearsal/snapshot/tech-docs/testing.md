# Testing

## Strategy

Every feature gets tests for the regular case **and** at least one error case, on each layer it touches.

| Layer | Location | Tooling | What is real, what is mocked |
| --- | --- | --- | --- |
| Logic unit tests | `GraderTests/Logic` | xUnit v3 | Everything real; logic classes have no dependencies worth mocking. |
| API unit tests | `GraderTests/Api` | xUnit v3, `WebApplicationFactory<Program>`, NSubstitute | Real HTTP pipeline (routing, binding, exception handler, JSON); logic interfaces replaced by substitutes. |
| Database integration tests | `GraderTests/Data` | xUnit v3, in-memory SQLite | Real database with all migrations applied, private per test (see [database.md](database.md)). |
| API integration tests | `GraderTests/Integration` | xUnit v3, `WebApplicationFactory<Program>` | Everything real. Also smoke tests that OpenAPI JSON and Scalar respond. |
| Angular component tests | `GraderWeb/src/**/*.spec.ts` | Vitest via `ng test` (jsdom) | Component real; generated API services mocked. |
| E2E tests | `GraderWeb/e2e` | Playwright (Chromium) | Everything real, production hosting setup. |

Design decisions:

- **API "unit" tests go through HTTP.** Minimal API handlers are thin, so the value is in the pipeline behavior (status codes, problem details, parameter binding), not in calling handler methods directly. Mocks are injected per test via `WithWebHostBuilder` + `ConfigureTestServices` (see `GraderTests/Api/CalculatorEndpointTests.cs`).
- **Integration tests are tagged** with `[Trait("Category", "Integration")]` so they can be run or skipped separately. Untagged tests are unit tests.
- **Angular tests mock the generated API services, not `HttpClient`.** Components only depend on the typed service (e.g. `CalculatorService`), so tests provide `{ provide: XService, useValue: { method: vi.fn() } }` and stay independent of URLs. Error cases throw an `HttpErrorResponse` whose `error` is a problem details object, as the real API does.
- **Angular tests are zoneless:** act, then `await fixture.whenStable()`, then assert. Do not use `fixture.detectChanges()`.
- **E2E tests run against the production setup** (Angular built into `GraderApi/wwwroot`, backend published and started in the Production environment on port 5200). This covers SPA hosting by the backend, which the dev setup (Angular dev server + proxy) does not. See `GraderWeb/playwright.config.ts`.
- Locators: Playwright uses roles and labels first (this also checks accessibility); `data-testid` is used for values without a natural role (e.g. the calculation result).

## QA script

`scripts/qa.sh` (repo root) runs the full QA baseline: `dotnet format` check, .NET build (analyzers, warnings as errors), .NET unit and integration tests, Biome, Angular build, Angular component tests and E2E tests. Linting and formatting are described in [code-quality.md](code-quality.md).

- Output: one `=== [n] <section> ===` block per tool ending in `--- PASS|FAIL|SKIP: <section>`, then `=== SUMMARY ===` and a final `QA RESULT: PASS|FAIL` line. Exit code `1` if any section failed.
- Only output of **failing** sections is printed. Full logs of all sections are in `.qa-logs/` (gitignored, recreated on every run).
- All sections run even after a failure, so a single run reports all problems. Sections that depend on a failed build are skipped.
- `--skip-e2e` skips the slowest section for quick feedback in between. The final run of a session must be the full run.
- The E2E section fails if port 5200 is already in use, because Playwright would reuse the stale server (see Gotchas).

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
- Use `TestContext.Current.CancellationToken` for async calls in xUnit v3 tests (the xUnit analyzers warn otherwise).
- Use `npm test`, not `ng test` directly, on a fresh clone: the `pretest` script generates the API client, which is not committed (see [api-client.md](api-client.md)). `ng test` runs in watch mode in interactive terminals.
- E2E must run the **published** backend. When the build output (`dotnet run`) runs in Production, `MapStaticAssets` answers compressed requests (`Accept-Encoding: gzip/br`, i.e. every browser request) with empty `200` responses, because the precompressed files only exist in `obj/` and are only mapped in Development. The page stays blank.
- Playwright reuses a server that already listens on port 5200 (outside of CI). Stop stale instances if E2E tests test an outdated build.
- Building `GraderTests` builds `GraderApi`, which regenerates `GraderApi/openapi/GraderApi.json` (see [api-client.md](api-client.md)). A changed spec after running tests means the API contract changed.
