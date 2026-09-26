import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CalculatorService } from '../api';
import { Calculator } from './calculator';

describe('Calculator', () => {
  let fixture: ComponentFixture<Calculator>;
  let add: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    add = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: CalculatorService, useValue: { add } }],
    });
    fixture = TestBed.createComponent(Calculator);
    await fixture.whenStable();
  });

  async function calculate(a: string, b: string) {
    const element = fixture.nativeElement as HTMLElement;
    const [inputA, inputB] = Array.from(element.querySelectorAll('input'));
    inputA.value = a;
    inputA.dispatchEvent(new Event('input'));
    inputB.value = b;
    inputB.dispatchEvent(new Event('input'));
    element.querySelector('button')!.click();
    await fixture.whenStable();
  }

  const query = (testId: string) =>
    (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);

  it('shows the result returned by the API', async () => {
    add.mockReturnValue(of({ result: 3 }));

    await calculate('1', '2');

    expect(add).toHaveBeenCalledWith(1, 2);
    expect(query('result')?.textContent).toBe('3');
    expect(query('error')).toBeNull();
  });

  it('shows the problem details returned by the API', async () => {
    add.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { status: 400, detail: "Parameter 'a' must not be negative, but was -1." },
          }),
      ),
    );

    await calculate('-1', '2');

    expect(add).toHaveBeenCalledWith(-1, 2);
    expect(query('error')?.textContent).toContain("Parameter 'a' must not be negative");
    expect(query('result')).toBeNull();
  });
});
