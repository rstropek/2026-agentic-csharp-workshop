Implement the first Web API use cases. Follow the architecture in tech-docs/web-api.md.

Use cases:
- Class management:
  - Pick a school year (newest first).
  - List the classes of a school year (by name, incl. number of students).
  - Open a class incl. its students (by last name, first name).
  - Create a class incl. its students.
  - Save a class incl. its students. The full student list is always sent (users maintain all rows in one UI, later maybe via Excel upload). Removing a student who has exam results fails.
  - Delete a class incl. its students. Fails if the class has exams.
- Exams:
  - Pick a course (by name).
  - List the exams of a class (by date, incl. course).
  - Open an exam incl. the results of all students of the class (by last name, first name; no result = null).
  - Create an exam incl. its results.
  - Save an exam incl. its results. Date, course and the full result list are always sent (users enter all results of an exam in one UI). The class of an exam cannot be changed. Results only for students of the exam's class.
  - Delete an exam incl. its results.
- Demo data: one endpoint that empties the database and fills it with demo data using the test data generator (will be a button in the UI later).

Requirements:
- No UI yet (next step).
- SchoolYear and Course have no write API (a CLI and agent support will follow later).
- GraderApi uses the SQLite database from the previous step.
- Exemplary tests, each with a regular and an error case:
  - Unit tests: one data access helper (e.g. translating a DB error into an exception); one endpoint with a mocked use-case service.
  - Integration tests: saving a class with added, changed and removed students against SQLite; one endpoint end to end incl. the error `code` in the Problem Details.
- You may extend, update and refine tech-docs/web-api.md (follow the tech docs rules in AGENTS.md). If you need to deviate from its principles, ask first.
- Add web-api.md to the tech docs index in AGENTS.md.
- Done when the QA script is green. Then commit directly to main.
