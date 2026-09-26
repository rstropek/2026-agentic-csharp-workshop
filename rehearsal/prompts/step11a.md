Prepare an in-app AI agent with a refactoring: the tools of the Web API's MCP server become a shared tool catalog based on Microsoft.Extensions.AI (`AIFunction`), independent of MCP. The MCP server at /mcp uses it now, the agent of the next session will too.

- Each tool is defined in one place: name, description, parameters and traits (read-only, destructive). The MCP server only adapts the catalog to MCP (annotations, errors) and has no tool definitions of its own.
- Pure refactoring: /mcp offers and runs exactly the same tools as before, the existing tests stay green.
- Exemplary test: a read-only tool of the catalog invoked as `AIFunction`, without MCP.
- Update tech-docs/mcp.md.
- Done when the tests and the QA script are green (no manual checks against a running API). Then commit directly to main.
