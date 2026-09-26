import { expect, Page, test } from '@playwright/test';
import { createClass, uniqueClassName } from './helpers';

// Selects are found by role: the text of a label that wraps a select includes the option texts.
const classFilter = (page: Page) => page.getByRole('combobox', { name: 'Class', exact: true });
const percentage = (page: Page, student: string) =>
  page.getByRole('spinbutton', { name: `Percentage of ${student}` });

test('enters the results of an exam', async ({ page }) => {
  // Own class, so the exam and its results do not depend on other tests.
  const className = uniqueClassName();
  await createClass(page, className, [
    ['Lovelace', 'Ada'],
    ['Babbage', 'Charles'],
    ['Turing', 'Alan'],
  ]);

  // New exam for the class: every student is listed without a result.
  await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Exams' }).click();
  await classFilter(page).selectOption({ label: className });
  await expect(page.getByText('0 exams · by date')).toBeVisible();
  await page.getByRole('button', { name: 'New exam' }).click();
  await expect(page.getByText('0 of 3 entered')).toBeVisible();
  await page.getByLabel('Date').fill('2026-11-05');
  await page.getByRole('combobox', { name: 'Course' }).selectOption({ label: 'History' });

  // Fast entry: Tab goes from student to student; decimals are allowed, empty means no result.
  await percentage(page, 'Babbage, Charles').fill('67.5');
  await page.keyboard.press('Tab');
  await expect(percentage(page, 'Lovelace, Ada')).toBeFocused();
  await page.keyboard.type('104');
  await expect(page.getByText('2 of 3 entered')).toBeVisible();

  // A value out of range blocks saving.
  await page.getByRole('button', { name: 'Save exam' }).click();
  await expect(page.getByText('1 field needs attention')).toBeVisible();
  await expect(page.getByText('Must be 0–100')).toBeVisible();
  await expect(percentage(page, 'Lovelace, Ada')).toBeFocused();
  await expect(page).toHaveURL(/\/exams\/new$/);

  await percentage(page, 'Lovelace, Ada').fill('91');
  await page.getByRole('button', { name: 'Save exam' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Saved' })).toBeVisible();
  await expect(page).toHaveURL(/\/exams\/\d+$/);
  await expect(page.getByRole('heading', { name: 'History' })).toBeVisible();
  const examList = page.getByRole('list', { name: `Exams of class ${className}` });
  await expect(examList.getByRole('link', { name: '05 Nov 2026 History' })).toHaveAttribute(
    'aria-current',
    'page',
  );

  // Change a result and remove one.
  await percentage(page, 'Babbage, Charles').fill('70');
  await percentage(page, 'Turing, Alan').fill('88.5');
  await percentage(page, 'Lovelace, Ada').fill('');
  await page.getByRole('button', { name: 'Save exam' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Saved' })).toBeVisible();

  // The results are stored; the reloaded exam selects its class in the filter.
  await page.reload();
  await expect(page.getByText('2 of 3 entered')).toBeVisible();
  await expect(percentage(page, 'Babbage, Charles')).toHaveValue('70');
  await expect(percentage(page, 'Lovelace, Ada')).toHaveValue('');
  await expect(percentage(page, 'Turing, Alan')).toHaveValue('88.5');
  await expect(classFilter(page).locator('option:checked')).toHaveText(className);
});
