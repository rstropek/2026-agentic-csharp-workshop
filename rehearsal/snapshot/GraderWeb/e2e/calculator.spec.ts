import { expect, Page, test } from '@playwright/test';

async function calculate(page: Page, a: string, b: string) {
  await page.goto('/');
  await page.getByLabel('First number').fill(a);
  await page.getByLabel('Second number').fill(b);
  await page.getByRole('button', { name: 'Calculate' }).click();
}

test('adds two numbers', async ({ page }) => {
  await calculate(page, '1', '2');

  await expect(page.getByTestId('result')).toHaveText('3');
  await expect(page.getByRole('alert')).toHaveCount(0);
});

test('shows the API error for negative numbers', async ({ page }) => {
  await calculate(page, '-1', '2');

  await expect(page.getByRole('alert')).toContainText("Parameter 'a' must not be negative");
  await expect(page.getByTestId('result')).toHaveCount(0);
});
