# Web API

Architecture principles and patterns for the Grader Web API (`GraderApi`) and its data access (`GraderLogic`). Follow it for every new endpoint. If a change needs to deviate from it, ask first, then update this article.

## Use cases, not tables

- Endpoints serve concrete use cases of a client (UI, CLI and the agents using it, see `cli.md`). There is no endpoint just because a table exists, and no generic CRUD per entity.
- Endpoints are grouped by use-case area (e.g. class management, grading), not by entity.
- Request and response shapes are tailored to the use case. A response may combine several tables or contain computed values.
- The server decides sort order per use case. Clients do not send filter, sort or paging expressions.

## Layers

```
Endpoint (GraderApi)  →  use-case service interface (GraderLogic)  →  DbContext
```

- **Endpoints are thin:** bind and validate the request, call a service, map the result to an HTTP response. Endpoints never use the `DbContext` directly and contain no business rules.
- **Use-case services** live in `GraderLogic`, one per use-case area, behind an interface, registered in DI (scoped, like the `DbContext`). Endpoint unit tests mock the interface (NSubstitute); service tests run against in-memory SQLite (see `database.md`).
- Services own the queries, the business rules and the transaction boundary.
- Folders per use-case area in both projects: `GraderLogic/<Area>/` (interface, service, request/response records), `GraderApi/<Area>/` (one `<Area>Endpoints.cs` with a `Map<Area>Endpoints` extension, called in `Program.cs`).

## Data access

- Database entities never leave `GraderLogic`. Services accept and return request/response records (DTOs). The OpenAPI spec contains no entity types.
- Reads project directly into response records (`Select`, no tracking). No loading of entities just to map them.
- Writes: load what is needed, apply the changes, call `SaveChanges` once (via `SaveChangesAndTranslateErrorsAsync`, see Errors). One request = one transaction, all or nothing.
- Exception: bulk operations (e.g. replacing all data with demo data) use `ExecuteDelete` inside an explicit transaction instead of loading thousands of rows.
- Writes return the saved document by running the same query as the corresponding read (e.g. `SaveClassAsync` ends with `GetClassAsync`), so the shapes cannot drift apart.
- Business rules are checked before saving, so the caller gets a specific error. The database constraints (see `database.md`) remain the last line of defense.

### Updating a parent with its children

When a use case edits a parent together with its children as one unit, the client sends the complete document (parent + all children) with `PUT`:

- children are identified by their `id` or by a natural key within the parent (e.g. the student of an exam result)
- known child → update (it must belong to this parent), unknown child → insert, existing child missing from the request → delete
- all or nothing; sending the same request twice gives the same result
- the response has the same shape as the corresponding `GET` (incl. new ids), so the client can replace its state with it
- create (`POST`) and save (`PUT`) use the same request record, so the UI can use one form for both; on create, children must not have an id
- a child with a nullable value in the `GET` (e.g. an exam result without percentage) may be sent back unchanged: `null` means "no child row" (delete or do not insert)

## Errors

- Services signal expected errors with the subclasses of `GraderLogicException`: `BusinessRuleException`, `NotFoundException`, `ConflictException`. Each carries a stable, kebab-case error code (e.g. `class-name-taken`) and a human-readable message. Endpoints list their error codes in `WithDescription`, so they show up in the OpenAPI spec and the generated client.
- Services translate database errors into these exceptions. `DbUpdateException` and SQLite error codes never leave the data access code. A constraint violation that was not caught by a business rule check (e.g. concurrent requests) becomes a conflict with the code `constraint-violation`. Other database errors stay unexpected (500).
- A central exception handler in `GraderApi` maps these exceptions to Problem Details (RFC 9457) with the error code in a `code` extension member. Problem details written by the framework get a code as well: `validation-failed` (field errors), `invalid-request` (unreadable body, e.g. invalid JSON or a missing property), `unexpected-error` (500).

| Situation | Status |
|---|---|
| Request validation failed | 400, validation Problem Details with field errors |
| Request body unreadable (invalid JSON, missing property) | 400 |
| Business rule violated by the request content (`BusinessRuleException`) | 400 |
| Unknown id (`NotFoundException`) | 404 |
| Conflict with existing data: duplicate, still referenced, constraint violation (`ConflictException`) | 409 |
| Anything unexpected | 500, no internals (stack traces, SQL) in the response |

