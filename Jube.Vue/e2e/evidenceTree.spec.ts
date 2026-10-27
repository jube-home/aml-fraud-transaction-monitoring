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

test.describe('EvidenceTree', () => {
  test.use({ viewport: { width: 1600, height: 1000 } })

  test('shows every level expanded, with readable values', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=evidence-tree')

    const tree = page.getByTestId('evidence-tree')

    await expect(tree.getByText('Fraud score')).toBeVisible()
    await expect(tree.getByText('Yes', { exact: true })).toBeVisible()
    await expect(tree.getByText('0.9123')).toBeVisible()
    await expect(tree.getByText('[0]').first()).toBeVisible()
    await expect(tree.getByText('Amount')).toBeVisible()
    await expect(page.getByTestId('evidence-count')).toHaveText('14 values')
  })

  test('tells null, empty text, empty array and empty object apart', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=evidence-tree')

    const tree = page.getByTestId('evidence-tree')

    await expect(tree.locator('.tabulator-row', { hasText: 'reviewer' })).toContainText('No value')
    await expect(tree.locator('.tabulator-row', { hasText: 'notes' })).toContainText('(empty)')
    await expect(tree.locator('.tabulator-row', { hasText: 'flags' })).toContainText('[]')
    await expect(tree.locator('.tabulator-row', { hasText: 'extra' })).toContainText('{}')
  })

  test('collapses and expands every branch', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=evidence-tree')

    const tree = page.getByTestId('evidence-tree')

    await page.getByTestId('evidence-collapse').click()

    await expect(tree.getByText('Fraud score')).toHaveCount(0)
    await expect(tree.getByText('metrics')).toBeVisible()

    await page.getByTestId('evidence-expand').click()

    await expect(tree.getByText('Fraud score')).toBeVisible()
    await expect(tree.getByText('Amount')).toBeVisible()
  })

  test('says so when there is nothing to show or the JSON is invalid', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=evidence-tree')

    await page.getByTestId('bench-json').fill('{}')
    await expect(page.getByTestId('evidence-empty')).toBeVisible()
    await expect(page.getByTestId('evidence-expand')).toHaveCount(0)

    await page.getByTestId('bench-json').fill('null')
    await expect(page.getByTestId('evidence-empty')).toBeVisible()

    await page.getByTestId('bench-json').fill('{nope')
    await expect(page.getByTestId('bench-json-error')).toBeVisible()
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=evidence-tree')
    await expect(page.getByText('Fraud score')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
