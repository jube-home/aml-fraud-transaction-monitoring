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
import { me } from './hierarchyStub'
import { settleAnimations } from './support'

const instance = (id: number, name: string, overrides: Record<string, unknown> = {}) => ({
  id,
  guid: `g${id}`,
  name,
  entityAnalysisModelId: 7,
  responsePayload: true,
  reportTable: false,
  active: true,
  createdUser: 'maker',
  createdDate: '2026-10-20T09:00:00Z',
  updatedUser: null,
  updatedDate: null,
  version: 3,
  deletedUser: null,
  deletedDate: null,
  ...overrides,
})

const instances = [
  instance(1, 'Fraud classifier'),
  instance(2, 'Pending instance'),
  instance(3, 'Rejected instance'),
  instance(4, 'Gone instance', { deletedUser: 'x' }),
  instance(5, 'Going instance', { deletedUser: 'x', entityAnalysisModelId: null }),
]

const status = (entityId: number, overrides: Record<string, unknown> = {}) => ({
  kind: 1,
  entityId,
  currentVersion: 3,
  effectiveVersion: 2,
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
  status(2, { pending: true, canApprove: true }),
  status(3, { rejected: true, returnedNote: 'Needs a reason.' }),
  status(5, { pending: true }),
]

const trainedModel = {
  id: 11,
  guid: 'm11',
  askJoobyAnalyticsInstanceId: 1,
  name: 'Fraud scorer',
  kind: 1,
  source: 1,
  active: true,
  activeVersionId: 102,
  activeVersionNumber: 2,
  createdDate: '2026-10-19T09:00:00Z',
}

const version = (id: number, number: number, overrides: Record<string, unknown> = {}) => ({
  id,
  trainedModelId: 11,
  versionNumber: number,
  isActive: false,
  createdDate: '2026-10-20T09:00:00Z',
  testRows: 1200,
  testRocAuc: 0.9,
  testAveragePrecision: 0.5,
  testLogLoss: 0.3,
  testBrierScore: 0.1,
  testExpectedCalibrationError: 0.05,
  warningCodes: [],
  transformedVariableCount: null,
  ...overrides,
})

const versions = [
  version(101, 1),
  version(102, 2, {
    isActive: true,
    testRocAuc: 0.95,
    testAveragePrecision: 0.6,
    testLogLoss: 0.25,
    testBrierScore: 0.08,
    testExpectedCalibrationError: 0.06,
    warningCodes: ['ConstantColumn'],
  }),
  version(103, 3, {
    testRocAuc: 0.93,
    testAveragePrecision: 0.62,
    testLogLoss: 0.25,
    testBrierScore: 0.08,
    testExpectedCalibrationError: 0.04,
  }),
]

const comparison = {
  versionA: versions[1],
  versionB: versions[2],
  sameSeed: true,
  sameSplitSizes: false,
  sameVariables: true,
  metrics: [
    { name: 'TestRocAuc', valueA: 0.95, valueB: 0.93, difference: -0.02 },
    { name: 'TestLogLoss', valueA: 0.25, valueB: 0.25, difference: 0 },
    { name: 'TestExpectedCalibrationError', valueA: 0.06, valueB: 0.04, difference: -0.02 },
    { name: 'TestPrecision', valueA: null, valueB: 0.5, difference: null },
  ],
  warningsOnlyInA: ['ConstantColumn'],
  warningsOnlyInB: [],
}

const narrative = (number: number, groups: Record<string, string[]>) => ({
  holistic: [`Version ${number} separates fraud well.`],
  groups,
})

const fullGroups = {
  ModelDiagram: ['The model is a logistic regression.'],
  Calibration: ['Calibration is good.'],
  Roc: ['Discrimination is strong.'],
}

const chart =
  '<svg class="jc" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 100" role="img" aria-label="chart"><rect x="10" y="10" width="100" height="30"/></svg>'

interface Seen {
  actives: Record<string, unknown>[]
  decisions: { path: string; body: Record<string, unknown> }[]
  actions: { path: string; body: Record<string, unknown> }[]
  deletes: string[]
  approveAll: Record<string, unknown>[]
  promotions: string[]
  compares: string[]
  charts: string[]
}

async function stub(
  page: Page,
  options: {
    permissions?: number[]
    groups?: Record<string, string[]>
    noEvidenceFor?: number
    activeStatus?: number
  } = {},
) {
  const seen: Seen = {
    actives: [],
    decisions: [],
    actions: [],
    deletes: [],
    approveAll: [],
    promotions: [],
    compares: [],
    charts: [],
  }

  await page.route('**/api/Chart/AnalyticsModelVersion/**', (route) => {
    seen.charts.push(new URL(route.request().url()).pathname)

    return route.fulfill({ contentType: 'image/svg+xml', body: chart })
  })
  await page.route('**/api/Chart/AnalyticsModelVersion/Narrative/*', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').pop())

    return route.fulfill({ json: narrative(id - 100, options.groups ?? fullGroups) })
  })
  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [45, 46, 47, 48] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({ json: [{ id: 7, name: 'Payments' }] }),
  )
  await page.route('**/api/AskJoobyAnalyticsInstance**', (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname

    if (request.method() === 'GET') return route.fulfill({ json: instances })

    if (request.method() === 'DELETE') {
      seen.deletes.push(path)

      return route.fulfill({ status: 200, body: '' })
    }

    if (path.endsWith('/active')) {
      seen.actives.push({ path, ...request.postDataJSON() })

      return options.activeStatus && options.activeStatus !== 200
        ? route.fulfill({ status: options.activeStatus, json: {} })
        : route.fulfill({ json: {} })
    }

    seen.actions.push({ path, body: request.postDataJSON() })

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityApproval/ValueStatus', (route) =>
    route.fulfill({ json: statuses }),
  )
  await page.route('**/api/EntityApproval/Approve', (route) => {
    seen.decisions.push({ path: '/Approve', body: route.request().postDataJSON() })

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityApproval/Reject', (route) => {
    seen.decisions.push({ path: '/Reject', body: route.request().postDataJSON() })

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityApproval/ApproveValues', (route) => {
    seen.approveAll.push(route.request().postDataJSON())

    return route.fulfill({ json: { approved: 1, refusals: ['One was already approved.'] } })
  })
  await page.route('**/api/AskJoobyAnalyticsModel**', (route) => {
    const url = new URL(route.request().url())
    const path = url.pathname

    if (path === '/api/AskJoobyAnalyticsModel') return route.fulfill({ json: [trainedModel] })

    if (path.endsWith('/versions')) return route.fulfill({ json: versions })

    if (path.includes('/Compare/')) {
      seen.compares.push(path)

      return route.fulfill({ json: comparison })
    }

    if (path.endsWith('/PromotionEvidence')) {
      const id = Number(path.split('/').slice(-2, -1)[0])

      seen.promotions.push(String(id))

      if (id === options.noEvidenceFor) return route.fulfill({ status: 409, json: {} })

      return route.fulfill({
        json: {
          versionId: id,
          versionNumber: id - 100,
          trainedModelId: 11,
          modelName: 'Fraud scorer',
          isActiveVersion: id === 102,
          instanceAllocated: true,
          allocatedEntityAnalysisModelId: 7,
          kind: 'Logistic',
          seed: 42,
          warnings:
            id === 102 ? [{ code: 'ConstantColumn', message: 'Column Notes is constant.' }] : [],
        },
      })
    }

    if (path.includes('/Version/')) {
      const id = Number(path.split('/').pop())

      return route.fulfill({
        json: { ...versions.find((item) => item.id === id), evidence: { coefficients: 3 } },
      })
    }

    return route.fulfill({ json: trainedModel })
  })

  return seen
}

