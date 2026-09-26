Use the skill-creator skill to create a project skill `grader-cli` (in .agents/skills/, linked into .claude/skills/ like the other skills) that teaches agents to do a teacher's everyday tasks in Grader with the CLI: e.g. setting up a class from a list of names, recording exam results, answering questions about grades.

- Focus on workflows and pitfalls, don't repeat --help (e.g. finding ids, saving complete documents, the API must be running, destructive commands only when the teacher asks for them).
- Keep the evaluation light: 2–3 realistic test tasks against the running development API with demo data, no benchmark or review viewer.
- Done when the QA script is green. Then commit directly to main.
