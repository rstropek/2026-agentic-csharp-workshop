import { Component, computed, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';

/**
 * Shows the first validation error of a form field below the control once the field was touched
 * (on blur, or for all fields when saving). Give it an `id` and reference it with
 * `aria-describedby` from the control.
 */
@Component({
  selector: 'app-field-error',
  template: `
    @if (message(); as message) {
      <span class="field-error">{{ message }}</span>
    }
  `,
})
export class FieldError {
  readonly field = input.required<FieldTree<unknown>>();
  /**
   * Message for input the browser cannot parse (e.g. "1e" in a number field); Signal Forms report
   * it as a `parse` error without a message.
   */
  readonly parseMessage = input('Invalid value');

  protected readonly message = computed(() => {
    const state = this.field()();
    const error = state.touched() ? state.errors()[0] : undefined;
    return error && (error.message ?? (error.kind === 'parse' ? this.parseMessage() : undefined));
  });
}

/** `aria-invalid` value for a control: invalid fields are only marked once touched. */
export function ariaInvalid(field: FieldTree<unknown>): 'true' | null {
  const state = field();
  return state.touched() && state.invalid() ? 'true' : null;
}
