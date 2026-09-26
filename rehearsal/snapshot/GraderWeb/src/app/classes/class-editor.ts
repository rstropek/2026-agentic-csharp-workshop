import {
  afterNextRender,
  Component,
  computed,
  effect,
  Injector,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  applyEach,
  FormField,
  FormRoot,
  form,
  maxLength,
  readonly,
  SchemaPath,
  validate,
} from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { LucideX } from '@lucide/angular';
import { firstValueFrom, Observable, tap } from 'rxjs';
import { ClassDetails, ClassManagementService, SaveClassRequest } from '../api';
import { toApiError } from '../shared/api-error';
import { ConfirmService } from '../shared/confirm';
import { ariaInvalid, FieldError } from '../shared/field-error';
import { LoadError } from '../shared/load-error';
import { SchoolYearStore } from '../shared/school-year';
import { Skeleton } from '../shared/skeleton';

/** Limits of the API (`SchoolClass.NameMaxLength`, `Student.*MaxLength` in GraderLogic). */
export const classNameMaxLength = 50;
export const firstNameMaxLength = 50;
export const lastNameMaxLength = 75;

/** A row of the students grid; `id` is `null` for students that are not saved yet. */
interface StudentRow {
  id: number | null;
  lastName: string;
  firstName: string;
}

interface ClassModel {
  name: string;
  students: StudentRow[];
}

/** Router `info` of the navigation to a class that was just created, to show it as saved. */
const createdInfo = 'class-created';

/**
 * Creates or edits a class incl. its students as one document: Save sends the class name and the
 * complete student list in one request, the response replaces the local state.
 */
@Component({
  selector: 'app-class-editor',
  imports: [FormField, FormRoot, RouterLink, LucideX, FieldError, LoadError, Skeleton],
  templateUrl: './class-editor.html',
  styleUrl: './class-editor.css',
})
export class ClassEditor {
  /** Route parameter; undefined for a new class. */
  readonly classId = input<string>();

  private readonly api = inject(ClassManagementService);
  private readonly router = inject(Router);
  private readonly confirmService = inject(ConfirmService);
  private readonly injector = inject(Injector);
  private readonly schoolYears = inject(SchoolYearStore);

  protected readonly isNew = computed(() => this.classId() === undefined);

  protected readonly loaded = rxResource({
    params: () => {
      const classId = this.classId();
      return classId === undefined ? undefined : Number(classId);
    },
    stream: ({ params: classId }) =>
      // A class opened via link shows its school year in the picker.
      this.api
        .getClass(classId)
        .pipe(tap((loaded) => this.schoolYears.select(loaded.schoolYearId))),
  });

  /** The last loaded or saved state of an existing class. */
  protected readonly savedClass = computed(() =>
    this.loaded.hasValue() ? this.loaded.value() : undefined,
  );

  /** School year of a new class: the one selected when the editor was opened. */
  private readonly newClassSchoolYearId = linkedSignal<number | undefined, number | undefined>({
    source: this.schoolYears.selectedId,
    computation: (selected, previous) => previous?.value ?? selected,
  });

  private readonly schoolYearId = computed(() =>
    this.isNew() ? this.newClassSchoolYearId() : this.savedClass()?.schoolYearId,
  );

  protected readonly model = linkedSignal<ClassModel>(() => toModel(this.savedClass()));

  protected readonly classForm = form(
    this.model,
    (path) => {
      requiredText(path.name, 'Class name is required');
      maxLength(path.name, classNameMaxLength, {
        message: `Class name must be at most ${classNameMaxLength} characters`,
      });
      applyEach(path.students, (student) => {
        requiredText(student.lastName, 'Last name is required');
        maxLength(student.lastName, lastNameMaxLength, {
          message: `Last name must be at most ${lastNameMaxLength} characters`,
        });
        requiredText(student.firstName, 'First name is required');
        maxLength(student.firstName, firstNameMaxLength, {
          message: `First name must be at most ${firstNameMaxLength} characters`,
        });
      });
      // Fields stay visible but read-only while saving or deleting.
      readonly(path, () => this.busy());
    },
    {
      submission: {
        action: () => this.saveClass(),
        onInvalid: () => this.showValidationErrors(),
      },
    },
  );

  protected readonly deleting = signal(false);
  protected readonly busy = computed(() => this.classForm().submitting() || this.deleting());

  /** Message of the last failed save or delete; shown in place of the error count. */
  protected readonly serverError = signal<string | undefined>(undefined);
  /** Whether saving was attempted with invalid fields; shows the error count until fixed. */
  private readonly saveAttempted = signal(false);
  private readonly justSaved = signal(this.router.currentNavigation()?.extras.info === createdInfo);