- Clients and tests check `code`, never `title` or `detail`.

## Validation

- Request records are validated at the API boundary with the built-in minimal API validation (`AddValidation`, data annotations with `[property: ...]` on the record parameters, so OpenAPI shows them too). Nested records and lists are validated recursively.
- Limits mirror the database (required, max lengths, ranges), so users get a 400 with field errors instead of a database error. Use the constants on the entities (e.g. `Student.LastNameMaxLength`) instead of repeating numbers.
- JSON deserialization requires all constructor parameters of request records (`RespectRequiredConstructorParameters`), so a missing value-type property (e.g. a date) is a 400 instead of silently becoming its default value. Nullable properties must be sent explicitly as `null`; the OpenAPI spec marks them as required, too.

## HTTP and OpenAPI conventions

- Routes: `/api/...`, plural kebab-case nouns, at most one level of nesting (e.g. `/api/school-years/{schoolYearId}/classes`).
- Success: `GET` 200, `POST` 201 with `Location` and the created document, `PUT` 200 with the saved document, `DELETE` 204. `POST` actions that do not create a resource (e.g. demo data) return 204.
- Ids are the database's integer ids; dates are `DateOnly` (ISO `yyyy-MM-dd`).
- Dates without a time stay ISO strings in every client, including the generated Angular client (see `api-client.md`). A date-only value has no time zone; converting it to a JavaScript `Date` makes it midnight UTC, which can show up as the previous day in the browser. Native `<input type="date">` uses the same string format.
- Tags are use-case areas, and operation names are use cases (e.g. `SaveClass`, not `Put`). The generated Angular client (see `api-client.md`) then gets one service per area, with methods that read like use cases.
- Every endpoint declares all its responses, including the error status codes.

## Deliberately not done

- No generic CRUD, no `PATCH`, no OData or generic filtering/sorting/paging (result sizes are small by nature).
- No generic repository or unit-of-work layer on top of EF Core: the `DbContext` already is the unit of work and its `DbSet`s are the repositories. The use-case service interface is the abstraction for mocking.
- No optimistic concurrency: last write wins.
- No authentication/authorization yet. Note that this includes the demo data endpoint, which deletes all data.
- No write API for school years and courses yet.

## Gotchas

- **SQLite cannot translate queries that need SQL `APPLY`**, e.g. a subquery that references the outer row inside a nested collection projection (a per-student result of an exam inside the exam's projection). EF Core only fails at runtime (`InvalidOperationException`), so every read query needs a test or a manual call. Split such reads into two queries (see `ExamService.GetExamAsync`).
- **Validation error keys are C# member paths** (`Students[0].LastName`), not the camelCase JSON names.
- **Validation only runs for types reachable from endpoint handler signatures** (the source generator runs in `GraderApi` and walks them, incl. types from `GraderLogic`). Missing metadata fails silently; test at least one invalid request per request type.
- **Validation problem details only get a `code`** because `AddProblemDetails` is registered: the validation filter then writes through `IProblemDetailsService`, which applies `CustomizeProblemDetails`.
- **The database schema is migrated on API startup** (`Program.cs`), skipped during build-time OpenAPI generation. In tests, the schema therefore only exists after the test host has started (`GraderApiFactory.CreateDbContext` takes care of it).
- **Text sorting uses SQLite's `BINARY` collation**: case-sensitive, non-ASCII letters (e.g. umlauts) after `z`.

## Pointers

- Services and request/response records: `GraderLogic/ClassManagement`, `GraderLogic/Exams`, `GraderLogic/DemoData`.
- Exceptions: `GraderLogic/GraderLogicException.cs` and its subclasses; database error translation: `GraderLogic/Data/DatabaseErrors.cs`.
- Exception handler and error codes of framework problems: `GraderApi/ErrorHandling`.
- Endpoints: `GraderApi/<Area>/<Area>Endpoints.cs`; DI registration, JSON options and database setup: `GraderApi/Program.cs`.
- Tests (see `testing.md`): endpoint unit tests with a mocked service `GraderTests/Api/ClassManagementEndpointTests.cs`; service tests against SQLite `GraderTests/Data/ClassManagementServiceTests.cs`; end-to-end API tests `GraderTests/Integration`; test host with a private in-memory database `GraderTests/GraderApiFactory.cs`.
