---
name: grader-cli
description: Do a teacher's everyday work in the Grader app with the `grader` CLI (or its MCP tools) - set up a class from a list of names, add, rename or remove students, record or correct exam results (incl. converting points to percentages), and answer questions about results, averages, missing results or how a student or class is doing. Use this skill whenever a teacher asks about or wants to change classes, students, exams, tests, results, points, percentages or grades in Grader, even if they don't mention the CLI. Not for developing GraderCli or the API itself.
---

# Teacher tasks with the `grader` CLI

The `grader` CLI mirrors the Grader Web API: one command per use case, no convenience commands. Everyday tasks
are compositions of a few commands. This skill covers how to compose them safely; `grader --help` and
`grader <area> <verb> --help` cover syntax, input shapes, error codes and exit codes.

## Before you start

- **CLI or MCP tools:** `grader mcp` runs the same use cases as MCP server. If its tools are available to you
  (`classes_get`, `exams_save`, ... = `grader <area> <verb>`), you may use them instead of the CLI: same
  descriptions, ids as parameters (`classId`), documents inline as `document` (or a JSON file path as `file`), the
  API's JSON as result and errors as tool errors with the same error codes. Destructive tools need no `--yes`; the
  rules under "Destructive commands" apply all the same. Everything else in this skill applies to both.
- **Invocation:** check with `command -v grader` whether the global tool is installed. Otherwise, in the repository
  root, build once (`dotnet build GraderCli`) and then use `dotnet run --no-build --project GraderCli -- <args>`:
  plain `dotnet run` checks the build on every call, which adds up in loops. Shell functions and variables do not
  survive between separate tool calls, so define helpers in the same command that uses them.
- **The API must be running.** Every command talks to the API (default `http://localhost:5100`, the development API;
  `GRADER_API_URL` or `--api-url` for others). Exit code 6 (`api-unreachable`) means it is not running or the URL is
  wrong. Tell the teacher and how to start the development API
  (`dotnet run --project GraderApi --launch-profile http`); do not switch to another URL on your own, because the
  data lives in the API the teacher uses.
- **Use `--json` for everything you parse** (with `jq`), the text output only to look at data. With `--json`, errors
  are JSON on stderr, so keep stderr apart from the JSON you pipe into `jq`.

## Data model in brief

- School years (e.g. `2025/26`, summer to summer) and courses (subjects, e.g. `Mathematics`) are read-only. If the
  teacher needs a new one, say that the CLI cannot create it; do not substitute a similar course.
- A class belongs to one school year and has its students. Class names repeat every year (`2B` exists in every
  school year), and a student belongs to exactly one class: the same child in last year's class is a different
  student record with a different id, with no link between them.
- An exam is a class + course + date (unique). It has one result per student: a percentage from 0 to 100, or
  `null` = no result (absent, not yet graded). The app stores percentages only, no grades or marks.

## Finding ids

Never guess ids or reuse ids from examples, earlier sessions or memory: they are database ids and change, for example
when demo data is regenerated. Resolve them by name, top-down, every session:

1. `grader school-years list --json` → school year id. If the teacher names no year, use the current one: it
   starts in late summer, so on 2026-09-26 it is `2026/27`. The list also contains past (and sometimes future)
   years, so pick by name, not by position.
2. `grader classes list <school-year-id> --json` → class id by name.
3. `grader classes get <class-id> --json` → student ids; `grader exams list <class-id> --json` → exam ids by date
   and course; `grader courses list --json` → course id by name.

Matching the teacher's words to records:

- Teachers say "Math", "the test last Thursday", "Anna", "Müller A." Map them carefully: course by meaning, student
  by last name and first name within the class. Relative dates count back from today: "Wednesday" is the most recent
  Wednesday, "last school year" the one before the current. Mention the resolved date or year in your report.
- For a list of names (e.g. results by last name), check that every name matches exactly one student of the class
  before writing anything, and that no student got two entries.
- If a name matches no one or more than one student, or an exam the teacher refers to as existing (to correct it or
  ask about it) matches no or several exams, stop and ask rather than picking the closest one. Say what you found
  (e.g. "2B has no Anna, but Annabell Hayes"). Recording results of a new test is different: there, no matching
  exam simply means you create it.

## Changing data: always the complete document

`create` and `save` take the complete document, and `save` **replaces** the lists in it:

- `classes save`: students without id are added, students with id updated, **students missing from the list are
  removed**.
- `exams save`: **results missing from the list or with `percentage: null` are removed.**

So never build a save document from scratch or from only the parts you want to change. Always:

```bash
grader classes get 85 --json > /tmp/class-85.json         # 1. get the current document
jq '...' /tmp/class-85.json > /tmp/class-85-new.json      # 2. change only what the teacher asked for
grader classes save 85 --file /tmp/class-85-new.json --json   # 3. save it; the response is the saved state
```

The `get` output can be saved back as is (extra properties are ignored); a result that is `null` in the `get` output
stays without result. Check the response of `save`/`create` against what the teacher asked for (e.g. number of
students, the changed results) before reporting success. Compare by content, not position: the API sorts students
and results by last name (case-sensitive, umlauts after z), not in your input order.

