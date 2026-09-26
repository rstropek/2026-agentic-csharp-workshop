import { Component, input } from '@angular/core';
import { FieldTree, FormField, max, min, SchemaPath } from '@angular/forms/signals';
import { ariaInvalid, FieldError } from './field-error';

/** Limits of the API (`ExamResult.MinPercentage`/`MaxPercentage` in GraderLogic). */
export const minPercentage = 0;
export const maxPercentage = 100;
const rangeMessage = `Must be ${minPercentage}–${maxPercentage}`;

/** Schema rules of a percentage field: empty (`null`, no result) or 0 to 100 inclusive. */
export function percentage(path: SchemaPath<number | null>) {
  min(path, minPercentage, { message: rangeMessage });
  max(path, maxPercentage, { message: rangeMessage });
}

let nextId = 0;

/**
 * Percentage input: native number field (decimals allowed), "%" after it, validation error below.
 * Empty means no result (`null`). Validate the field with `percentage()` in the form schema.
 */
@Component({
  selector: 'app-percentage-field',
  imports: [FormField, FieldError],
  template: `
    <span class="control">
      <input
        type="number"
        step="any"
        class="input tabular"
        placeholder="No result"
        [formField]="field()"
        [attr.aria-label]="label()"
        [attr.aria-invalid]="ariaInvalid(field())"
        [attr.aria-describedby]="errorId"
      />
      <span class="unit muted" aria-hidden="true">%</span>
    </span>
    <app-field-error [id]="errorId" [field]="field()" [parseMessage]="rangeMessage" />
  `,
  styles: `
    :host {
      display: block;
    }

    .control {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    input {
      width: 104px;
      text-align: right;
      /* No spin buttons: they would cover the right-aligned value; arrow keys still step. */
      appearance: textfield;
    }

    input::-webkit-inner-spin-button,
    input::-webkit-outer-spin-button {
      appearance: none;
      margin: 0;
    }

    .unit {
      font-size: var(--font-size-small);
    }
  `,
})
export class PercentageField {
  readonly field = input.required<FieldTree<number | null>>();
  /** Accessible name of the input, e.g. "Percentage of Anderson, Maya". */
  readonly label = input.required<string>();

  protected readonly errorId = `percentage-error-${nextId++}`;
  protected readonly rangeMessage = rangeMessage;
  protected readonly ariaInvalid = ariaInvalid;
}
