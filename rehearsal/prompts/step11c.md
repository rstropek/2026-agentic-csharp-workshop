Add the Assistant area to the web UI: a chat with the AI assistant of the Web API (mockups 06–08 and assistant.md in spec/design/).

- Talk to the AG-UI endpoint with `@ag-ui/client` and render the markdown answers with `marked`; no chat UI framework. Answer text and tool calls appear while they stream in.
- The conversation lasts until the teacher leaves the Assistant area.
- Exemplary component test with a mocked agent: a tool call shows "Running…", then "Done", then the answer renders as markdown.
- Inspect the result in a real browser (e.g. Claude in Chrome) at 1920×1080, in light and dark mode, with a real question to the assistant. Compare it with the mockups and fix the differences.
- Keep tech-docs/ui.md up to date.
- Done when the QA script is green. Then commit directly to main.