  protected readonly invalidFieldCount = computed(
    () =>
      new Set(
        this.classForm()
          .errorSummary()
          .map((error) => error.fieldTree),
      ).size,
  );
  protected readonly showErrorCount = computed(
    () => this.saveAttempted() && this.invalidFieldCount() > 0,
  );
  protected readonly showSaved = computed(() => this.justSaved() && !this.classForm().dirty());

  /** Row numbers of saved students; new rows have none. */
  protected readonly rowNumbers = computed(() => {
    let number = 0;
    return this.model().students.map((student) => (student.id === null ? undefined : ++number));
  });

  protected readonly title = computed(() =>
    this.isNew() ? 'New class' : (this.savedClass()?.name ?? ''),
  );

  protected readonly ariaInvalid = ariaInvalid;
  protected readonly skeletonRows = Array.from({ length: 12 }, (_, index) => index);

  constructor() {
    // The class belongs to one school year: selecting another one goes back to the class list.
    effect(() => {
      const selected = this.schoolYears.selectedId();
      const schoolYearId = this.schoolYearId();
      if (selected !== undefined && schoolYearId !== undefined && selected !== schoolYearId) {
        this.router.navigate(['/classes']);
      }
    });
  }

  protected addStudent() {
    this.model.update((model) => ({
      ...model,
      students: [{ id: null, lastName: '', firstName: '' }, ...model.students],
    }));
    afterNextRender(() => this.classForm.students[0].lastName().focusBoundControl(), {
      injector: this.injector,
    });
  }

  protected removeStudent(index: number) {
    this.model.update((model) => ({
      ...model,
      students: model.students.filter((_, i) => i !== index),
    }));
  }

  protected removeLabel(student: StudentRow) {
    const name = [student.lastName.trim(), student.firstName.trim()].filter(Boolean).join(', ');
    return name ? `Remove ${name}` : 'Remove new student';
  }

  protected cancel() {
    this.router.navigate(['/classes']);
  }

  protected async deleteClass() {
    const schoolClass = this.savedClass();
    if (!schoolClass) {
      return;
    }
    const confirmed = await this.confirmService.confirm({
      title: `Delete class ${schoolClass.name}?`,
      message: `The class and its ${pluralize(schoolClass.students.length, 'student')} will be deleted. This cannot be undone.`,
      confirmLabel: 'Delete class',
    });
    if (!confirmed) {
      return;
    }

    this.serverError.set(undefined);
    this.deleting.set(true);
    try {
      await firstValueFrom(this.api.deleteClass(schoolClass.id));
      await this.router.navigate(['/classes']);
    } catch (error) {
      this.serverError.set(toApiError(error).message);
    } finally {
      this.deleting.set(false);
    }
  }

  private async saveClass() {
    this.serverError.set(undefined);
    this.justSaved.set(false);
    const request = toRequest(this.model());
    const schoolClass = this.savedClass();
    const schoolYearId = this.schoolYearId();
    let save: Observable<ClassDetails>;
    if (!this.isNew() && schoolClass) {
      save = this.api.saveClass(schoolClass.id, request);
    } else if (this.isNew() && schoolYearId !== undefined) {
      save = this.api.createClass(schoolYearId, request);
    } else {
      return;
    }

    try {
      const saved = await firstValueFrom(save);
      this.saveAttempted.set(false);
      if (this.isNew()) {
        await this.router.navigate(['/classes', saved.id], {
          replaceUrl: true,
          info: createdInfo,
        });
        return;
      }
      // The response is the saved document (e.g. sorted, with ids): it replaces the local state.
      this.loaded.set(saved);
      this.classForm().reset();
      this.justSaved.set(true);
    } catch (error) {
      this.serverError.set(toApiError(error).message);
    }
  }

  private showValidationErrors() {
    this.serverError.set(undefined);
    this.justSaved.set(false);
    this.saveAttempted.set(true);
    this.classForm().errorSummary()[0]?.fieldTree().focusBoundControl();
  }
}

function pluralize(count: number, noun: string) {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}

/** Like `required`, but whitespace does not count (as in the API). */
function requiredText(path: SchemaPath<string>, message: string) {
  validate(path, ({ value }) => (value().trim() ? undefined : { kind: 'required', message }));
}

function toModel(schoolClass: ClassDetails | undefined): ClassModel {
  return {
    name: schoolClass?.name ?? '',
    students: (schoolClass?.students ?? []).map(({ id, lastName, firstName }) => ({
      id,
      lastName,
      firstName,
    })),
  };
}

function toRequest(model: ClassModel): SaveClassRequest {
  return {
    name: model.name.trim(),
    students: model.students.map(({ id, lastName, firstName }) => ({
      id,
      lastName: lastName.trim(),
      firstName: firstName.trim(),
    })),
  };
}
