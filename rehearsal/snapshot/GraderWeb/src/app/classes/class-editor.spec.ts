import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ClassDetails, ClassManagementService } from '../api';
import { ClassEditor } from './class-editor';

const class2B: ClassDetails = {
  id: 7,
  name: '2B',
  schoolYearId: 3,
  schoolYearName: '2026/27',
  students: [
    { id: 1, lastName: 'Anderson', firstName: 'Maya' },
    { id: 2, lastName: 'Baker', firstName: 'Liam' },
  ],
};

describe('ClassEditor', () => {
  let fixture: ComponentFixture<ClassEditor>;
  let api: {
    getSchoolYears: ReturnType<typeof vi.fn>;
    getClass: ReturnType<typeof vi.fn>;
    saveClass: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    api = {
      getSchoolYears: vi.fn(() => of([{ id: 3, name: '2026/27' }])),
      getClass: vi.fn(() => of(class2B)),
      saveClass: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ClassManagementService, useValue: api }],
    });
    fixture = TestBed.createComponent(ClassEditor);
    fixture.componentRef.setInput('classId', '7');
    await fixture.whenStable();
  });

  const element = () => fixture.nativeElement as HTMLElement;
  const inputs = (label: string) =>
    Array.from(element().querySelectorAll<HTMLInputElement>(`input[aria-label="${label}"]`));
  /** Finds a button by its accessible name (aria-label or text). */
  const button = (name: string) =>
    Array.from(element().querySelectorAll('button')).find(
      (b) => (b.getAttribute('aria-label') ?? b.textContent?.trim()) === name,
    ) as HTMLButtonElement;

  function type(input: HTMLInputElement, value: string) {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  /** Clicks a button and waits until async work (e.g. the submission) is done and rendered. */
  async function click(label: string) {
    button(label).click();
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
  }

  it('shows the class and its students', () => {
    expect(api.getClass).toHaveBeenCalledWith(7);
    expect(inputs('Last name').map((input) => input.value)).toEqual(['Anderson', 'Baker']);
    expect(element().querySelector('h2')?.textContent).toContain('2');
  });

  it('saves the class name and the complete student list, then shows the response', async () => {
    const saved: ClassDetails = {
      ...class2B,
      students: [
        { id: 1, lastName: 'Anderson', firstName: 'Maya' },
        { id: 2, lastName: 'Baker', firstName: 'Liam' },
        { id: 3, lastName: 'Carter', firstName: 'Olivia' },
      ],
    };
    api.saveClass.mockReturnValue(of(saved));

    await click('Add student');
    const newRow = inputs('Last name')[0];
    expect(document.activeElement).toBe(newRow);
    type(newRow, ' Carter ');
    type(inputs('First name')[0], 'Olivia');
    await click('Save class');

    expect(api.saveClass).toHaveBeenCalledWith(7, {
      name: '2B',
      students: [
        { id: null, lastName: 'Carter', firstName: 'Olivia' },
        { id: 1, lastName: 'Anderson', firstName: 'Maya' },
        { id: 2, lastName: 'Baker', firstName: 'Liam' },
      ],
    });
    expect(inputs('Last name').map((input) => input.value)).toEqual([
      'Anderson',
      'Baker',
      'Carter',
    ]);
    expect(element().querySelector('[role="status"]')?.textContent).toContain('Saved');
  });

  it('sends removed students by leaving them out', async () => {
    api.saveClass.mockReturnValue(of({ ...class2B, students: [class2B.students[1]] }));

    await click('Remove Anderson, Maya');
    await click('Save class');

    expect(api.saveClass).toHaveBeenCalledWith(7, {
      name: '2B',
      students: [{ id: 2, lastName: 'Baker', firstName: 'Liam' }],
    });
  });

  it('does not save invalid fields, shows the errors and focuses the first invalid field', async () => {
    type(inputs('First name')[1], '');
    type(inputs('Last name')[1], '   ');
    await click('Save class');

    expect(api.saveClass).not.toHaveBeenCalled();
    expect(element().textContent).toContain('2 fields need attention');
    expect(element().textContent).toContain('Last name is required');
    expect(element().textContent).toContain('First name is required');
    expect(inputs('Last name')[1].getAttribute('aria-invalid')).toBe('true');
    expect(document.activeElement).toBe(inputs('Last name')[1]);
  });

  it('shows the error message of the server', async () => {
    api.saveClass.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: {
              status: 409,
              code: 'student-has-results',
              detail: 'The student Anderson, Maya has exam results and cannot be removed.',
            },
          }),
      ),
    );

    await click('Remove Anderson, Maya');
    await click('Save class');

    expect(element().querySelector('[role="alert"]')?.textContent).toContain(
      'The student Anderson, Maya has exam results and cannot be removed.',
    );
    // The local edits are kept, so the user can fix them.
    expect(inputs('Last name').map((input) => input.value)).toEqual(['Baker']);
  });
});
