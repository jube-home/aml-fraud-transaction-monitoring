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
import { modelOf, stubFormModels } from './formModels'
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

const listValues = [
  { id: 1, listValue: 'alpha', deleteExpiryDate: null, deletedUser: null },
  { id: 2, listValue: 'bravo', deleteExpiryDate: '2026-10-20T10:30:00+00:00', deletedUser: null },
  { id: 3, listValue: 'gone', deleteExpiryDate: null, deletedUser: 'maker' },
  { id: 4, listValue: 'leaving', deleteExpiryDate: null, deletedUser: 'maker' },
]

const status = (id: number, overrides: Record<string, unknown> = {}) => ({
  kind: 3,
  entityId: id,
  currentVersion: 2,
  effectiveVersion: 1,
  pending: false,
  rejected: false,
  canApprove: false,
  makerUser: 'maker',
  approvalsRecorded: 0,
  approvalsRequired: 1,
  note: null,
  returnedNote: null,
  ...overrides,
})

const statuses = [
  status(1),
  status(2, { pending: true, canApprove: true }),
  status(3),
  status(4, { pending: true }),
]

interface Options {
  values?: unknown[]
  statuses?: unknown[]
  statusFails?: boolean
  mutationStatus?: number
  mutationBody?: unknown
}

async function stub(page: Page, calls: string[] = [], options: Options = {}) {
  await stubFormModels(page, [
    modelOf({
      area: 'EntityAnalysisModelDictionary',
      values: {
        area: 'EntityAnalysisModelDictionaryKvp',
        endpoint: '/api/EntityAnalysisModelDictionaryKvp',
        parentField: 'entityAnalysisModelDictionaryId',
        parentKind: 4,
        fields: [
          { field: 'kvpKey', title: 'Key', type: 'string' },
          { field: 'kvpValue', title: 'Value', type: 'number' },
        ],
      },
    }),
    modelOf({
      area: 'EntityAnalysisModelList',
      values: {
        area: 'EntityAnalysisModelListValue',
        endpoint: '/api/EntityAnalysisModelListValue',
        parentField: 'entityAnalysisModelListId',
        parentKind: 2,
        fields: [{ field: 'listValue', title: 'Value', type: 'string' }],
      },
    }),
  ])
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityApproval/ValueStatus', (route) =>
    options.statusFails
      ? route.fulfill({ status: 500, body: '' })
      : route.fulfill({ json: options.statuses ?? statuses }),
  )
  await page.route('**/api/EntityApproval/Approve', (route) => {
    calls.push(`Approve ${route.request().postData() ?? ''}`)

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityApproval/ApproveValues', (route) => {
    calls.push('ApproveValues')

    return route.fulfill({ json: { approved: 1, refusals: [] } })
  })
  await page.route('**/api/EntityAnalysisModelListValue**', (route) => {
    const request = route.request()
    const url = new URL(request.url())

    if (request.method() === 'GET') return route.fulfill({ json: options.values ?? listValues })

    calls.push(`${request.method()} ${url.pathname}${url.search} ${request.postData() ?? ''}`)

    return route.fulfill({
      status: options.mutationStatus ?? 200,
      json: options.mutationBody ?? {},
    })
  })
}

async function open(page: Page, kind = 'EntityAnalysisModelList') {
  await page.goto('/tools?tool=value-grid')
  await page.getByTestId('bench-kind').selectOption(kind)
  await page.getByTestId('bench-parent').fill('9')
  await page.getByTestId('bench-load').click()
  await expect(page.locator('.tabulator-row').first()).toBeVisible()
}

