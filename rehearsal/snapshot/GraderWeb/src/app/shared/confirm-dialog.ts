import { afterRenderEffect, Component, ElementRef, inject, viewChild } from '@angular/core';
import { ConfirmService } from './confirm';

/**
 * Modal confirmation dialog for destructive actions, rendered once by the app shell and opened via
 * `ConfirmService`. Uses the native `<dialog>` (focus trap, Escape, backdrop). Cancel has the focus,
 * so Enter never confirms by accident.
 */
@Component({
  selector: 'app-confirm-dialog',
  template: `
    <dialog
      #dialog
      class="dialog"
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-message"
      (close)="onClose()"
    >
      @if (request(); as request) {
        <h2 id="confirm-dialog-title" class="section-title">{{ request.title }}</h2>
        <p id="confirm-dialog-message" class="message">{{ request.message }}</p>
        <div class="toolbar">
          <button type="button" class="btn btn-primary" (click)="answer(true)">
            {{ request.confirmLabel }}
          </button>
          <button #cancel type="button" class="btn btn-secondary" (click)="answer(false)">
            Cancel
          </button>
        </div>
      }
    </dialog>
  `,
  styles: `
    .dialog {
      width: 480px;
      padding: var(--space-6);
      border: var(--rule-strong);
      border-radius: 0;
      background: var(--color-bg);
      color: var(--color-text);
    }

    .dialog::backdrop {
      background: var(--color-backdrop);
    }

    .message {
      margin: var(--space-3) 0 var(--space-6);
    }
  `,
})
export class ConfirmDialog {
  private readonly confirmService = inject(ConfirmService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancel');

  protected readonly request = this.confirmService.request;

  constructor() {
    afterRenderEffect(() => {
      const dialog = this.dialog().nativeElement;
      if (this.request() && !dialog.open) {
        dialog.showModal();
        this.cancelButton()?.nativeElement.focus();
      } else if (!this.request() && dialog.open) {
        dialog.close();
      }
    });
  }

  protected answer(confirmed: boolean) {
    this.confirmService.answer(confirmed);
  }

  /** Closed by the browser (Escape): counts as cancel. */
  protected onClose() {
    if (this.request()) {
      this.confirmService.answer(false);
    }
  }
}
