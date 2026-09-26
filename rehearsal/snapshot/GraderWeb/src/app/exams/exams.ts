import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LoadError } from '../shared/load-error';
import { SchoolYearStore } from '../shared/school-year';
import { Skeleton } from '../shared/skeleton';
import { ExamsStore } from './exams-store';

/**
 * Exams area: the exams of a class (class filter, list sorted by date) on the left, the selected
 * exam (child route) on the right.
 */
@Component({
  selector: 'app-exams',
  imports: [DatePipe, RouterLink, RouterLinkActive, RouterOutlet, LoadError, Skeleton],
  providers: [ExamsStore],
  templateUrl: './exams.html',
  styleUrl: './exams.css',
})
export class Exams {
  private readonly router = inject(Router);
  protected readonly schoolYears = inject(SchoolYearStore);
  protected readonly store = inject(ExamsStore);

  protected readonly examCount = computed(() => {
    const count = this.store.exams.hasValue() ? this.store.exams.value().length : 0;
    return `${count} ${count === 1 ? 'exam' : 'exams'}`;
  });

  /** Number of skeleton rows while loading. */
  protected readonly placeholders = Array.from({ length: 8 }, (_, index) => index);

  protected selectClass(event: Event) {
    this.store.selectClass(Number((event.target as HTMLSelectElement).value));
  }

  protected newExam() {
    this.router.navigate(['/exams/new']);
  }
}
