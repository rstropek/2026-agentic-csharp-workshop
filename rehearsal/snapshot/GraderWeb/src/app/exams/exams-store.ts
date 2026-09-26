import { computed, inject, Service, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ClassManagementService, ExamsService } from '../api';
import { SchoolYearStore } from '../shared/school-year';

/**
 * State of the Exams area, shared by the exam list and the exam editor: the classes of the selected
 * school year, the class filter, the exams of the selected class and the courses to pick from.
 * Provided by the `Exams` page, so it lives as long as the area is open.
 */
@Service({ autoProvided: false })
export class ExamsStore {
  private readonly classApi = inject(ClassManagementService);
  private readonly examApi = inject(ExamsService);
  private readonly schoolYears = inject(SchoolYearStore);

  readonly classes = rxResource({
    params: () => this.schoolYears.selectedId(),
    stream: ({ params: schoolYearId }) => this.classApi.getClasses(schoolYearId),
  });

  /** The loaded classes; empty while loading or after an error. */
  readonly classList = computed(() => (this.classes.hasValue() ? this.classes.value() : []));

  /**
   * The class the user (or an opened exam) asked for. Kept while the classes of another school year
   * load, so opening an exam of another year selects its class once the list is there.
   */
  private readonly requestedClassId = signal<number | undefined>(undefined);

  /** The selected class: the requested one if it is in the list, otherwise the first one. */
  readonly selectedClassId = computed(() => {
    const classes = this.classList();
    return (
      classes.find((schoolClass) => schoolClass.id === this.requestedClassId())?.id ??
      classes[0]?.id
    );
  });

  readonly selectedClass = computed(() =>
    this.classList().find((schoolClass) => schoolClass.id === this.selectedClassId()),
  );

  /** Exams of the selected class, sorted by date (by the API). */
  readonly exams = rxResource({
    params: () => this.selectedClassId(),
    stream: ({ params: classId }) => this.examApi.getExams(classId),
  });

  readonly courses = rxResource({ stream: () => this.examApi.getCourses() });

  selectClass(classId: number) {
    this.requestedClassId.set(classId);
  }
}
