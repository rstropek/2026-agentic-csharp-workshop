import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { PercentageField, percentage } from './percentage-field';

@Component({
  imports: [PercentageField],
  template: `<app-percentage-field [field]="resultForm.percentage" label="Percentage of Anderson, Maya" />`,
})
class Host {
  readonly model = signal<{ percentage: number | null }>({ percentage: 82 });
  readonly resultForm = form(this.model, (path) => percentage(path.percentage));
}

describe('PercentageField', () => {
  let fixture: ComponentFixture<Host>;

  beforeEach(async () => {
    fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
  });

  const element = () => fixture.nativeElement as HTMLElement;
  const input = () => element().querySelector('input') as HTMLInputElement;
  const model = () => fixture.componentInstance.model().percentage;

  async function type(value: string) {
    input().value = value;
    input().dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function blur() {
    input().dispatchEvent(new Event('blur'));
    await fixture.whenStable();
  }

  it('is a labeled number field with the unit after it', () => {
    expect(input().type).toBe('number');
    expect(input().getAttribute('aria-label')).toBe('Percentage of Anderson, Maya');
    expect(input().placeholder).toBe('No result');
    expect(input().value).toBe('82');
    expect(element().textContent).toContain('%');
  });

  it('accepts decimals', async () => {
    await type('67.5');

    expect(model()).toBe(67.5);
  });

  it('means no result when empty', async () => {
    await type('');
    await blur();

    expect(model()).toBeNull();
    expect(input().getAttribute('aria-invalid')).toBeNull();
    expect(element().querySelector('.field-error')).toBeNull();
  });

  it.each(['0', '100'])('accepts the limit %s', async (value) => {
    await type(value);
    await blur();

    expect(model()).toBe(Number(value));
    expect(fixture.componentInstance.resultForm.percentage().valid()).toBe(true);
  });

  it.each(['104', '-1', '100.5'])('rejects %s once touched', async (value) => {
    await type(value);

    // No error while typing.
    expect(element().querySelector('.field-error')).toBeNull();

    await blur();

    expect(fixture.componentInstance.resultForm.percentage().valid()).toBe(false);
    expect(input().getAttribute('aria-invalid')).toBe('true');
    const error = element().querySelector('.field-error');
    expect(error?.textContent?.trim()).toBe('Must be 0–100');
    expect(input().getAttribute('aria-describedby')).toBe(error?.parentElement?.id);
  });
});