test.describe('ValueGrid', () => {
  test.use({ viewport: { width: 1700, height: 1100 } })

  test('lists live values and pending deletions, and hides resolved deletions', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    await expect(page.locator('.tabulator-row')).toHaveCount(3)
    await expect(page.getByText('gone')).toHaveCount(0)
    await expect(page.getByText('leaving')).toBeVisible()
    await expect(page.getByText('Deletion awaiting approval')).toBeVisible()
    await expect(page.getByText('Awaiting approval', { exact: true })).toBeVisible()
    await expect(page.getByText('20 Oct 2026, 10:30')).toBeVisible()
  })

  test('adds a value', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('value-add').click()
    await page.getByTestId('grid-input-listValue').fill('charlie')
    await page.getByTestId('grid-action-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toMatch(/^POST \/api\/EntityAnalysisModelListValue /)
    expect(calls[0]).toContain('"entityAnalysisModelListId":9')
    expect(calls[0]).toContain('"listValue":"charlie"')
  })

  test('edits a value in place and can cancel', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('grid-action-edit').first().click()
    await expect(page.getByTestId('grid-input-listValue')).toHaveValue('alpha')
    await expect(page.getByTestId('value-add')).toBeDisabled()

    await page.getByTestId('grid-action-cancel').click()

    await expect(page.getByTestId('grid-input-listValue')).toHaveCount(0)

    await page.getByTestId('grid-action-edit').first().click()
    await page.getByTestId('grid-input-listValue').fill('alpha2')
    await page.getByTestId('grid-action-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toMatch(/^PUT /)
    expect(calls[0]).toContain('"id":1')
    expect(calls[0]).toContain('"listValue":"alpha2"')
  })

  test('asks before deleting', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('grid-action-delete').first().click()
    await expect(page.getByTestId('value-confirm')).toBeVisible()
    await page.getByTestId('value-confirm-no').click()

    await expect(page.getByTestId('value-confirm')).toBeHidden()
    expect(calls).toHaveLength(0)

    await page.getByTestId('grid-action-delete').first().click()
    await page.getByTestId('value-confirm-yes').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toMatch(/^DELETE \/api\/EntityAnalysisModelListValue\?id=1 /)
  })

  test('approves a pending value with the typed note', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('grid-input-note').fill('ok')
    await page.getByTestId('grid-action-approve').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"entityId":2')
    expect(calls[0]).toContain('"version":2')
    expect(calls[0]).toContain('"note":"ok"')
  })

  test('approves everything pending in one go', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('value-approve-all').click()

    await expect.poll(() => calls).toContain('ApproveValues')
  })

  test('offers resubmit and retract on a rejected value', async ({ page }) => {
    await stub(page, [], {
      statuses: [
        status(1, { rejected: true, returnedNote: 'Fix it' }),
        status(2),
        status(3),
        status(4),
      ],
    })
    await open(page)

    await expect(page.getByTestId('grid-action-resubmit')).toBeVisible()
    await expect(page.getByTestId('grid-action-retract')).toBeVisible()
    await expect(page.getByText('Fix it')).toBeVisible()
  })

  test('finds values by text', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('value-search').fill('brav')

    await expect(page.locator('.tabulator-row')).toHaveCount(1)
  })

  test('says so when the approval status is unavailable and keeps every row', async ({ page }) => {
    await stub(page, [], { statusFails: true })
    await open(page)

    await expect(page.getByTestId('value-status-unavailable')).toBeVisible()
    await expect(page.locator('.tabulator-row')).toHaveCount(4)
  })

  test('lists the server reasons when a save is refused', async ({ page }) => {
    await stub(page, [], { mutationStatus: 400, mutationBody: { reasons: ['That value exists.'] } })
    await open(page)

    await page.getByTestId('value-add').click()
    await page.getByTestId('grid-input-listValue').fill('alpha')
    await page.getByTestId('grid-action-save').click()

    await expect(page.getByTestId('value-reasons')).toContainText('That value exists.')
  })

  test('pages through many values with the pager', async ({ page }) => {
    const many = Array.from({ length: 60 }, (_, index) => ({
      id: index + 1,
      listValue: `value-${String(index + 1).padStart(2, '0')}`,
      deleteExpiryDate: null,
      deletedUser: null,
    }))

    await stub(page, [], { values: many, statuses: [] })
    await open(page)

    const summary = page.getByTestId('grid-pager-summary')

    await expect(summary).toHaveText('1–25 of 60')
    await expect(page.getByTestId('grid-pager-page')).toHaveText('Page 1 of 3')
    await expect(page.getByTestId('grid-pager-previous')).toBeDisabled()
    await expect(page.getByTestId('grid-pager-first')).toBeDisabled()

    await page.getByTestId('grid-pager-next').click()

    await expect(summary).toHaveText('26–50 of 60')
    await expect(page.getByText('value-26')).toBeVisible()

    await page.getByTestId('grid-pager-last').click()

    await expect(summary).toHaveText('51–60 of 60')
    await expect(page.getByTestId('grid-pager-next')).toBeDisabled()

    await page.getByTestId('grid-pager-size').selectOption('50')

    await expect(summary).toHaveText('1–50 of 60')
    await expect(page.getByTestId('grid-pager-page')).toHaveText('Page 1 of 2')
  })

  test('the pager follows the search', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('value-search').fill('brav')

    await expect(page.getByTestId('grid-pager-summary')).toHaveText('1–1 of 1')
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await open(page)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
