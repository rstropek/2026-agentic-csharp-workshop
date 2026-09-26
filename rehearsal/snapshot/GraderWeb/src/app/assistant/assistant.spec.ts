import { AbstractAgent, BaseEvent, EventType, RunAgentInput } from '@ag-ui/client';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, ReplaySubject } from 'rxjs';
import { Assistant } from './assistant';
import { ASSISTANT_AGENT_FACTORY } from './assistant-agent';

/** An agent whose run emits the events the test pushes, instead of calling the API. */
class ScriptedAgent extends AbstractAgent {
  readonly inputs: RunAgentInput[] = [];
  private events = new ReplaySubject<BaseEvent>();

  override run(input: RunAgentInput): Observable<BaseEvent> {
    this.inputs.push(input);
    this.events = new ReplaySubject<BaseEvent>();
    return this.events;
  }

  /** Emits events of the current run; `threadId`/`runId` are taken from its input. */
  emit(...events: BaseEvent[]) {
    const { threadId, runId } = this.inputs[this.inputs.length - 1];
    for (const event of events) {
      this.events.next(
        event.type === EventType.RUN_STARTED || event.type === EventType.RUN_FINISHED
          ? { ...event, threadId, runId }
          : event,
      );
    }
  }

  finish() {
    this.emit({ type: EventType.RUN_FINISHED } as BaseEvent);
    this.events.complete();
  }

  fail(error: unknown) {
    this.events.error(error);
  }
}

describe('Assistant', () => {
  let fixture: ComponentFixture<Assistant>;
  let agent: ScriptedAgent;
  let element: HTMLElement;

  beforeEach(async () => {
    agent = new ScriptedAgent();
    TestBed.configureTestingModule({
      providers: [{ provide: ASSISTANT_AGENT_FACTORY, useValue: () => agent }],
    });
    fixture = TestBed.createComponent(Assistant);
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  /** The agent processes events asynchronously (its subscribers may be async). */
  async function settle() {
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
  }

  function button(name: string) {
    const found = Array.from(element.querySelectorAll('button')).find(
      (candidate) => candidate.textContent?.trim() === name,
    );
    if (!found) {
      throw new Error(`No button "${name}"`);
    }
    return found;
  }

  async function ask(question: string) {
    const field = element.querySelector('textarea') as HTMLTextAreaElement;
    field.value = question;
    field.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    button('Send').click();
    await settle();
  }

  function toolCallStatus() {
    return element.querySelector('app-tool-call summary .status')?.textContent?.trim();
  }

  it('shows a tool call running, then done, then the answer as markdown', async () => {
    await ask('Which students in 2B scored below 70 %?');

    expect(agent.inputs[0].messages).toEqual([
      expect.objectContaining({ role: 'user', content: 'Which students in 2B scored below 70 %?' }),
    ]);
    expect(element.querySelector('.user')?.textContent).toContain(
      'Which students in 2B scored below 70 %?',
    );
    expect(button('Answering…').disabled).toBe(true);
    expect((element.querySelector('textarea') as HTMLTextAreaElement).disabled).toBe(true);

    agent.emit(
      { type: EventType.RUN_STARTED } as BaseEvent,
      {
        type: EventType.TOOL_CALL_START,
        toolCallId: 'call-1',
        toolCallName: 'exam_results_get',
      } as BaseEvent,
      {
        type: EventType.TOOL_CALL_ARGS,
        toolCallId: 'call-1',
        delta: '{"examId":412}',
      } as BaseEvent,
      { type: EventType.TOOL_CALL_END, toolCallId: 'call-1' } as BaseEvent,
    );
    await settle();

    expect(element.querySelector('app-tool-call .name')?.textContent).toBe('exam_results_get');
    expect(toolCallStatus()).toBe('Running…');
    expect(element.querySelector('app-tool-call details')?.hasAttribute('open')).toBe(false);
    expect(element.querySelector('app-tool-call .details')?.textContent).toContain(
      'Waiting for result…',
    );

    agent.emit({
      type: EventType.TOOL_CALL_RESULT,
      messageId: 'result-1',
      toolCallId: 'call-1',
      content: '[{"student":"Walsh, Finn","percentage":58}]',
    } as BaseEvent);
    await settle();

    expect(toolCallStatus()).toBe('Done');
    expect(element.querySelector('app-tool-call .details')?.textContent).toContain(
      '{ "student": "Walsh, Finn", "percentage": 58 }',
    );

    agent.emit(
      { type: EventType.TEXT_MESSAGE_START, messageId: 'answer-1', role: 'assistant' } as BaseEvent,
      {
        type: EventType.TEXT_MESSAGE_CONTENT,
        messageId: 'answer-1',
        delta: 'One student scored below 70 %:\n\n',
      } as BaseEvent,
      {
        type: EventType.TEXT_MESSAGE_CONTENT,
        messageId: 'answer-1',
        delta: '- **Walsh, Finn**: 58 %',
      } as BaseEvent,
      { type: EventType.TEXT_MESSAGE_END, messageId: 'answer-1' } as BaseEvent,
    );
    agent.finish();
    await settle();

    const answer = element.querySelector('app-markdown') as HTMLElement;
    expect(answer.querySelector('p')?.textContent).toBe('One student scored below 70 %:');
    expect(answer.querySelector('ul > li strong')?.textContent).toBe('Walsh, Finn');
    expect(answer.textContent).not.toContain('**');
    expect(toolCallStatus()).toBe('Done');
    expect(button('Send').disabled).toBe(false);
  });

  it('shows why the assistant could not answer and lets the teacher ask again', async () => {
    // The AG-UI client logs failed runs.
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    await ask('Which classes are there?');

    agent.fail(
      Object.assign(new Error('HTTP 503'), {
        status: 503,
        payload: JSON.stringify({
          status: 503,
          title: 'The assistant is not configured.',
          detail: 'The API needs an API key for OpenRouter.',
          code: 'assistant-not-configured',
        }),
      }),
    );
    await settle();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'The assistant could not answer. The API needs an API key for OpenRouter.',
    );
    expect(button('Send').disabled).toBe(false);
    expect(document.activeElement).toBe(element.querySelector('textarea'));
  });
});
