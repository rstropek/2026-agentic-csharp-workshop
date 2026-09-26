import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { ClassManagementService } from '../api';
import { LoadError } from '../shared/load-error';
import { SchoolYearStore } from '../shared/school-year';
import { Skeleton } from '../shared/skeleton';

/** Classes of the selected school year as a grid; each cell opens the class editor. */
@Component({
  selector: 'app-class-list',
  imports: [RouterLink, LoadError, Skeleton],
  templateUrl: './class-list.html',
  styleUrl: './class-list.css',
})
export class ClassList {
  private readonly api = inject(ClassManagementService);
  private readonly router = inject(Router);
  protected readonly schoolYears = inject(SchoolYearStore);

  /** Reloads when another school year is selected; a newer selection cancels the pending request. */
  protected readonly classes = rxResource({
    params: () => this.schoolYears.selectedId(),
    stream: ({ params: schoolYearId }) => this.api.getClasses(schoolYearId),
  });

  /** Number of skeleton cells while loading (two rows). */
  protected readonly placeholders = Array.from({ length: 10 }, (_, index) => index);

  protected newClass() {
    this.router.navigate(['/classes/new']);
  }
}
