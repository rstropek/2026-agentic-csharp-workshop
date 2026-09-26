import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ClassManagementService, ExamDetails, ExamsService } from '../api';
import { ExamEditor } from './exam-editor';
import { ExamsStore } from './exams-store';

const exam: ExamDetails = {
  id: 493,
  examDate: '2026-09-24',
  courseId: 4,
  courseName: 'Computer Science',
  classId: 25,
  className: '2B',
  schoolYearId: 3,
  schoolYearName: '2026/27',
  results: [
    { studentId: 1, lastName: 'Anderson', firstName: 'Maya', percentage: 82 },
    { studentId: 2, lastName: 'Baker', firstName: 'Liam', percentage: 67.5 },
    { studentId: 3, lastName: 'Carter', firstName: 'Olivia', percentage: null },
  ],
};

describe('ExamEditor', () => {
  let fixture: ComponentFixture<ExamEditor>;
  let examApi: {
    getCourses: ReturnType<typeof vi.fn>;
    getExams: ReturnType<typeof vi.fn>;
    getExam: ReturnType<typeof vi.fn>;
    saveExam: ReturnType<typeof vi.fn>;
    createExam: ReturnType<typeof vi.fn>;
  };
  let classApi: {
    getSchoolYears: ReturnType<typeof vi.fn>;
    getClasses: ReturnType<typeof vi.fn>;
    getClass: ReturnType<typeof vi.fn>;
  };
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    examApi = {
      getCourses: vi.fn(() =>
        of([
          { id: 4, name: 'Computer Science' },
          { id: 1, name: 'Mathematics' },
        ]),
      ),
      getExams: vi.fn(() => of([])),
      getExam: vi.fn(() => of(exam)),
      saveExam: vi.fn(),
      createExam: vi.fn(),
    };
    classApi = {
      getSchoolYears: vi.fn(() => of([{ id: 3, name: '2026/27' }])),
      getClasses: vi.fn(() => of([{ id: 25, name: '2B', studentCount: 3 }])),
      getClass: vi.fn(() =>
        of({
          id: 25,
          name: '2B',
          schoolYearId: 3,
          schoolYearName: '2026/27',
          students: exam.results.map(({ studentId, lastName, firstName }) => ({
            id: studentId,
            lastName,
            firstName,
          })),
        }),
      ),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        ExamsStore,
        { provide: ExamsService, useValue: examApi },
        { provide: ClassManagementService, useValue: classApi },
      ],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  /** Opens the editor for an existing exam (`examId`) or a new one (undefined). */
  async function open(examId?: string) {
    fixture = TestBed.createComponent(ExamEditor);
    if (examId !== undefined) {
      fixture.componentRef.setInput('examId', examId);
    }
    await fixture.whenStable();
  }

  const element = () => fixture.nativeElement as HTMLElement;
  const percentage = (name: string) =>
    element().querySelector<HTMLInputElement>(
      `input[aria-label="Percentage of ${name}"]`,
    ) as HTMLInputElement;
  const percentages = () =>
    Array.from(element().querySelectorAll<HTMLInputElement>('input[type="number"]'));
  const button = (name: string) =>
    Array.from(element().querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === name,
    ) as HTMLButtonElement;

  /** Sets the value like a user; browsers fire `input` (and `change`) for text fields and selects. */
  function type(control: HTMLInputElement | HTMLSelectElement, value: string) {
    control.value = value;
    control.dispatchEvent(new Event('input'));
    control.dispatchEvent(new Event('change'));
  }

  /** Clicks a button and waits until async work (e.g. the submission) is done and rendered. */
  async function click(label: string) {
    button(label).click();
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
  }

  it('shows the exam with a result field per student', async () => {
    await open('493');

    expect(examApi.getExam).toHaveBeenCalledWith(493);
    expect(element().querySelector('h2')?.textContent).toBe('Computer Science');
    expect(element().textContent).toContain('Class 2B · 2026/27');
    expect(percentages().map((input) => input.value)).toEqual(['82', '67.5', '']);
    expect(element().textContent).toContain('2 of 3 entered');
  });

  it('saves date, course and all results, empty ones as no result', async () => {
    const saved: ExamDetails = {
      ...exam,
      examDate: '2026-09-25',
      courseId: 1,
      courseName: 'Mathematics',
      results: [
        { ...exam.results[0], percentage: null },
        { ...exam.results[1], percentage: 70 },
        { ...exam.results[2], percentage: 91.5 },
      ],
    };
    examApi.saveExam.mockReturnValue(of(saved));
    await open('493');

    type(element().querySelector('input[type="date"]') as HTMLInputElement, '2026-09-25');
    type(element().querySelector('select') as HTMLSelectElement, '1');
    type(percentage('Anderson, Maya'), '');
    type(percentage('Baker, Liam'), '70');
    type(percentage('Carter, Olivia'), '91.5');
    await fixture.whenStable();
    expect(element().textContent).toContain('2 of 3 entered');
    await click('Save exam');

    expect(examApi.saveExam).toHaveBeenCalledWith(493, {
      examDate: '2026-09-25',
      courseId: 1,
      results: [
        { studentId: 1, percentage: null },
        { studentId: 2, percentage: 70 },
        { studentId: 3, percentage: 91.5 },
      ],
    });
    // The response replaces the local state; the list shows the new date and course.
    expect(element().querySelector('h2')?.textContent).toBe('Mathematics');
    expect(element().querySelector('[role="status"]')?.textContent).toContain('Saved');
    expect(examApi.getExams).toHaveBeenCalledTimes(2);
  });

  it('does not save a percentage out of range and focuses it', async () => {
    await open('493');

    type(percentage('Carter, Olivia'), '104');
    await click('Save exam');

    expect(examApi.saveExam).not.toHaveBeenCalled();
    expect(element().textContent).toContain('1 field needs attention');
    expect(element().textContent).toContain('Must be 0–100');
    expect(percentage('Carter, Olivia').getAttribute('aria-invalid')).toBe('true');
    expect(document.activeElement).toBe(percentage('Carter, Olivia'));
  });

  it('shows the error message of the server and keeps the edits', async () => {
    examApi.saveExam.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: {
              status: 409,
              code: 'exam-exists',
              detail: 'The class already has an exam in this course on this date.',
            },
          }),
      ),
    );
    await open('493');

    type(percentage('Carter, Olivia'), '75');
    await click('Save exam');

    expect(element().querySelector('[role="alert"]')?.textContent).toContain(
      'The class already has an exam in this course on this date.',
    );
    expect(percentage('Carter, Olivia').value).toBe('75');
  });

  it('creates an exam for the selected class with all its students', async () => {
    examApi.createExam.mockReturnValue(of({ ...exam, id: 500 }));
    await open();

    expect(classApi.getClass).toHaveBeenCalledWith(25);
    expect(element().querySelector('h2')?.textContent).toBe('New exam');
    expect(percentages().map((input) => input.value)).toEqual(['', '', '']);

    // A course is required.
    await click('Save exam');
    expect(examApi.createExam).not.toHaveBeenCalled();
    expect(element().textContent).toContain('Course is required');

    type(element().querySelector('input[type="date"]') as HTMLInputElement, '2026-09-24');
    type(element().querySelector('select') as HTMLSelectElement, '4');
    type(percentage('Baker, Liam'), '88');
    await click('Save exam');

    expect(examApi.createExam).toHaveBeenCalledWith(25, {
      examDate: '2026-09-24',
      courseId: 4,
      results: [
        { studentId: 1, percentage: null },
        { studentId: 2, percentage: 88 },
        { studentId: 3, percentage: null },
      ],
    });
    expect(navigate).toHaveBeenCalledWith(['/exams', 500], expect.anything());
  });
});
