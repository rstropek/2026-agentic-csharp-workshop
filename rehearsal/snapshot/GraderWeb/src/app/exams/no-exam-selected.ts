import { Component } from '@angular/core';

/** Detail area of the Exams page while no exam is open. */
@Component({
  selector: 'app-no-exam-selected',
  template: `
    <h2 class="visually-hidden">No exam selected</h2>
    <p class="muted">Select an exam in the list, or create one with “New exam”.</p>
  `,
})
export class NoExamSelected {}
