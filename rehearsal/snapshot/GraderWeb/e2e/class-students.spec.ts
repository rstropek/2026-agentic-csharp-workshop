import { expect, Page, test } from '@playwright/test';
import { addStudent, firstNames, lastNames, saveClass, uniqueClassName } from './helpers';

async function expectStudents(page: Page, students: [string, string][]) {
  await expect(lastNames(page)).toHaveCount(students.length);
  for (const [index, [lastName, firstName]] of students.entries()) {
    await expect(lastNames(page).nth(index)).toHaveValue(lastName);
    await expect(firstNames(page).nth(index)).toHaveValue(firstName);
  }
}

test('adds and edits the students of a class', async ({ page }) => {
  const className = uniqueClassName();

  // Create a class with two students.
  await page.goto('/classes');
  await page.getByRole('button', { name: 'New class' }).click();
  await page.getByLabel('Class name').fill(className);
  await addStudent(page, 'Lovelace', 'Ada');
  await addStudent(page, 'Babbage', 'Charles');
  await saveClass(page);

  // The saved class comes back sorted by last name.
  await expect(page).toHaveURL(/\/classes\/\d+$/);
  await expectStudents(page, [
    ['Babbage', 'Charles'],
    ['Lovelace', 'Ada'],
  ]);

  // Edit a student, remove one and add another one.
  await lastNames(page).nth(1).fill('King');
  await page.getByRole('button', { name: 'Remove Babbage, Charles' }).click();
  await addStudent(page, 'Turing', 'Alan');
  await saveClass(page);
  await expectStudents(page, [
    ['King', 'Ada'],
    ['Turing', 'Alan'],
  ]);

  // The changes are stored.
  await page.reload();
  await expectStudents(page, [
    ['King', 'Ada'],
    ['Turing', 'Alan'],
  ]);
  await page.getByRole('navigation', { name: 'Breadcrumb' }).getByRole('link').click();
  await expect(page.getByRole('link', { name: className })).toContainText('2 students');
});

test('does not save a class with missing names', async ({ page }) => {
  await page.goto('/classes');
  await page.getByRole('list', { name: 'Classes' }).getByRole('link').first().click();
  await expect(lastNames(page).first()).not.toHaveValue('');
  const studentCount = await lastNames(page).count();

  await page.getByRole('button', { name: 'Add student' }).click();
  await page.getByRole('button', { name: 'Save class' }).click();

  await expect(page.getByText('2 fields need attention')).toBeVisible();
  await expect(page.getByText('Last name is required')).toBeVisible();
  await expect(page.getByText('First name is required')).toBeVisible();
  await expect(lastNames(page).first()).toBeFocused();

  // Nothing was saved.
  await page.reload();
  await expect(lastNames(page)).toHaveCount(studentCount);
});
