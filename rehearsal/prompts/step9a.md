Turn the CLI into an MCP server: `grader mcp` runs Grader as a stdio server for the Model Context Protocol, so AI agents can use it as tools.

- Research MCP with https://modelcontextprotocol.io/llms.txt and add it to "Researching Documentation" in AGENTS.md as the source for MCP concepts and the protocol. Support the current spec revision (2026-07-28); don't write extra code for older revisions.
- Use the official MCP C# SDK. One tool per CLI command, sharing the code with the commands. Tools that save a document take it inline or as a path to a local JSON file.
- Mark read-only and destructive tools; API errors become tool errors incl. the error code.
- Exemplary tests: a tool call through an MCP client against the Web API in memory; an API error becoming a tool error.
- Update tech-docs/cli.md (incl. how to register the server in an MCP client) and testing.md; mention the MCP server in the grader-cli skill.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
