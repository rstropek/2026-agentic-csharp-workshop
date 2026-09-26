import { Component, computed, input, output } from '@angular/core';
import { toApiError } from './api-error';

/** Shows why data could not be loaded, with a button to try again. */
@Component({
  selector: 'app-load-error',
  template: `
    <div class="load-error" role="alert">
      <p class="error-text">{{ message() }}</p>
      <button type="button" class="btn btn-secondary" (click)="retry.emit()">Try again</button>
    </div>
  `,
  styles: `
    .load-error {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: var(--space-3);
      padding: var(--space-6) 0;
    }
  `,
})
export class LoadError {
  /** The error of the failed request (any error, e.g. a resource's `error()`). */
  readonly error = input.required<unknown>();
  /** What could not be loaded, e.g. "The classes". */
  readonly subject = input.required<string>();
  readonly retry = output();

  protected readonly message = computed(
    () => `${this.subject()} could not be loaded. ${toApiError(this.error()).message}`,
  );
}