async function open(page: Page, query = '') {
  await page.goto(`/tools?tool=advanced-analytics${query}`)
  await expect(page.getByTestId('analytics-instances')).toBeVisible()
}

async function openModel(page: Page) {
  await page
    .locator('[aria-label="AskJooby Analytics instances"] .tabulator-row')
    .first()
    .getByTestId('grid-expand')
    .click()
  await page
    .getByTestId('analytics-models-1')
    .getByRole('button', { name: 'Open the model' })
    .click()
  await expect(page.getByTestId('analytics-band')).toBeVisible()
}

test.describe('AskJooby Analytics instances', () => {
  test.use({ viewport: { width: 1700, height: 1300 } })

  test('lists the instances with the allocated model and approval state, hiding a settled deletion', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    const rows = page.locator('[aria-label="AskJooby Analytics instances"] .tabulator-row')

    await expect(rows).toHaveCount(4)
    await expect(rows.first()).toContainText('Payments')
    await expect(rows.first()).toContainText('Approved')
    await expect(page.getByText('Awaiting approval', { exact: true })).toBeVisible()
    await expect(page.getByText('Rejected', { exact: true })).toBeVisible()
    await expect(page.getByText('Deletion awaiting approval')).toBeVisible()
    await expect(page.getByText('Gone instance')).toHaveCount(0)
    await expect(page.getByText('Not allocated')).toBeVisible()
  })

  test('approves and rejects with a checker note', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('grid-input-note').fill('Looks right.')
    await page.getByTestId('grid-action-approve').click()

    await expect.poll(() => seen.decisions.length).toBe(1)
    expect(seen.decisions[0]).toMatchObject({
      path: '/Approve',
      body: { kind: 1, entityId: 2, version: 3, note: 'Looks right.' },
    })

    await page.getByTestId('grid-action-reject').click()
    await expect.poll(() => seen.decisions.length).toBe(2)
    expect(seen.decisions[1]!.path).toBe('/Reject')
  })

  test('resubmits and retracts a rejected instance with a return note', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('grid-input-returnNote').fill('Added the reason.')
    await page.getByTestId('grid-action-resubmit').click()
    await expect.poll(() => seen.actions.length).toBe(1)
    expect(seen.actions[0]).toEqual({
      path: '/api/AskJoobyAnalyticsInstance/3/Resubmit',
      body: { note: 'Added the reason.' },
    })

    await page.getByTestId('grid-action-retract').click()
    await expect.poll(() => seen.actions.length).toBe(2)
    expect(seen.actions[1]!.path).toBe('/api/AskJoobyAnalyticsInstance/3/Retract')
  })

  test('approves every pending instance of the allocated models and shows what was refused', async ({
    page,
  }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('analytics-approve-all').click()

    await expect(page.getByTestId('analytics-reasons')).toContainText('One was already approved.')
    expect(seen.approveAll).toEqual([{ kind: 1, entityId: 7 }])
  })

  test('switches an instance on and off, and only with the permission', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('instance-active-1').uncheck()

    await expect.poll(() => seen.actives.length).toBe(1)
    expect(seen.actives[0]).toMatchObject({ active: false })
  })

  test('freezes the active switch without permission 47', async ({ page }) => {
    await stub(page, { permissions: [45, 46] })
    await open(page)

    await expect(page.getByTestId('instance-active-1')).toBeDisabled()
  })

  test('deletes after a confirmation that names the instance, refusing an allocated one without both permissions', async ({
    page,
  }) => {
    const seen = await stub(page)
    await open(page)

    await page
      .locator('[aria-label="AskJooby Analytics instances"] .tabulator-row')
      .first()
      .getByTestId('grid-action-delete')
      .click()

    await expect(page.getByTestId('analytics-confirm')).toContainText('Delete Fraud classifier?')
    await page.getByTestId('analytics-confirm-yes').click()

    await expect.poll(() => seen.deletes).toEqual(['/api/AskJoobyAnalyticsInstance/1'])
  })

  test('disables delete of an allocated instance without the Promote Model permission', async ({
    page,
  }) => {
    await stub(page, { permissions: [45, 46, 48] })
    await open(page)

    await expect(
      page
        .locator('[aria-label="AskJooby Analytics instances"] .tabulator-row')
        .first()
        .getByTestId('grid-action-delete'),
    ).toBeDisabled()
  })

  test('says so when the user may not see Advanced Analytics', async ({ page }) => {
    await stub(page, { permissions: [] })
    await page.goto('/tools?tool=advanced-analytics')

    await expect(page.getByTestId('analytics-refused')).toBeVisible()
  })
})

