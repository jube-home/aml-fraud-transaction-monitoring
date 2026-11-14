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
import {expect} from '@playwright/test'
import {test} from './fixtures'
import {stubHierarchy} from './hierarchyStub'

test.describe('Hierarchy', () => {
  test.use({viewport: {width: 1500, height: 1200}})

  test('each kind shows how many children there are and how many are active or locked', async ({
                                                                                                 page,
                                                                                               }) => {
    await page.route(
      (url) => url.pathname.startsWith('/api/'),
      (route) => route.fulfill({status: 404, json: {}}),
    )
    await stubHierarchy(page)
    await page.goto('/models')
    await page
      .locator('.tabulator-row', {hasText: 'Fraud model'})
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('hub-total-CaseWorkflow')).toHaveText('3')
    await expect(page.getByTestId('hub-CaseWorkflow')).toContainText('2 active')
    await expect(page.getByTestId('hub-CaseWorkflow')).toContainText('1 locked')
    await expect(page.getByTestId('hub-total-EntityAnalysisModelTag')).toHaveText('0')
    await expect(page.getByTestId('hub-EntityAnalysisModelTag')).toContainText('None yet')
  })
})
