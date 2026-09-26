import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, required, SchemaPath, validate } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { CalculatorService, ProblemDetails } from '../api';

@Component({
  selector: 'app-calculator',
  imports: [FormField, FormRoot],
  templateUrl: './calculator.html',
  styleUrl: './calculator.css',
})
export class Calculator {
  private readonly calculatorService = inject(CalculatorService);

  protected readonly model = signal({ a: 0, b: 0 });
  protected readonly result = signal<number | undefined>(undefined);
  protected readonly error = signal<string | undefined>(undefined);

  protected readonly calculatorForm = form(
    this.model,
    (path) => {
      wholeNumber(path.a);
      wholeNumber(path.b);
    },
    {
      submission: {
        action: async () => {
          this.result.set(undefined);
          this.error.set(undefined);
          try {
            const { a, b } = this.model();
            const response = await firstValueFrom(this.calculatorService.add(a, b));
            this.result.set(response.result);
          } catch (err) {
            this.error.set(toErrorMessage(err));
          }
        },
      },
    },
  );
}

function wholeNumber(path: SchemaPath<number>) {
  required(path, { message: 'Please enter a number.' });
  validate(path, ({ value }) =>
    Number.isInteger(value())
      ? undefined
      : { kind: 'wholeNumber', message: 'Please enter a whole number.' },
  );
}

function toErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    // The API reports errors as RFC 9457 problem details (see GraderLogicExceptionHandler).
    const problem = err.error as ProblemDetails | null;
    const message = problem?.detail || problem?.title;
    if (message) {
      return message;
    }
    if (err.status === 0) {
      return 'The server is not reachable.';
    }
  }
  return 'An unexpected error occurred.';
}
