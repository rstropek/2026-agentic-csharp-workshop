import { Component, inject } from '@angular/core';
import { SchoolYearStore } from '../shared/school-year';

/** Placeholder until the exams use case area is built. */
@Component({
  selector: 'app-exams',
  template: `
    <div class="page">
      <p class="overline">School year {{ schoolYears.selected()?.name }}</p>
      <h1 class="page-title">Exams</h1>
      <p class="muted note">Exams and results entry are coming soon.</p>
    </div>
  `,
  styles: `
    .page-title {
      margin-top: var(--space-1);
    }

    .note {
      margin-top: var(--space-6);
    }
  `,
})
export class Exams {
  protected readonly schoolYears = inject(SchoolYearStore);
}
