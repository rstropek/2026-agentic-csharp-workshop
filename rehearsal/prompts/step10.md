Add an MCP server to the Web API: GraderApi offers the tools of `grader mcp` over Streamable HTTP at /mcp, so agents can use Grader without installing anything.

- Same tool names, descriptions and annotations as the stdio server in GraderCli (without local file paths). The tools call the GraderLogic services directly, like the endpoints (see web-api.md).
- Only the current spec revision (2026-07-28, stateless); don't write extra code for older revisions.
- Tools are plain methods with descriptions, grouped by use-case area, without MCP-specific parameters or logic – an in-app agent will reuse them later.
- Exemplary tests: a tool call against /mcp of the Web API in memory; a business rule violation becoming a tool error.
- New tech docs article tech-docs/mcp.md (both MCP servers, how they differ, how to connect a client); update AGENTS.md and testing.md.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
