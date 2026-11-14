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
import type {Page} from '@playwright/test'
import {test as base} from '@playwright/test'
import {wireSchema} from './svirfdStub'

async function stubWireSchema(page: Page) {
  await page.route('**/api/WireSchema**', (route) => route.fulfill({json: wireSchema}))
}

export const test = base.extend({
  page: async ({page}, use) => {
    const route = page.route.bind(page) as Page['route']

    await stubWireSchema(page)

    page.route = (async (url, handler, options) => {
      await route(url, handler, options)

      if (typeof url !== 'string') await stubWireSchema(page)
    }) as Page['route']

    await use(page)
  },
})
