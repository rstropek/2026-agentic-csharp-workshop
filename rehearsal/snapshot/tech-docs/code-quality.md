# Code Quality (QA Baseline)

## Approach

Fast, deterministic checks that fail on every finding: no warnings that "can be ignored". `scripts/qa.sh` runs all of them (see [testing.md](testing.md#qa-script)).

| Area | Tool | Configuration | Auto-fix |
| --- | --- | --- | --- |
| C# formatting and code style | IDE rules, enforced on build + `dotnet format --verify-no-changes` | `.editorconfig` (repo root) | `dotnet format` (repo root) |
| C# code analysis | .NET analyzers (`AnalysisLevel` `latest-recommended`), xUnit and NSubstitute analyzers | `Directory.Build.props`, `.editorconfig` | partly via `dotnet format` |
| C# compiler warnings | `TreatWarningsAsErrors` | `Directory.Build.props` | – |
| TypeScript/CSS/JSON lint + format, import sorting, HTML lint | Biome (replaces Prettier) | `GraderWeb/biome.json` | `npm run fix` (in `GraderWeb`) |
| Angular templates and TypeScript | Angular compiler (strict templates, extended diagnostics as errors) | `GraderWeb/tsconfig.json` | – |

## Design decisions

- **One place per stack.** All projects inherit `Directory.Build.props`; C# style lives only in the root `.editorconfig`. `GraderWeb` has its own `.editorconfig` (`root = true`) that Biome reads (`useEditorconfig`).
- **Only `warning` is enforced.** Rules we want enforced are set to `warning` (which becomes an error). Preferences that would cause churn (primary constructors, expression bodies, collection expressions, ...) stay at `suggestion`: they show in the IDE but never fail the build. To enforce a new rule, set its severity to `warning` in `.editorconfig` and fix the existing code in the same change.
- **Per-rule severities, no bulk severities.** `dotnet_analyzer_diagnostic.category-Style.severity` is ignored when `AnalysisLevel` includes a mode (`-recommended`), so each IDE rule gets its own severity (`option = value:warning` or `dotnet_diagnostic.IDExxxx.severity`).
- **EF Core migrations are generated code** (`generated_code = true` in `.editorconfig`), so analyzers and `dotnet format` skip them. See [database.md](database.md#gotchas).
- **Test naming:** tests use `Method_Scenario_Expectation`, so CA1707 (no underscores) is disabled for `GraderTests` only.
- **Biome rules turned off**, both because they clash with Angular/TypeScript settings:
  - `useLiteralKeys`: it conflicts with `noPropertyAccessFromIndexSignature` in `tsconfig.json`, which requires `process.env['CI']`.
  - `useImportType`: Biome cannot see that decorator metadata (Angular constructor DI) uses a type import as a value. Biome's docs recommend turning it off for Angular.
- **Angular extended diagnostics are errors** (`extendedDiagnostics.defaultCategory`). By default they are warnings and the build succeeds, e.g. with a signal in a template that is not invoked (`{{ result }}`).

## Gotchas

- **Biome does not format HTML.** Its HTML formatter does not understand Angular control flow (`@if`/`@for` blocks): it breaks their indentation and merges blocks onto one line. HTML formatting is off and HTML linting (mainly a11y) is on. Format templates by hand to match the existing ones (2 spaces, block content indented).
- **IDE0005 (unnecessary usings) only runs on build with `GenerateDocumentationFile`.** That setting would also report missing XML comments (CS1591), which is suppressed. XML comments are optional.
- **Naming rules need `dotnet_diagnostic.IDE1006.severity`.** The `severity` of a `dotnet_naming_rule` only applies in the IDE.
- Biome respects `.gitignore` (`vcs.useIgnoreFile`), so it skips the generated API client and build output. Files Biome cannot handle are ignored (`files.ignoreUnknown`).
- `dotnet format` reports every finding up to several times (one pass each for whitespace, style and analyzers). Duplicate lines are expected.
