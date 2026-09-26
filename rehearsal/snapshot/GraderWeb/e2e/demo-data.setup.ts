import { expect, test as setup } from '@playwright/test';

// Runs once before all tests (project "setup" in playwright.config.ts): fills the temporary E2E
// database with demo data via the Settings page, which also covers the reset and its confirmation.
setup('fill the database with demo data', async ({ page }) => {
  await page.goto('/settings');
  await page.getByRole('button', { name: 'Reset demo data' }).click();

  const dialog = page.getByRole('dialog', { name: 'Reset demo data?' });
  await dialog.getByRole('button', { name: 'Reset demo data' }).click();

  await expect(page.getByRole('status')).toHaveText('The demo data has been reset.', {
    timeout: 30_000,
  });
});
