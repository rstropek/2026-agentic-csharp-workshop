# API Client Generation

## Approach

The .NET code is the single source of truth for the HTTP API. The Angular client is generated from it; it is never written by hand.

```
GraderApi endpoints ──dotnet build──▶ GraderApi/openapi/GraderApi.json ──npm run generate:api──▶ GraderWeb/src/app/api/
  (C# code)                            (committed)                                                (generated, not committed)
```

1. **Spec at build time.** `Microsoft.Extensions.ApiDescription.Server` runs the app's entry point after every build of `GraderApi` and writes the OpenAPI document to `GraderApi/openapi/GraderApi.json` (configured in `GraderApi/GraderApi.csproj`).
2. **Spec is committed.** It is the reviewable API contract: API changes show up as a diff of this file. Commit it together with the endpoint changes.
3. **Client via ng-openapi.** `GraderWeb/openapi.config.ts` generates services and models into `GraderWeb/src/app/api/`. The npm `prestart`, `prebuild` and `pretest` scripts run the generator automatically, so the client is always in sync with the committed spec. The generated folder is gitignored.

## Commands

```bash
# Regenerate the spec after changing endpoints (any build of GraderApi, GraderTests or the solution does it)
dotnet build GraderApi

# Regenerate the Angular client from the spec (also runs automatically before npm start/build/test)
cd GraderWeb && npm run generate:api
```

## Usage in Angular

- The client is registered once in `GraderWeb/src/app/app.config.ts` (`provideHttpClient(withInterceptorsFromDi())` + `provideDefaultClient({ basePath: '' })`).
- **All API access goes through the generated services** (e.g. `ClassManagementService`, `DemoDataService`); components never build URLs or use `HttpClient` directly. Component tests replace the service with a mock object (see [testing.md](testing.md)).
- **Loading data: `rxResource`** with the generated method as `stream` (the methods return Observables). Parameters come from signals (e.g. the selected school year); when they change, the pending request is cancelled (unsubscribed) and a new one starts. Reference: `GraderWeb/src/app/classes/class-list.ts`.
- **Writing data:** `firstValueFrom(service.method(...))` in `async` code (Signal Forms submission action, button handlers). Reference: `GraderWeb/src/app/classes/class-editor.ts`.
- **Errors → user messages:** `toApiError` (`GraderWeb/src/app/shared/api-error.ts`) turns any error into `{ code, status, message }`: the problem details' `detail` (the API's messages are written for users), field errors of `validation-failed`, and fixed texts for network errors (status 0), 5xx and 404 without details. Decide on `code`, show `message`. `LoadError` shows load failures with a retry button.

## Designing endpoints for a good client

The generated TypeScript is only as good as the OpenAPI metadata. Reference: `GraderApi/ClassManagement/ClassManagementEndpoints.cs`.

| OpenAPI metadata | Source in .NET | Effect on the client |
| --- | --- | --- |
| Tag | `WithTags("ClassManagement")` on the route group | One service per tag: `ClassManagementService` |
| `operationId` | `WithName("Add")` | Method name: `add(...)`. Keep names unique and meaningful. |
| Success response | `TypedResults` return types (`Ok<T>`, ...) | Typed return value (`Observable<T>`) |
| Error responses | `.ProducesProblem(statusCode)` | Documented problem details response |
| Descriptions | `WithSummary`, `WithDescription`, `[Description]` on parameters, records and properties | JSDoc comments on methods and models |

Expected domain errors are exceptions derived from `GraderLogic.GraderLogicException`; `GraderApi/ErrorHandling/GraderLogicExceptionHandler.cs` turns them into `400`/`404`/`409` problem details with an error `code` (see [web-api.md](web-api.md#errors)). Endpoints declare every status they can return: `.ProducesProblem(...)` for 404/409, `.ProducesValidationProblem()` for 400 on endpoints with a request body (validation problem details are a superset of problem details).

## Gotchas

- **Numbers typed as `number | string`:** ASP.NET Core's web JSON defaults accept numbers as strings, so OpenAPI describes them as `integer | string`. `GraderApi/Program.cs` sets `JsonNumberHandling.Strict` to keep the schema (and the TypeScript types) clean. Do not remove it.
- **Base path:** operation paths in the spec already start with `/api`, but the generated `BASE_PATH_DEFAULT` token defaults to `'/api'` (→ `/api/api/...`). Always provide `basePath: ''`. Same origin works in both setups: dev server proxy (`GraderWeb/proxy.conf.json`) and backend-hosted SPA.
- **Interceptors:** the generated client registers a class-based interceptor via `HTTP_INTERCEPTORS`. It only runs with `withInterceptorsFromDi()`; client-specific interceptors passed to `provideDefaultClient` would silently be ignored without it.
- **Build-time generation runs `Program.cs`** in the Development environment (`OpenApiGenerationEnvironment` in the csproj). Startup code that needs external resources (databases, secrets) must be skipped during generation, e.g. by checking `Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider"`.
- **OpenAPI version:** .NET 11 emits OpenAPI 3.2 by default. ng-openapi handles the current spec; if a future spec breaks the generator, set `options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1` in `AddOpenApi`.
- **Missing parameters:** generated methods skip `null`/`undefined` query parameters. The API then rejects the request with a `400` problem details without `detail` (binding failure, code `invalid-request`). Validate required inputs in the UI before calling the API.
- **Resource errors:** `resource.error()` of a failed API call is the `HttpErrorResponse` itself (it counts as error-like because it has `name` and `message`). Values that are not error-like are wrapped in an `Error` with the original as `cause`; `toApiError` handles both.
- **`resource.value()` throws in the error state.** Read it only after `hasValue()` (see `SchoolYearStore.list`).
- **Validation limits are not in the generated models.** The max lengths of the API (`SchoolClass.NameMaxLength`, ...) are repeated as constants in the Angular form (`class-editor.ts`); change both together.
- Never edit files in `GraderWeb/src/app/api/`; they are overwritten. On a fresh clone the folder is missing (TypeScript errors in the IDE) until `npm run generate:api` or any `npm start/build/test` has run.
