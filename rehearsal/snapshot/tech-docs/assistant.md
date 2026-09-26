# AI Assistant

The Web API hosts an AI assistant that answers a teacher's questions about classes, exams and results. It is an agent built with Microsoft Agent Framework and served over the AG-UI protocol, so the web UI can talk to it with a standard AG-UI client. The UI design is in `spec/design/assistant.md`.

AG-UI is documented at https://docs.ag-ui.com/llms.txt (the docs and the specification as Markdown pages). Microsoft Agent Framework's code and samples are in the `microsoft/agent-framework` repository (`dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`, `dotnet/samples/02-agents/AGUI`).

## Architecture

```
Web UI (AG-UI client)
  → POST /api/assistant (MapAGUIServer: run input as JSON, AG-UI events as server-sent events)
  → ChatClientAgent (instructions, the read-only tools of the tool catalog)
  → FunctionInvokingChatClient (our own tool invoker)
  → OpenRouter (OpenAI SDK, Chat Completions)
```

Everything lives in `GraderApi/Assistant`: `AssistantSetup` (registration, agent, tool invoker, endpoint and instructions), `OpenRouterOptions` and `NotConfiguredChatClient`.

- **Read-only tools from the tool catalog** (`CatalogTool.IsReadOnly`, see [mcp.md](mcp.md#the-web-apis-tool-catalog)): the assistant never changes data, so no approvals are needed. A new read tool in the catalog is automatically available to the assistant.
- **Instructions** (`AssistantSetup.Instructions`): short answers in the markdown subset the UI renders (see `spec/design/assistant.md`), the newest school year unless the teacher names another one, never invent data, resolve ids by name like the MCP server's instructions, read-only.
- **A scope per tool call:** the AG-UI endpoint resolves the agent once, so the agent is a singleton, but the tool classes and the `DbContext` are scoped. Our own `FunctionInvokingChatClient.FunctionInvoker` creates a scope for each tool call and passes its services as `AIFunctionArguments.Services`. This does not depend on the HTTP request (no `IHttpContextAccessor`), and concurrent tool calls never share a `DbContext`. The agent does not add a second function invocation when the chat client already has one.
- **Expected tool errors become the tool's result:** the same `ToolError` JSON as the MCP server's tool errors (`code`, `message`, `status`, `errors`), so the model can react, e.g. to `class-not-found`. Unexpected errors only tell the model that the call failed (details are logged).
- **Stateless:** no `AgentSessionStore` is registered, so nothing is kept between runs. AG-UI clients send the whole conversation (`messages`) with every run.
- **Not part of the OpenAPI spec** (`ExcludeFromDescription`): the generated API clients do not change, and the CLI has no assistant command. It lives under `/api`, so the Angular dev server proxies it like the rest of the API.
- **No authentication**, like the rest of the API (see [web-api.md](web-api.md#deliberately-not-done)).

## Configuration

The LLM is OpenRouter's OpenAI-compatible Chat Completions API, configuration section `OpenRouter`:

| Value | Where | |
| --- | --- | --- |
| `OpenRouter:Endpoint` | `appsettings.json` | `https://openrouter.ai/api/v1` |
| `OpenRouter:Model` | `appsettings.json` | OpenRouter's model id, e.g. `z-ai/glm-5.3-flash` |
| `OpenRouter:ApiKey` | user secrets (development) or environment variable `OpenRouter__ApiKey` | **Never in the repository.** |

Without an API key, the app starts as usual (a `NotConfiguredChatClient` stands in for the LLM) and only the assistant fails: `POST /api/assistant` answers `503` with the problem code `assistant-not-configured`.

## Running it

```bash
dotnet user-secrets set "OpenRouter:ApiKey" "<key>" --project GraderApi   # once per machine
dotnet run --project GraderApi --launch-profile http
```

Outside of Development (no user secrets), set the environment variable `OpenRouter__ApiKey` instead. Without the web UI, send an AG-UI run with curl (`-N` streams the events as they arrive):

```bash
curl -N http://localhost:5100/api/assistant -H 'Content-Type: application/json' -d '{
  "threadId": "thread-1", "runId": "run-1", "tools": [], "context": [], "forwardedProps": {},
  "messages": [{ "id": "message-1", "role": "user", "content": "Which classes are there?" }] }'
```

## Testing

Tests never call a real LLM: `ScriptedChatClient` (`GraderTests`) replaces the keyed `IChatClient` (`AssistantSetup.ChatClientKey`) with scripted responses, e.g. a tool call, then the answer, and records the requests. `AssistantIntegrationTests` sends an AG-UI run through the test server's `HttpClient`, parses the server-sent events and asserts the tool call, its result and the answer. See [testing.md](testing.md).

## Gotchas

- **The AG-UI hosting package is a preview** (`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`, no stable version exists yet). It depends on AG-UI's .NET SDK 0.0.6 (`AGUI.Server`), although AG-UI 1.0 is out; do not reference a newer `AGUI.*` version directly unless the hosting package supports it.
- **The events contain `null` for absent optional fields** (`timestamp`, `metadata`, `parentRunId`, ...), which AG-UI 1.0 clients reject. The events are serialized with the SDK's own source-generated context, so our JSON options cannot change it; the web UI removes the `null`s (see [ui.md](ui.md#gotchas)).
- **The method names changed:** `AddAGUIServer` and `MapAGUIServer`; older docs and blog posts use `AddAGUI`/`MapAGUI`, and `CreateAIAgent` instead of `AsAIAgent`.
- **The agent is resolved when the endpoint is mapped**, not per request. A scoped agent registration does not work, and a test that replaces the chat client needs its own host (`WithWebHostBuilder`).
- **AG-UI hides exception details:** any exception during a run becomes a generic `RUN_ERROR` event. That is why a missing API key is checked in an endpoint filter before the run starts.
- **The endpoint uses the API's HTTP JSON options** (strict numbers, required constructor parameters, see [web-api.md](web-api.md#validation)); `AddAGUIServer` adds the AG-UI types to them.
- **`WebApplicationFactory` runs in Development, which loads user secrets:** `GraderApiFactory` sets `OpenRouter:ApiKey` to an empty value, so a developer's key never makes tests call OpenRouter.
