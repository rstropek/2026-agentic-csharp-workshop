import { expect, Page } from '@playwright/test';

// Every test works on its own class (unique name) or does not save, so tests are independent of
// each other and of the order in which they run.
export function uniqueClassName() {
  return `E2E ${Date.now().toString(36)}${Math.floor(Math.random() * 1000)}`;
}

export const lastNames = (page: Page) => page.getByRole('textbox', { name: 'Last name' });
export const firstNames = (page: Page) => page.getByRole('textbox', { name: 'First name' });

export async function addStudent(page: Page, lastName: string, firstName: string) {
  await page.getByRole('button', { name: 'Add student' }).click();
  // The new row is inserted on top and its last name gets the focus.
  await expect(lastNames(page).first()).toBeFocused();
  await page.keyboard.type(lastName);
  await page.keyboard.press('Tab');
  await page.keyboard.type(firstName);
}

export async function saveClass(page: Page) {
  await page.getByRole('button', { name: 'Save class' }).click();
  await expect(page.getByRole('status')).toHaveText('Saved');
}

/** Creates a class in the selected (newest) school year with the given students. */
export async function createClass(page: Page, className: string, students: [string, string][]) {
  await page.goto('/classes');
  await page.getByRole('button', { name: 'New class' }).click();
  await page.getByLabel('Class name').fill(className);
  for (const [lastName, firstName] of students) {
    await addStudent(page, lastName, firstName);
  }
  await saveClass(page);
  await expect(page).toHaveURL(/\/classes\/\d+$/);
}
