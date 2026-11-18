/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

import process from 'node:process'
import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './docs-shots',
  outputDir: 'test-results/docs-shots',
  timeout: 60 * 1000,
  fullyParallel: true,
  workers: 4,
  reporter: 'list',
  globalSetup: './docs-shots/signIn.ts',
  globalTeardown: './docs-shots/sync.ts',
  use: {
    baseURL: process.env.JUBE_DOCS_BASE_URL ?? 'http://127.0.0.1:5001',
    storageState: 'test-results/docs-shots/state.json',
    viewport: { width: 1440, height: 900 },
    actionTimeout: 10 * 1000,
    deviceScaleFactor: 2,
    colorScheme: 'light',
    locale: 'en-GB',
    timezoneId: 'UTC',
    headless: true,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
})
