import { computed, inject, linkedSignal, Service } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ClassManagementService, SchoolYearSummary } from '../api';

/**
 * Global school year selection, the context of all areas. The API returns the newest year first;
 * it is selected by default. The selection survives reloads of the list as long as the year exists.
 */
@Service()
export class SchoolYearStore {
  private readonly api = inject(ClassManagementService);

  readonly years = rxResource({
    stream: () => this.api.getSchoolYears(),
    defaultValue: [] as SchoolYearSummary[],
  });

  /** The loaded school years; empty while loading or after an error. */
  readonly list = computed(() => (this.years.hasValue() ? this.years.value() : []));

  readonly selectedId = linkedSignal<SchoolYearSummary[], number | undefined>({
    source: this.list,
    computation: (years, previous) =>
      years.find((year) => year.id === previous?.value)?.id ?? years[0]?.id,
  });

  readonly selected = computed(() => this.list().find((year) => year.id === this.selectedId()));

  select(id: number) {
    this.selectedId.set(id);
  }

  reload() {
    this.years.reload();
  }
}
