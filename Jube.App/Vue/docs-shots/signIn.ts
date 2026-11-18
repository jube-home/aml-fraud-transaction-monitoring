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

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname } from 'node:path'
import { chromium, type FullConfig } from '@playwright/test'

export const statePath = 'test-results/docs-shots/state.json'

export default async function signIn(config: FullConfig): Promise<void> {
  const user = process.env.JUBE_DOCS_USER
  const password = process.env.JUBE_DOCS_PASSWORD
  mkdirSync(dirname(statePath), { recursive: true })

  if (!user || !password) {
    writeFileSync(statePath, JSON.stringify({ cookies: [], origins: [] }), 'utf8')

    return
  }

  const browser = await chromium.launch()
  const context = await browser.newContext({ baseURL: config.projects[0]!.use.baseURL })
  const page = await context.newPage()

  await page.goto('/')
  await page.getByLabel('User name').fill(user)
  await page.getByLabel('Password', { exact: true }).fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await page.getByRole('navigation', { name: 'Main navigation' }).waitFor()
  await context.storageState({ path: statePath })
  await browser.close()
}
