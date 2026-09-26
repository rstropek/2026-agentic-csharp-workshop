# Web API

Architecture principles and patterns for the Grader Web API (`GraderApi`) and its data access (`GraderLogic`). This article was written before the first real endpoint existed. Follow it for every new endpoint. If a change needs to deviate from it, ask first, then update this article.

## Use cases, not tables

- Endpoints serve concrete use cases of a client (UI, later CLI or agent). There is no endpoint just because a table exists, and no generic CRUD per entity.
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

## Data access

- Database entities never leave `GraderLogic`. Services accept and return request/response records (DTOs). The OpenAPI spec contains no entity types.
- Reads project directly into response records (`Select`, no tracking). No loading of entities just to map them.
- Writes: load what is needed, apply the changes, call `SaveChanges` once. One request = one transaction, all or nothing.
- Business rules are checked before saving, so the caller gets a specific error. The database constraints (see `database.md`) remain the last line of defense.

### Updating a parent with its children

When a use case edits a parent together with its children as one unit, the client sends the complete document (parent + all children) with `PUT`:

- children are identified by their `id` or by a natural key within the parent (e.g. the student of an exam result)
- known child → update (it must belong to this parent), unknown child → insert, existing child missing from the request → delete
- all or nothing; sending the same request twice gives the same result
- the response has the same shape as the corresponding `GET` (incl. new ids), so the client can replace its state with it

## Errors

- Services signal expected errors with a small set of exceptions defined in `GraderLogic` (e.g. not found, conflict with a business rule). Each carries a stable, kebab-case error code (e.g. `name-taken`) and a human-readable message.
- Services translate database errors into these exceptions. `DbUpdateException` and SQLite error codes never leave the data access code. A constraint violation that was not caught by a business rule check (e.g. concurrent requests) becomes a conflict with the code `constraint-violation`.
- A central exception handler in `GraderApi` maps these exceptions to Problem Details (RFC 9457) with the error code in a `code` extension member:

| Situation | Status |
|---|---|
| Request validation failed | 400, validation Problem Details with field errors |
| Business rule violated by the request content | 400 |
| Unknown id | 404 |
| Conflict with existing data (duplicate, still referenced, constraint violation) | 409 |
| Anything unexpected | 500, no internals (stack traces, SQL) in the response |

- Clients and tests check `code`, never `title` or `detail`.

## Validation

- Request records are validated at the API boundary with the built-in minimal API validation (data annotations).
- Limits mirror the database (required, max lengths, ranges), so users get a 400 with field errors instead of a database error.

## HTTP and OpenAPI conventions

- Routes: `/api/...`, plural kebab-case nouns, at most one level of nesting (e.g. `/api/school-years/{schoolYearId}/classes`).
- Success: `GET` 200, `POST` 201 with `Location`, `PUT` 200 with the saved document, `DELETE` 204.
- Ids are the database's integer ids; dates are `DateOnly` (ISO `yyyy-MM-dd`).
- Dates without a time stay ISO strings in every client, including the generated Angular client (see `api-client.md`). A date-only value has no time zone; converting it to a JavaScript `Date` makes it midnight UTC, which can show up as the previous day in the browser. Native `<input type="date">` uses the same string format.
- Tags are use-case areas, and operation names are use cases (e.g. `SaveClass`, not `Put`). The generated Angular client (see `api-client.md`) then gets one service per area, with methods that read like use cases.
- Every endpoint declares all its responses, including the error status codes.

## Deliberately not done

- No generic CRUD, no `PATCH`, no OData or generic filtering/sorting/paging (result sizes are small by nature).
- No generic repository or unit-of-work layer on top of EF Core: the `DbContext` already is the unit of work and its `DbSet`s are the repositories. The use-case service interface is the abstraction for mocking.
- No optimistic concurrency: last write wins.
- No authentication/authorization yet.

## Pointers

*(to be added with the implementation: services and their interfaces, exceptions, exception handler, endpoint files, tests)*
