import { formatDate } from '@angular/common';
import {
  Component,
  computed,
  effect,
  inject,
  input,
  LOCALE_ID,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { applyEach, FormField, FormRoot, form, readonly, required } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { firstValueFrom, map, Observable, tap } from 'rxjs';
import {
  ClassDetails,
  ClassManagementService,
  ExamDetails,
  ExamStudentResult,
  ExamsService,
  SaveExamRequest,
} from '../api';
import { toApiError } from '../shared/api-error';
import { ConfirmService } from '../shared/confirm';
import { ariaInvalid, FieldError } from '../shared/field-error';
import { LoadError } from '../shared/load-error';
import { PercentageField, percentage } from '../shared/percentage-field';
import { SchoolYearStore } from '../shared/school-year';
import { Skeleton } from '../shared/skeleton';
import { ExamsStore } from './exams-store';

/** The exam as loaded, or the draft of a new exam (without id and course). */
interface LoadedExam {
  id?: number;
  examDate: string;
  courseId?: number;
  courseName?: string;
  classId: number;
  className: string;
  schoolYearName: string;
  results: ExamStudentResult[];
}

interface ResultRow {
  studentId: number;
  lastName: string;
  firstName: string;
  /** `null`: no result. */
  percentage: number | null;
}

interface ExamModel {
  /** ISO date (`yyyy-MM-dd`), as used by `<input type="date">` and the API. */
  examDate: string;
  /** Course id as select value; empty if no course is selected yet. */
  courseId: string;
  results: ResultRow[];
}

/** Router `info` of the navigation to an exam that was just created, to show it as saved. */
const createdInfo = 'exam-created';

/**
 * Creates or edits an exam incl. the results of all students of its class as one document: Save
 * sends date, course and all results (empty = no result) in one request, the response replaces the
 * local state.
 */
@Component({
  selector: 'app-exam-editor',
  imports: [FormField, FormRoot, FieldError, LoadError, PercentageField, Skeleton],
  templateUrl: './exam-editor.html',
  styleUrl: './exam-editor.css',
})
export class ExamEditor {
  /** Route parameter; undefined for a new exam. */
  readonly examId = input<string>();

  private readonly examApi = inject(ExamsService);
  private readonly classApi = inject(ClassManagementService);
  private readonly router = inject(Router);
  private readonly confirmService = inject(ConfirmService);
  private readonly schoolYears = inject(SchoolYearStore);
  private readonly locale = inject(LOCALE_ID);
  protected readonly store = inject(ExamsStore);

  protected readonly isNew = computed(() => this.examId() === undefined);

  /** Class of a new exam: the one selected when the editor was opened. */
  private readonly newExamClassId = linkedSignal<number | undefined, number | undefined>({
    source: this.store.selectedClassId,
    computation: (selected, previous) => previous?.value ?? selected,
  });

  /** An existing exam, or the students of the class of a new exam. */
  protected readonly loaded = rxResource<
    LoadedExam,
    { examId: number } | { classId: number } | undefined
  >({
    params: () => {
      const examId = this.examId();
      if (examId !== undefined) {
        return { examId: Number(examId) };
      }
      const classId = this.newExamClassId();
      return classId === undefined ? undefined : { classId };
    },
    stream: ({ params }) =>
      'examId' in params
        ? // An exam opened via link shows its school year and class in the pickers.
          this.examApi.getExam(params.examId).pipe(
            tap((exam) => {
              this.schoolYears.select(exam.schoolYearId);
              this.store.selectClass(exam.classId);
            }),
          )
        : this.classApi.getClass(params.classId).pipe(map(toNewExam)),
  });

  protected readonly loadedExam = computed(() =>
    this.loaded.hasValue() ? this.loaded.value() : undefined,
  );

  protected readonly model = linkedSignal<ExamModel>(() => toModel(this.loadedExam()));

  protected readonly examForm = form(
    this.model,
    (path) => {
      required(path.examDate, { message: 'Date is required' });
      required(path.courseId, { message: 'Course is required' });
      applyEach(path.results, (result) => percentage(result.percentage));
      // Fields stay visible but read-only while saving or deleting.
      readonly(path, () => this.busy());
    },
    {
      submission: {
        action: () => this.saveExam(),
        onInvalid: () => this.showValidationErrors(),
      },
    },
  );

  protected readonly deleting = signal(false);
  protected readonly busy = computed(() => this.examForm().submitting() || this.deleting());

  // The component is reused when another exam of the list is opened: these states start over.
  /** Message of the last failed save or delete; shown in place of the error count. */
  protected readonly serverError = linkedSignal<string | undefined, string | undefined>({
    source: this.examId,
    computation: () => undefined,
  });
  /** Whether saving was attempted with invalid fields; shows the error count until fixed. */
  private readonly saveAttempted = linkedSignal({ source: this.examId, computation: () => false });
  private readonly openedAfterCreate = this.router.currentNavigation()?.extras.info === createdInfo;
  private readonly justSaved = linkedSignal<string | undefined, boolean>({
    source: this.examId,
    computation: (_, previous) => (previous === undefined ? this.openedAfterCreate : false),
  });

  protected readonly invalidFieldCount = computed(
    () =>
      new Set(
        this.examForm()
          .errorSummary()
          .map((error) => error.fieldTree),
      ).size,
  );
  protected readonly showErrorCount = computed(
    () => this.saveAttempted() && this.invalidFieldCount() > 0,
  );
  /**
   * Whether the form still shows the loaded or saved exam. Not `dirty()`: Signal Forms mark the form
   * dirty as soon as a date input is rendered.
   */
  private readonly unchanged = computed(
    () => JSON.stringify(this.model()) === JSON.stringify(toModel(this.loadedExam())),
  );
  protected readonly showSaved = computed(() => this.justSaved() && this.unchanged());

  protected readonly enteredCount = computed(
    () => this.model().results.filter((result) => result.percentage !== null).length,
  );

  /** Result row indexes of the two columns: first half left, second half right. */
  protected readonly columns = computed(() => {
    const indexes = this.model().results.map((_, index) => index);
    const half = Math.ceil(indexes.length / 2);
    return [indexes.slice(0, half), indexes.slice(half)].filter((column) => column.length > 0);
  });

  protected readonly title = computed(() =>
    this.isNew() ? 'New exam' : (this.loadedExam()?.courseName ?? ''),
  );

  protected readonly ariaInvalid = ariaInvalid;
  protected readonly skeletonColumns = [0, 1].map(() =>
    Array.from({ length: 10 }, (_, index) => index),
  );

  constructor() {
    // An exam belongs to one class: selecting another class (or school year) closes the editor.
    effect(() => {
      const selected = this.store.selectedClassId();
      const classId = this.isNew() ? this.newExamClassId() : this.loadedExam()?.classId;
      if (selected !== undefined && classId !== undefined && selected !== classId) {
        this.router.navigate(['/exams']);
      }
    });
  }

  protected async deleteExam() {
    const exam = this.loadedExam();
    if (exam?.id === undefined) {
      return;
    }
    const resultCount = exam.results.filter((result) => result.percentage !== null).length;
    const confirmed = await this.confirmService.confirm({
      title: `Delete ${exam.courseName} on ${this.formatDate(exam.examDate)}?`,
      message: `The exam of class ${exam.className} and its ${pluralize(resultCount, 'result')} will be deleted. This cannot be undone.`,
      confirmLabel: 'Delete exam',
    });
    if (!confirmed) {
      return;
    }

    this.serverError.set(undefined);
    this.deleting.set(true);
    try {
      await firstValueFrom(this.examApi.deleteExam(exam.id));
      this.store.exams.reload();
      await this.router.navigate(['/exams']);
    } catch (error) {
      this.serverError.set(toApiError(error).message);
    } finally {
      this.deleting.set(false);
    }
  }

  private async saveExam() {
    this.serverError.set(undefined);
    this.justSaved.set(false);
    const exam = this.loadedExam();
    if (!exam) {
      return;
    }
    const request = toRequest(this.model());
    const save: Observable<ExamDetails> =
      exam.id === undefined
        ? this.examApi.createExam(exam.classId, request)
        : this.examApi.saveExam(exam.id, request);

    try {
      const saved = await firstValueFrom(save);
      this.saveAttempted.set(false);
      // Date and course are shown in the list.
      this.store.exams.reload();
      if (this.isNew()) {
        await this.router.navigate(['/exams', saved.id], { replaceUrl: true, info: createdInfo });
        return;
      }
      // The response is the saved document: it replaces the local state.
      this.loaded.set(saved);
      this.examForm().reset();
      this.justSaved.set(true);
    } catch (error) {
      this.serverError.set(toApiError(error).message);
    }
  }

  private showValidationErrors() {
    this.serverError.set(undefined);
    this.justSaved.set(false);
    this.saveAttempted.set(true);
    this.examForm().errorSummary()[0]?.fieldTree().focusBoundControl();
  }

  private formatDate(date: string) {
    return formatDate(date, 'dd MMM yyyy', this.locale);
  }
}

function pluralize(count: number, noun: string) {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}

/** Today as ISO date in the user's time zone. */
function today() {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

function toNewExam(schoolClass: ClassDetails): LoadedExam {
  return {
    examDate: today(),
    classId: schoolClass.id,
    className: schoolClass.name,
    schoolYearName: schoolClass.schoolYearName,
    results: schoolClass.students.map((student) => ({
      studentId: student.id,
      lastName: student.lastName,
      firstName: student.firstName,
      percentage: null,
    })),
  };
}

function toModel(exam: LoadedExam | undefined): ExamModel {
  return {
    examDate: exam?.examDate ?? '',
    courseId: exam?.courseId === undefined ? '' : String(exam.courseId),
    results: (exam?.results ?? []).map(({ studentId, lastName, firstName, percentage }) => ({
      studentId,
      lastName,
      firstName,
      percentage,
    })),
  };
}

/** All students are sent; `null` means no result (an existing result is removed). */
function toRequest(model: ExamModel): SaveExamRequest {
  return {
    examDate: model.examDate,
    courseId: Number(model.courseId),
    results: model.results.map(({ studentId, percentage }) => ({ studentId, percentage })),
  };
}