test.describe('Advanced Analytics model story', () => {
  test.use({ viewport: { width: 1700, height: 1400 } })

  test('opens a model from its instance under a trail and leads with the live version and its standing', async ({
    page,
  }) => {
    await stub(page)
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-model-name')).toHaveText('Fraud scorer')
    await expect(page.getByTestId('analytics-live')).toContainText('Live: v2')
    await expect(page.getByTestId('analytics-standing')).toHaveText(
      'Live is best on 3 of 5 metrics',
    )
    await expect(page.getByTestId('analytics-standing-line')).toContainText('best on 3 of 5')
    await expect(page.getByTestId('analytics-viewing')).toContainText('1 warning')
    await expect(page.getByTestId('analytics-viewing')).not.toContainText('1 warnings')
    await expect(page.getByTestId('canvas-index').getByRole('button')).toHaveText([
      'Which version is live',
      'What it says',
      'How well it performs',
      'What the model looks like',
      'The stored evidence',
    ])

    await page.getByTestId('analytics-trail-instances').click()
    await expect(page.getByTestId('analytics-instances')).toBeVisible()
  })

  test('compares the versions side by side, metric by metric, with the best marked in words', async ({
    page,
  }) => {
    await stub(page)
    await open(page)
    await openModel(page)

    const table = page.getByTestId('analytics-versions')

    await expect(table.locator('.tabulator-row')).toHaveCount(8)
    await expect(table.locator('.tabulator-col-title')).toHaveText([
      'Metric',
      'v3',
      'v2 · live',
      'v1',
    ])
    await expect(table.getByText('best', { exact: true })).toHaveCount(7)
    await expect(table.getByText('0.9500').first()).toBeInViewport()
    await expect(page.getByTestId('analytics-show-101')).toBeInViewport()
    await expect(page.getByTestId('analytics-compare-102')).toHaveCount(0)
    await expect(page.getByTestId('analytics-show-102')).toBeDisabled()
  })

  test('shows another version everywhere below and returns to the live one', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-narrative')).toContainText('Version 2 separates')

    await page.getByTestId('analytics-show-101').click()

    await expect(page.getByTestId('analytics-viewing')).toContainText('Viewing v1')
    await expect(page.getByTestId('analytics-narrative')).toContainText('Version 1 separates')
    expect(seen.promotions).toEqual(expect.arrayContaining(['102', '101']))

    await page.getByTestId('analytics-back-to-live').click()

    await expect(page.getByTestId('analytics-viewing')).toContainText('Viewing v2')
    await expect(page.getByTestId('analytics-back-to-live')).toHaveCount(0)
  })

  test('compares a version with the live one and says when they are not fairly comparable', async ({
    page,
  }) => {
    const seen = await stub(page)
    await open(page)
    await openModel(page)

    await page.getByTestId('analytics-compare-103').click()

    const panel = page.getByTestId('analytics-comparison')

    await expect(panel).toContainText('Live v2 against v3')
    await expect(page.getByTestId('analytics-not-comparable')).toContainText(
      'different train, validation and test split sizes',
    )
    await expect(panel).toContainText('Worse')
    await expect(panel).toContainText('Better')
    await expect(panel).toContainText('No change')
    await expect(panel).toContainText('Warnings only on the live version: ConstantColumn.')
    expect(seen.compares).toEqual(['/api/AskJoobyAnalyticsModel/Compare/102/103'])
  })

  test('shows the live warnings beside the narrative', async ({ page }) => {
    await stub(page)
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-warning')).toContainText('Column Notes is constant.')
    await expect(page.getByTestId('analytics-facts')).toContainText(
      'Instance allocated to the Jube model Payments',
    )
  })

  test('draws calibration and ROC only when the narrative says the model has them', async ({
    page,
  }) => {
    const seen = await stub(page)
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-calibration')).toContainText('Calibration is good.')
    await expect(page.getByTestId('analytics-roc')).toContainText('Discrimination is strong.')
    await expect
      .poll(() => seen.charts)
      .toEqual(
        expect.arrayContaining([
          '/api/Chart/AnalyticsModelVersion/Calibration/102',
          '/api/Chart/AnalyticsModelVersion/Roc/102',
        ]),
      )
  })

  test('says so for a kind of model with no calibration or ROC', async ({ page }) => {
    await stub(page, { groups: { ModelDiagram: ['A forest.'] } })
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-no-performance')).toBeVisible()
    await expect(page.getByTestId('analytics-calibration')).toHaveCount(0)
  })

  test('opens the diagram and the stored evidence from the index', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-diagram')).toHaveCount(0)
    await expect(page.getByTestId('analytics-evidence')).toHaveCount(0)

    await page.getByTestId('canvas-index-looks').click()
    await page.getByTestId('canvas-index-evidence').click()

    await expect(page.getByTestId('analytics-diagram')).toContainText('logistic regression')
    await expect(page.getByTestId('evidence-tree')).toBeVisible()
    expect(seen.charts).toContain('/api/Chart/AnalyticsModelVersion/ModelDiagram/102')
  })

  test('says when a version has no stored evidence', async ({ page }) => {
    await stub(page, { noEvidenceFor: 102 })
    await open(page)
    await openModel(page)

    await expect(page.getByTestId('analytics-no-evidence')).toBeVisible()
  })

  test('has no accessibility violations with every section open', async ({ page }) => {
    await stub(page)
    await open(page)
    await openModel(page)
    await page.getByTestId('canvas-index-looks').click()
    await page.getByTestId('canvas-index-evidence').click()
    await expect(page.getByTestId('evidence-tree')).toBeVisible()
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })

  test('has no accessibility violations on the instance list', async ({ page }) => {
    await stub(page)
    await open(page)
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })
})
