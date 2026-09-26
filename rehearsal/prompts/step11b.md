Add an AI assistant to the Web API: an agent built with Microsoft Agent Framework that answers a teacher's questions about classes, exams and results. The web UI will talk to it via the AG-UI protocol.

- Research AG-UI with https://docs.ag-ui.com/llms.txt and add it to "Researching Documentation" in AGENTS.md.
- The agent uses the read-only tools of the shared tool catalog; it never changes data.
- LLM via OpenRouter's OpenAI-compatible Chat Completions API. API key from the configuration value `OpenRouter:ApiKey` (user secrets or environment variable, never in the repo), model `z-ai/glm-5.3-flash` in appsettings.json. Without a key, the app still starts and only the assistant reports an error.
- Instructions: short answers in the markdown subset of spec/design/assistant.md; the newest school year unless the teacher names one; never invent data.
- Exemplary test: an AG-UI run against the Web API in memory with a fake chat client that calls a tool; the tool call, its result and the answer appear in the event stream.
- New tech docs article tech-docs/assistant.md (architecture, configuration, running it); update AGENTS.md and testing.md.
- Done when the tests and the QA script are green (no manual checks against a running API or a real LLM). Then commit directly to main.
