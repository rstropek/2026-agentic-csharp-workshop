import { defineConfig, devices } from '@playwright/test';

const port = 5200;
const baseURL = `http://localhost:${port}`;

// E2E tests run against the production setup: the Angular app is built into GraderApi/wwwroot,
// the backend is published, and the published app serves both the Angular app and the API.
// It must be the *published* app: when running the build output in Production, MapStaticAssets
// answers compressed requests with empty responses (see ../tech-docs/testing.md).
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: process.env['CI'] ? 'github' : 'list',
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command:
      'npm run build && dotnet publish ../GraderApi -c Release -o ../GraderApi/bin/e2e-publish && cd ../GraderApi/bin/e2e-publish && dotnet GraderApi.dll',
    url: baseURL,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: baseURL,
    },
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
