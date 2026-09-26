import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ClassManagementService } from './api';
import { App } from './app';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: ClassManagementService,
          useValue: {
            getSchoolYears: vi.fn(() =>
              of([
                { id: 2, name: '2026/27' },
                { id: 1, name: '2025/26' },
              ]),
            ),
          },
        },
      ],
    });
  });

  it('shows the areas and selects the newest school year', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    const areas = Array.from(element.querySelectorAll('nav a')).map((a) => a.textContent?.trim());
    expect(areas).toEqual(['Classes', 'Exams', 'Assistant', 'Settings']);
    const select = element.querySelector('select') as HTMLSelectElement;
    expect(select.selectedOptions[0].textContent?.trim()).toBe('2026/27');
  });
});