### Setting up a class from a list of names

- Split each name into first and last name. Lists come as "First Last", "LAST First", "Last, First", with middle
  names or double last names. A comma or an all-caps word marks the last name (write it in normal case: "MÜLLER" →
  "Müller"); otherwise the last word is the last name. Keep the teacher's spelling incl. diacritics (ü, é, ß) and
  apostrophes. If a split is ambiguous (e.g. "Maria Anna Berger", "Jean Luc"), ask or state the split you chose.
- New students need an explicit `"id": null`. Without it the API only answers `Bad Request [invalid-request]`
  with no hint which property is missing (the same applies to any missing property, e.g. `percentage` in results).

  ```bash
  jq -n '{name: "1D", students: [
    {id: null, firstName: "Ada", lastName: "Lovelace"},
    {id: null, firstName: "Émile", lastName: "Müller"}]}' | grader classes create 9 --json
  ```

- `class-name-taken` (409): the class exists already in that school year. Do not create it under another name;
  open the existing class and ask whether to add the students to it. Checking `classes list` first avoids this.
- Adding students to an existing class: `get`, append `{id: null, ...}` entries, `save`. Before appending, check
  that the student is not already in the class.

### Recording exam results

- Find the exam by class, course and date (`exams list`). If it exists, `get` → edit → `save`. If not,
  `exams create <class-id>` with `examDate` (`yyyy-MM-dd`), `courseId` and `results`. `exam-exists` (409) on create
  means it exists after all: open it instead of creating a second one on another date.
- Results for create: `[{studentId, percentage}]`, only for students of that class (`student-not-in-class`). The
  response (like `exams get`) lists every student of the class, with `null` for students without result, so 20
  entries in the response do not mean 20 results were entered.
- **Missing ≠ zero.** A student who was absent or has no result yet gets `null` (or is left out). `0` is a real
  result of 0 % and ruins averages. Only enter 0 if the teacher says the student scored 0, and point it out in your
  report in case they meant absent.
- **Points → percentage:** `points / maxPoints * 100`, rounded half up to one decimal (jq: `(. * 10 + 0.5 | floor) / 10`),
  and show the teacher the conversion. If the maximum is unclear, ask.
  If the teacher gives grades (e.g. "a 2"), ask for the percentage or points; the app has no grade scale.
- When correcting one result, change only that entry in the full document; all other results stay as they are.

## Answering questions

There are no per-student or statistics commands; gather the data and compute it yourself:

```bash
# all exams of a class incl. results as one JSON array
grader exams list 85 --json > /tmp/exams-85.json
for id in $(jq '.[].id' /tmp/exams-85.json); do grader exams get "$id" --json; done | jq -s . > /tmp/class-85-exams.json
jq length /tmp/exams-85.json /tmp/class-85-exams.json   # must be equal, otherwise an exams get failed (see stderr)
```

- `exams list` already has date and course, so filter it first if you only need one course. `exams get` lists
  every student of the class, with `percentage: null` for no result.
- Averages exclude `null` results; always say how many results were missing (e.g. "average 78.4 % over 4 of
  5 exams, missed the History exam on 2026-10-12"). "Overall average" means the average over all results unless the
  teacher wants the average of subject averages; say which one you used.
- Rankings ("best in Math"): a student with fewer results can top the list by chance. Flag such students and say how
  you ranked, instead of silently picking or dropping them.
- Students with the same name in different classes or school years are different records; only combine them
  across years if the teacher asks, and say so.
- If the teacher asks for grades, use their grading scale; if they have not given one, ask or clearly state the
  scale you assumed. Do not present grades as if the app stored them.
- Answer in the teacher's terms: names, courses, dates, percentages; ids only when useful for follow-up.
- Answering questions never needs a write command.

## Destructive commands

`classes delete`, `exams delete`, `demo-data replace` (deletes **all** data), and saves that remove students or
results cannot be undone. Only run them when the teacher explicitly asks for exactly that, and only for the records
they mean. `--yes` is the CLI's confirmation: add it only then, never as a reflex.

Errors are not a reason to delete. `class-has-exams` (class delete), `student-has-results` (removing a student) and
`exam-exists` protect data: report them and let the teacher decide, instead of deleting exams or results to make the
command pass. Never run `demo-data replace` to "reset" or "fix" something.

## Errors

Read the error code in the message (`[code, HTTP status]`) and the exit code before retrying:

- 2: your command line or JSON is wrong (or `--yes` missing); nothing was sent.
- 3: the API rejected the content (`validation-failed` lists field errors, e.g. `Results[0].Percentage`;
  `invalid-request` = missing property or invalid JSON/date format).
- 4: an id does not exist; resolve ids again by name.
- 5: conflict with existing data; the message says what to do, but check with the teacher before deleting anything.
- 6: API not running (see above).

## Reporting back

Tell the teacher what changed in their terms ("Created class 1D (2026/27) with 18 students", "Entered 17 results for
the Mathematics exam on 2026-09-24; Lula Russel has no result"), plus any assumptions (name splits, rounding, which
school year) and anything you did not do and why.
