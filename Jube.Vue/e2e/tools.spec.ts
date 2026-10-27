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

import { expect, test } from '@playwright/test'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

test.describe('TEMP: Tools', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test.beforeEach(async ({ page }) => {
    await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
    await page.route('**/api/RuleVocabulary', (route) => route.fulfill({ json: [] }))
    await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({ json: [] }))
    await page.route('**/api/Completions/ByEntityAnalysisModelIdParseTypeId**', (route) =>
      route.fulfill({ json: [] }),
    )
    await page.route('**/api/VisualisationRegistry/Summary', (route) => route.fulfill({ json: [] }))
    await page.route('**/api/VisualisationRegistry/GetByShowInDirectory', (route) =>
      route.fulfill({ json: [] }),
    )
    await page.route('**/api/Parser', (route) => route.fulfill({ json: { message: 'Compiled' } }))
    await page.route('**/api/ServiceChange/Stream', (route) =>
      route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
    )
  })

  test('is reachable from the drawer for any signed-in user', async ({ page }) => {
    await page.goto('/tools')
    await page
      .getByRole('navigation', { name: 'Main navigation' })
      .getByRole('link', { name: 'TEMP: Tools' })
      .click()

    await expect(page).toHaveURL(/\/tools$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Tools workbench' })).toBeVisible()
  })

  test('opens on BuilderCoder and keeps the chosen tool in the address', async ({ page }) => {
    await page.goto('/tools')

    await expect(page.getByTestId('tool-title')).toHaveText('BuilderCoder')

    await page.getByTestId('tool-visualisation-dashboard').click()
    await expect(page).toHaveURL(/tool=visualisation-dashboard/)
    await expect(page.getByTestId('tool-title')).toHaveText('VisualisationDashboard')
    await expect(page.getByTestId('bench-pick')).toBeVisible()
  })

  test('restores the chosen tool from a deep link', async ({ page }) => {
    await page.goto('/tools?tool=evidence-tree')

    await expect(page.getByTestId('tool-title')).toHaveText('EvidenceTree')
  })
})
