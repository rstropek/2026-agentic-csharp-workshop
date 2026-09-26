import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmService } from './confirm';
import { ConfirmDialog } from './confirm-dialog';

describe('ConfirmDialog', () => {
  let fixture: ComponentFixture<ConfirmDialog>;
  let confirmService: ConfirmService;

  beforeEach(async () => {
    fixture = TestBed.createComponent(ConfirmDialog);
    confirmService = TestBed.inject(ConfirmService);
    await fixture.whenStable();
  });

  const dialog = () =>
    (fixture.nativeElement as HTMLElement).querySelector('dialog') as HTMLDialogElement;
  const button = (label: string) =>
    Array.from(dialog().querySelectorAll('button')).find((b) => b.textContent?.trim() === label);

  /** Opens the dialog; returns the pending answer (wrapped, so awaiting `open()` does not wait for it). */
  async function open() {
    const answer = confirmService.confirm({
      title: 'Delete class 2B?',
      message: 'This cannot be undone.',
      confirmLabel: 'Delete class',
    });
    await fixture.whenStable();
    return { answer };
  }

  it('is closed until a confirmation is requested', () => {
    expect(dialog().open).toBe(false);
  });

  it('shows the request and focuses Cancel', async () => {
    await open();

    expect(dialog().open).toBe(true);
    expect(dialog().textContent).toContain('Delete class 2B?');
    expect(dialog().textContent).toContain('This cannot be undone.');
    expect(document.activeElement).toBe(button('Cancel'));
  });

  it('resolves to true and closes when the user confirms', async () => {
    const { answer } = await open();

    button('Delete class')?.click();
    await fixture.whenStable();

    await expect(answer).resolves.toBe(true);
    expect(dialog().open).toBe(false);
  });

  it('resolves to false when the user cancels', async () => {
    const { answer } = await open();

    button('Cancel')?.click();
    await fixture.whenStable();

    await expect(answer).resolves.toBe(false);
    expect(dialog().open).toBe(false);
  });

  it('resolves to false when the browser closes the dialog (Escape)', async () => {
    const { answer } = await open();

    dialog().close();
    await fixture.whenStable();

    await expect(answer).resolves.toBe(false);
  });
});
