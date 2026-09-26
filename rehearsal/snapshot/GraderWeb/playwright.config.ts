import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { defineConfig, devices } from '@playwright/test';

// Own port, so E2E tests never talk to a server started for something else (e.g. on 5200).
const port = 5210;
const baseURL = `http://localhost:${port}`;

// Temporary database, deleted before the server starts and filled with demo data by
// e2e/demo-data.setup.ts. E2E tests never touch data/grader.db.
const databaseDirectory = join(tmpdir(), 'grader-e2e');
const databaseFile = join(databaseDirectory, 'grader.db');

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
  projects: [
    { name: 'setup', testMatch: /\.setup\.ts$/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1920, height: 1080 } },
      dependencies: ['setup'],
    },
  ],
  webServer: {
    command: [
      `node -e "require('node:fs').rmSync(process.argv[1], { recursive: true, force: true }); require('node:fs').mkdirSync(process.argv[1])" "${databaseDirectory}"`,
      'npm run build',
      'dotnet publish ../GraderApi -c Release -o ../GraderApi/bin/e2e-publish',
      'cd ../GraderApi/bin/e2e-publish',
      'dotnet GraderApi.dll',
    ].join(' && '),
    url: baseURL,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: baseURL,
      ConnectionStrings__Grader: `Data Source=${databaseFile}`,
    },
    // Never reuse a running server: it might use another database (the tests reset all data).
    reuseExistingServer: false,
    timeout: 180_000,
  },
});
