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
import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { settleAnimations } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

async function stub(page: Page) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
}

test.describe('GridStatistics', () => {
  test.use({ viewport: { width: 1600, height: 1000 } })

  test('shows a histogram and figures per column, and says when a column has no values', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=grid-statistics-table')

    const rows = page.locator('.tabulator-row')

    await expect(rows).toHaveCount(2)
    await expect(rows.nth(0)).toContainText('droppedCount')
    await expect(rows.nth(0)).toContainText('No values')
    await expect(rows.nth(0)).not.toContainText('0.00')
    await expect(rows.nth(0)).toContainText('–')
    await expect(rows.nth(1)).toContainText('Queue depth')
    await expect(rows.nth(1)).toContainText('412.00')
    await expect(rows.nth(1)).toContainText('96.40')
    await expect(rows.nth(1).locator('svg rect')).toHaveCount(10)
  })

  test('describes each histogram to assistive technology and gives each bar a title', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=grid-statistics-table')

    const svg = page.locator('.tabulator-row').nth(1).locator('svg')

    await expect(svg).toHaveAttribute('role', 'img')
    await expect(svg).toHaveAttribute('aria-label', /10 bins from 0\.00 to 412\.00/)
    await expect(svg.locator('rect title').first()).toHaveText('0.00: 120')
  })

  test('shows the display name, and the column name when there is none', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=grid-statistics-table')

    const rows = page.locator('.tabulator-row')

    await expect(rows.nth(1)).toContainText('Queue depth')
    await expect(rows.nth(1)).not.toContainText('queueDepth')
    await expect(rows.nth(0)).toContainText('droppedCount')

    await page.getByTestId('bench-labels').fill('{"droppedCount":"Dropped messages"}')

    await expect(
      page.locator('.tabulator-row').filter({ hasText: 'Dropped messages' }),
    ).toHaveCount(1)
    await expect(page.locator('.tabulator-row').filter({ hasText: 'queueDepth' })).toHaveCount(1)

    await page.getByTestId('bench-labels').fill('')

    await expect(page.getByTestId('bench-json-error')).toBeVisible()
  })

  test('sorts by a figure from the column header', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=grid-statistics-table')

    await page.locator('.tabulator-col', { hasText: 'Mean' }).click()
    await page.locator('.tabulator-col', { hasText: 'Mean' }).click()

    await expect(page.locator('.tabulator-row').first()).toContainText('Queue depth')
  })

  test('says so when there are no statistics or the JSON is invalid', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=grid-statistics-table')

    await page.getByTestId('bench-json').fill('{"columns":{}}')
    await expect(page.getByTestId('statistics-empty')).toBeVisible()

    await page.getByTestId('bench-json').fill('null')
    await expect(page.getByTestId('statistics-empty')).toBeVisible()

    await page.getByTestId('bench-json').fill('{nope')
    await expect(page.getByTestId('bench-json-error')).toBeVisible()
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=grid-statistics-table')
    await expect(page.locator('.tabulator-row')).toHaveCount(2)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
