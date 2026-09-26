import { Service, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  /** Label of the button that confirms, e.g. "Delete class". */
  confirmLabel: string;
}

export interface ConfirmRequest extends ConfirmOptions {
  resolve: (confirmed: boolean) => void;
}

/**
 * Asks the user to confirm a destructive action. The dialog itself is rendered once by the app
 * shell (`ConfirmDialog`).
 */
@Service()
export class ConfirmService {
  private readonly current = signal<ConfirmRequest | undefined>(undefined);

  /** The open confirmation, if any. */
  readonly request = this.current.asReadonly();

  /** Opens the confirmation dialog; resolves to `true` if the user confirmed. */
  confirm(options: ConfirmOptions): Promise<boolean> {
    this.current()?.resolve(false);
    return new Promise((resolve) => {
      this.current.set({ ...options, resolve });
    });
  }

  /** Closes the open confirmation with the user's answer. */
  answer(confirmed: boolean) {
    const request = this.current();
    this.current.set(undefined);
    request?.resolve(confirmed);
  }
}
