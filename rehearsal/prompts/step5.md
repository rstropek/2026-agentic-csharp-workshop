Add a database. The data model is specified in ./spec/2026-09-storyboard_erdiagram.md.

- SQLite with EF Core. The database file lives in ./data (relative to the repo root). Keep the folder in git, but not its content.
- Add Microsoft.EntityFrameworkCore.Sqlite and Microsoft.EntityFrameworkCore.Design to GraderLogic with `dotnet package add`.
- Data model and DbContext in GraderLogic according to the spec.
- Generating and applying migrations must work from the GraderLogic project.
- Test data generator (see spec) in a separate class. An integration test runs it and verifies the row counts.
- Exemplary integration tests for database constraints:
  - Adding a duplicate SchoolYear.Name fails.
  - Deleting a SchoolYear that has SchoolClass records fails.
  - Adding an ExamResult with a Percentage of 101 fails.
- AGENTS.md: ./spec contains specifications. They are historical and not always kept up to date, so they might not reflect the current state of the system if they were written a while ago. If a spec contradicts code or tech docs, code and tech docs win; ask if unsure.
- New tech docs article tech-docs/database.md (follow the tech docs rules in AGENTS.md).
- Done when the QA script is green. Then commit directly to main.
