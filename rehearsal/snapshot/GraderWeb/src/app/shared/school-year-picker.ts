import { Component, inject } from '@angular/core';
import { SchoolYearStore } from './school-year';

/** The global school year select in the header. */
@Component({
  selector: 'app-school-year-picker',
  template: `
    <label class="picker">
      <span class="field-label">School year</span>
      <select class="input" [disabled]="store.list().length === 0" (change)="select($event)">
        @for (year of store.list(); track year.id) {
          <option [value]="year.id" [selected]="year.id === store.selectedId()">
            {{ year.name }}
          </option>
        } @empty {
          <option>{{ store.years.isLoading() ? 'Loading…' : store.years.error() ? 'Not available' : 'None' }}</option>
        }
      </select>
    </label>
  `,
  styles: `
    .picker {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    select {
      width: 136px;
    }
  `,
})
export class SchoolYearPicker {
  protected readonly store = inject(SchoolYearStore);

  protected select(event: Event) {
    this.store.select(Number((event.target as HTMLSelectElement).value));
  }
}
