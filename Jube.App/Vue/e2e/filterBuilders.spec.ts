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
import {expect, type Page} from '@playwright/test'
import {test} from './fixtures'
import {openEditorHost, stubEditorHost, type Sent} from './editorHost'
import {settleAnimations} from './support'

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
  {guid: '11111111-1111-1111-1111-111111111111', name: 'New'},
  {guid: '22222222-2222-2222-2222-222222222222', name: 'Review'},
]

async function stub(
  page: Page,
  editor: 'case' | 'exhaustive',
  requested: string[] = [],
  completionsBody: unknown = completions,
) {
  const sent = await stubEditorHost(page, {editor})

  await page.route('**/api/Completions/**', (route) => {
    requested.push(new URL(route.request().url()).pathname + new URL(route.request().url()).search)

    return Array.isArray(completionsBody) || completionsBody === null
      ? route.fulfill({json: completionsBody})
      : route.fulfill({status: 404, body: ''})
  })
  await page.route('**/api/CaseWorkflowStatus/**', (route) => route.fulfill({json: statuses}))

  return sent
}

async function open(page: Page) {
  await openEditorHost(page)
}

async function save(page: Page, sent: Sent[]) {
  await page.getByTestId('crud-save').click()
  await expect.poll(() => sent.length).toBe(1)

  return sent[0]!.body
}

test.describe('CaseFilterBuilder', () => {
  test.use({viewport: {width: 1440, height: 1000}})

  test('offers case columns, status names and payload fields, and says Incomplete until a rule exists', async ({
                                                                                                                 page,
                                                                                                               }) => {
    await stub(page, 'case')
    await open(page)

    const where = page.getByTestId('where-builder')
    const select = page.getByTestId('select-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await expect(select.locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')

    const filterOptions = where.locator('.rule-filter-container select option')

    await expect(filterOptions.filter({hasText: /^Locked$/})).toHaveCount(1)
    await expect(filterOptions.filter({hasText: /^Payload\.Name$/})).toHaveCount(1)

    await where.locator('.rule-filter-container select').selectOption('CaseWorkflowStatusGuid')
    await expect(where.locator('.rule-value-container select option')).toHaveText(['New', 'Review'])
  })

  test('starts calm: nothing is flagged before the user has done anything', async ({page}) => {
    await stub(page, 'case')
    await open(page)

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')

    await expect(page.locator('.rule-builder-island .has-error')).toHaveCount(0)
    await expect(page.getByTestId('compile-messages')).toHaveCount(0)
  })

  test('builds a sort column and a filter and produces the JSON the case search needs', async ({
                                                                                                 page,
                                                                                               }) => {
    const sent = await stub(page, 'case')
    await open(page)

    const where = page.getByTestId('where-builder')
    const select = page.getByTestId('select-builder')

    await expect(select.locator('.rule-container')).toHaveCount(1)
    await expect(select.getByRole('button', {name: 'Add column'})).toBeVisible()
    await expect(select.getByRole('button', {name: 'Add group'})).toBeHidden()

    await select.locator('.rule-filter-container select').selectOption('CaseKey')
    await select.locator('input[type="radio"][value="DESC"]').check()

    await where.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-operator-container select').selectOption('equal')
    await where.locator('.rule-value-container input').fill('Smith')

    await expect(page.getByTestId('compile-status')).toContainText('Valid')

    const body = await save(page, sent)

    expect(String(body.selectJson)).toContain('CaseKey')
    expect(String(body.selectJson)).toContain('DESC')
    expect(String(body.filterJson)).toContain('Payload.Name')
    expect(String(body.filterJson)).toContain('Smith')
  })

  test('says what is missing and goes to it', async ({page}) => {
    await stub(page, 'case')
    await open(page)

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

  test('still works when the status names cannot be loaded', async ({page}) => {
    await stub(page, 'case')
    await page.route('**/api/CaseWorkflowStatus/**', (route) =>
      route.fulfill({status: 500, body: ''}),
    )
    await open(page)

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
  })

  test('says so when the columns cannot be loaded', async ({page}) => {
    await stub(page, 'case', [], 'not an array')
    await open(page)

    await expect(page.getByTestId('filter-builder-load-failed')).toBeVisible()
  })

  test('has no accessibility violations', async ({page}) => {
    await stub(page, 'case')
    await open(page)
    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({page})
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
  test.use({viewport: {width: 1440, height: 1000}})

  test('produces the SQL, its parameters and the rules as the filter is built, treating a double as a real number', async ({
                                                                                                                             page,
                                                                                                                           }) => {
    const sent = await stub(page, 'exhaustive')
    await open(page)

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')
    await expect(page.getByTestId('sql-preview')).toHaveCount(0)

    await where.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await where.locator('.rule-operator-container select').selectOption('greater')
    await where.locator('.rule-value-container input').fill('1.5')

    await expect(page.getByTestId('compile-status')).toContainText('Valid')

    const body = await save(page, sent)

    expect(body.filterSql).toBe(`"Json" ->> 'Amount' > @1`)
    expect(body.filterTokens).toBe('[\n  1.5\n]')
    expect(String(body.filterJson)).toContain('Payload.Amount')
  })

  test('starts calm: nothing is flagged before the user has done anything', async ({page}) => {
    await stub(page, 'exhaustive')
    await open(page)

    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')

    await expect(page.locator('.rule-builder-island .has-error')).toHaveCount(0)
    await expect(page.getByTestId('compile-messages')).toHaveCount(0)
  })

  test('offers Yes and No for a boolean and only equals', async ({page}) => {
    await stub(page, 'exhaustive')
    await open(page)

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await where.locator('.rule-filter-container select').selectOption('Payload.Flag')

    await expect(where.locator('.rule-value-container input[type="radio"]')).toHaveCount(2)
    await where.locator('.rule-value-container input[type="radio"][value="True"]').check()
    await expect(page.getByTestId('compile-status')).toContainText('Valid')
  })

  test('reports a text value that is missing', async ({page}) => {
    await stub(page, 'exhaustive')
    await open(page)

    const where = page.getByTestId('where-builder')

    await expect(where.locator('.rule-container')).toHaveCount(1)
    await where.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-operator-container select').selectOption('contains')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
    await expect(page.getByTestId('problem-builder')).toContainText('A value is needed')
  })

  test('says so when there is nothing to filter on', async ({page}) => {
    await stub(page, 'exhaustive', [], [])
    await open(page)

    await expect(page.getByTestId('filter-builder-no-columns')).toBeVisible()
  })

  test('says so when the columns cannot be loaded', async ({page}) => {
    await stub(page, 'exhaustive', [], 'not an array')
    await open(page)

    await expect(page.getByTestId('filter-builder-load-failed')).toBeVisible()
  })

  test('has no accessibility violations', async ({page}) => {
    await stub(page, 'exhaustive')
    await open(page)
    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({page})
      .include('[data-testid="exhaustive-filter-builder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
