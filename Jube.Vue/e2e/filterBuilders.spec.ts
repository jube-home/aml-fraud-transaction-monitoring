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

const completions = [
  {
    name: 'Payload.Name',
    value: 'Payload.Name',
    meta: 'Name:string',
    score: 1000,
    dataType: 'string',
    group: 'Payload',
    field: `"Json" ->> 'Name'`,
  },
  {
    name: 'Payload.Amount',
    value: 'Payload.Amount',
    meta: 'Amount:double',
    score: 1000,
    dataType: 'double',
    group: 'Payload',
    field: `"Json" ->> 'Amount'`,
  },
  {
    name: 'Payload.Flag',
    value: 'Payload.Flag',
    meta: 'Flag:boolean',
    score: 1000,
    dataType: 'boolean',
    group: 'Payload',
    field: `"Json" ->> 'Flag'`,
  },
]

const statuses = [
  { guid: '11111111-1111-1111-1111-111111111111', name: 'New' },
  { guid: '22222222-2222-2222-2222-222222222222', name: 'Review' },
]

async function stub(page: Page, requested: string[] = [], completionsBody: unknown = completions) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/Completions/**', (route) => {
    requested.push(new URL(route.request().url()).pathname + new URL(route.request().url()).search)

    return Array.isArray(completionsBody) || completionsBody === null
      ? route.fulfill({ json: completionsBody })
      : route.fulfill({ status: 404, body: '' })
  })
  await page.route('**/api/CaseWorkflowStatus/**', (route) => route.fulfill({ json: statuses }))
}

test.describe('CaseFilterBuilder', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('offers case columns, status names and payload fields, and says Incomplete until a rule exists', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=case-filter-builder')

    const where = page.getByTestId('where-builder')
    const select = page.getByTestId('select-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await expect(select.locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')

    const filterOptions = where.locator('.rule-filter-container select option')

    await expect(filterOptions.filter({ hasText: /^Locked$/ })).toHaveCount(1)
    await expect(filterOptions.filter({ hasText: /^Payload\.Name$/ })).toHaveCount(1)

    await where.locator('.rule-filter-container select').selectOption('CaseWorkflowStatusGuid')
    await expect(where.locator('.rule-value-container select option')).toHaveText(['New', 'Review'])
  })

  test('starts calm: nothing is flagged before the user has done anything', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=case-filter-builder')

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')
    await expect(page.getByTestId('bench-value')).toContainText('"valid"')

    await expect(page.locator('.rule-builder-island .has-error')).toHaveCount(0)
    await expect(page.getByTestId('compile-messages')).toHaveCount(0)
  })

  test('builds a sort column and a filter and produces the JSON the case search needs', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=case-filter-builder')

    const where = page.getByTestId('where-builder')
    const select = page.getByTestId('select-builder')

    await expect(select.locator('.rule-container')).toHaveCount(1)
    await expect(select.getByRole('button', { name: 'Add column' })).toBeVisible()
    await expect(select.getByRole('button', { name: 'Add group' })).toBeHidden()

    await select.locator('.rule-filter-container select').selectOption('CaseKey')
    await select.locator('input[type="radio"][value="DESC"]').check()

    await where.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-operator-container select').selectOption('equal')
    await where.locator('.rule-value-container input').fill('Smith')

    const value = page.getByTestId('bench-value')

    await expect(value).toContainText('valid')
    await expect(value).toContainText('"valid": true')
    await expect(value).toContainText('CaseKey')
    await expect(value).toContainText('DESC')
    await expect(value).toContainText('Payload.Name')
    await expect(value).toContainText('Smith')
    await expect(page.getByTestId('compile-status')).toContainText('Valid')
  })

  test('says what is missing and goes to it', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=case-filter-builder')

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await where.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-operator-container select').selectOption('equal')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')

    const problem = page.getByTestId('problem-builder')

    await expect(problem).toContainText('Filter: A value is needed')

    await problem.click()
    await expect(where.locator('.rule-value-container input')).toBeFocused()

    await where.locator('.rule-value-container input').fill('Smith')
    await expect(page.getByTestId('compile-status')).toContainText('Valid')
  })

  test('still works when the status names cannot be loaded', async ({ page }) => {
    await stub(page)
    await page.route('**/api/CaseWorkflowStatus/**', (route) =>
      route.fulfill({ status: 500, body: '' }),
    )
    await page.goto('/tools?tool=case-filter-builder')

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
  })

  test('says so when the columns cannot be loaded', async ({ page }) => {
    await stub(page, [], 'not an array')
    await page.goto('/tools?tool=case-filter-builder')

    await expect(page.getByTestId('filter-builder-load-failed')).toBeVisible()
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=case-filter-builder')
    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="case-filter-builder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})

test.describe('ExhaustiveFilterBuilder', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('produces the SQL, its parameters and the rules as the filter is built, treating a double as a real number', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=exhaustive-filter-builder')

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')
    await expect(page.getByTestId('sql-preview')).toHaveCount(0)

    await where.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await where.locator('.rule-operator-container select').selectOption('greater')
    await where.locator('.rule-value-container input').fill('1.5')

    await expect(page.getByTestId('compile-status')).toContainText('Valid')

    const value = page.getByTestId('bench-value')

    await expect(value).toContainText(`"filterSql": "\\"Json\\" ->> 'Amount' > @1"`)
    await expect(value).toContainText('"filterTokens": "[\\n  1.5\\n]"')
    await expect(value).toContainText('Payload.Amount')
    await expect(value).toContainText('"valid": true')
  })

  test('starts calm: nothing is flagged before the user has done anything', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=exhaustive-filter-builder')

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')
    await expect(page.getByTestId('bench-value')).toContainText('"valid"')

    await expect(page.locator('.rule-builder-island .has-error')).toHaveCount(0)
    await expect(page.getByTestId('compile-messages')).toHaveCount(0)
  })

  test('offers Yes and No for a boolean and only equals', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=exhaustive-filter-builder')

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await where.locator('.rule-filter-container select').selectOption('Payload.Flag')

    await expect(where.locator('.rule-value-container input[type="radio"]')).toHaveCount(2)
    await where.locator('.rule-value-container input[type="radio"][value="True"]').check()
    await expect(page.getByTestId('compile-status')).toContainText('Valid')
  })

  test('reports a text value that is missing', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=exhaustive-filter-builder')

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await where.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-operator-container select').selectOption('contains')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
    await expect(page.getByTestId('problem-builder')).toContainText('A value is needed')
  })

  test('reads the columns of a dataset when that is the source', async ({ page }) => {
    const requested: string[] = []

    await stub(page, requested)
    await page.goto('/tools?tool=exhaustive-filter-builder')
    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)

    await page.getByTestId('bench-source').selectOption('Csv')

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    expect(
      requested.some(
        (url) => url.includes('ByExhaustiveInstanceDataset') && url.includes('kind=Csv'),
      ),
    ).toBe(true)
    expect(requested.some((url) => url.includes('ByEntityAnalysisModelId'))).toBe(true)
  })

  test('says so when there is nothing to filter on', async ({ page }) => {
    await stub(page, [], [])
    await page.goto('/tools?tool=exhaustive-filter-builder')

    await expect(page.getByTestId('filter-builder-no-columns')).toBeVisible()
  })

  test('says so when the columns cannot be loaded', async ({ page }) => {
    await stub(page, [], 'not an array')
    await page.goto('/tools?tool=exhaustive-filter-builder')

    await expect(page.getByTestId('filter-builder-load-failed')).toBeVisible()
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=exhaustive-filter-builder')
    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="exhaustive-filter-builder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
