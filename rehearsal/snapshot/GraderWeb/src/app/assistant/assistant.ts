import { Message, randomUUID } from '@ag-ui/client';
import {
  afterNextRender,
  afterRenderEffect,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  Injector,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { ASSISTANT_AGENT_FACTORY, toAssistantError } from './assistant-agent';
import { toTurns } from './conversation';
import { Markdown } from './markdown';
import { ToolCall } from './tool-call';

/** Distance from the bottom (px) up to which the history counts as scrolled to the newest message. */
const bottomTolerance = 32;

/**
 * Assistant area: a chat with the AI assistant of the Web API (AG-UI). The conversation lives as long
 * as the component: leaving the area ends it. Answer text and tool calls render while they stream in.
 */
@Component({
  selector: 'app-assistant',
  imports: [Markdown, ToolCall],
  templateUrl: './assistant.html',
  styleUrl: './assistant.css',
})
export class Assistant {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private readonly agent = inject(ASSISTANT_AGENT_FACTORY)();
  private readonly history = viewChild.required<ElementRef<HTMLElement>>('history');
  private readonly field = viewChild.required<ElementRef<HTMLTextAreaElement>>('field');

  /** The AG-UI messages of the conversation, updated on every event of a run. */
  private readonly messages = signal<readonly Message[]>([]);
  protected readonly answering = signal(false);
  protected readonly error = signal<string | undefined>(undefined);
  protected readonly draft = signal('');
  protected readonly turns = computed(() => toTurns(this.messages(), this.answering()));

  /** Whether new content scrolls the history down; off while the teacher scrolls up to read. */
  private followNewest = true;

  constructor() {
    const subscription = this.agent.subscribe({
      onMessagesChanged: ({ messages }) => this.messages.set([...messages]),
      onRunErrorEvent: ({ event }) => this.error.set(event.message),
    });
    this.destroyRef.onDestroy(() => {
      subscription.unsubscribe();
      this.agent.abortRun();
    });

    afterRenderEffect({
      write: () => {
        this.turns();
        this.error();
        if (this.followNewest) {
          const history = this.history().nativeElement;
          history.scrollTop = history.scrollHeight;
        }
      },
    });
  }

  protected onScroll() {
    const { scrollTop, scrollHeight, clientHeight } = this.history().nativeElement;
    this.followNewest = scrollHeight - scrollTop - clientHeight <= bottomTolerance;
  }

  /** Enter sends, Shift+Enter adds a line (the browser's default). */
  protected onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      this.send();
    }
  }

  protected async send() {
    const text = this.draft().trim();
    if (!text || this.answering()) {
      return;
    }

    this.error.set(undefined);
    this.draft.set('');
    this.agent.addMessage({ id: randomUUID(), role: 'user', content: text });
    this.messages.set([...this.agent.messages]);
    this.followNewest = true;
    this.answering.set(true);
    try {
      await this.agent.runAgent();
    } catch (error) {
      this.error.set(toAssistantError(error).message);
    } finally {
      this.answering.set(false);
      // Leaving the area aborts the run; otherwise the field is enabled again after the next render.
      if (!this.destroyRef.destroyed) {
        afterNextRender(() => this.field().nativeElement.focus(), { injector: this.injector });
      }
    }
  }
}
