import { GraderAssistantAgent, toAssistantError } from './assistant-agent';

/** Events as the Web API sends them (AG-UI .NET SDK 0.x): absent optional fields are `null`. */
const serverEvents = [
  {
    type: 'RUN_STARTED',
    threadId: 'thread-1',
    runId: 'run-1',
    parentRunId: null,
    input: null,
    timestamp: null,
    rawEvent: null,
    metadata: null,
  },
  {
    type: 'TEXT_MESSAGE_START',
    messageId: 'answer-1',
    role: 'assistant',
    name: 'GraderAssistant',
    subagentRunId: null,
    timestamp: null,
    rawEvent: { role: 'assistant', contents: [] },
    metadata: null,
  },
  {
    type: 'TEXT_MESSAGE_CONTENT',
    messageId: 'answer-1',
    delta: 'There are **10 classes**.',
    timestamp: null,
    rawEvent: null,
    metadata: null,
  },
  {
    type: 'TEXT_MESSAGE_END',
    messageId: 'answer-1',
    timestamp: null,
    rawEvent: null,
    metadata: null,
  },
  {
    type: 'RUN_FINISHED',
    threadId: 'thread-1',
    runId: 'run-1',
    result: null,
    outcome: { type: 'success' },
    usage: [{ provider: null, model: 'z-ai/glm-5.3-flash', inputTokens: 783, outputTokens: 74 }],
    timestamp: null,
    rawEvent: null,
    metadata: null,
  },
];

describe('GraderAssistantAgent', () => {
  it('accepts the events of the Web API with null fields', async () => {
    const body = serverEvents.map((event) => `data: ${JSON.stringify(event)}\n\n`).join('');
    const fetch = vi.fn(
      async () => new Response(body, { headers: { 'Content-Type': 'text/event-stream' } }),
    );
    const agent = new GraderAssistantAgent({ url: '/api/assistant', threadId: 'thread-1', fetch });
    agent.addMessage({ id: 'question-1', role: 'user', content: 'How many classes are there?' });

    await agent.runAgent({ runId: 'run-1' });

    expect(fetch).toHaveBeenCalledWith(
      '/api/assistant',
      expect.objectContaining({ method: 'POST' }),
    );
    expect(agent.messages.at(-1)).toEqual(
      expect.objectContaining({ role: 'assistant', content: 'There are **10 classes**.' }),
    );
  });
});

describe('toAssistantError', () => {
  it('uses the problem details of an HTTP error', () => {
    const error = Object.assign(new Error('HTTP 503'), {
      status: 503,
      payload: '{"status":503,"detail":"Not configured.","code":"assistant-not-configured"}',
    });

    expect(toAssistantError(error)).toEqual({
      code: 'assistant-not-configured',
      status: 503,
      message: 'Not configured.',
    });
  });

  it('reports network errors', () => {
    expect(toAssistantError(new TypeError('Failed to fetch')).status).toBe(0);
  });
});
