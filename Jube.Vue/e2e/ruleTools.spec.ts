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

const request = {
  entityAnalysisModelId: 1,
  ruleType: 'ActivationRule',
  ruleId: 5,
  ruleText: 'Amount > 1000',
  filterJson: null,
  classJson: null,
  from: null,
  to: null,
  limit: 100000,
  sampleSize: 10,
}

const instance = (overrides: Record<string, unknown> = {}) => ({
  id: 7,
  ruleType: 'ActivationRule',
  ruleId: 5,
  status: 'Succeeded',
  progress: 100,
  scanned: 500,
  evaluated: 400,
  createdDate: '2026-10-21T10:00:00Z',
  createdUser: 'analyst',
  startedDate: '2026-10-21T10:00:01Z',
  error: null,
  request,
  fired: 40,
  positives: 30,
  truePositives: 25,
  falsePositives: 15,
  falseNegatives: 5,
  trueNegatives: 355,
  ...overrides,
})

const sample = (guid: string) => ({
  entityAnalysisModelInstanceEntryGuid: guid,
  entryKeyValue: `entry-${guid}`,
  referenceDate: '2026-10-20T09:00:00Z',
  fired: true,
  positive: true,
  error: null,
})

const result = {
  valid: true,
  scanned: 500,
  filteredOut: 100,
  filterErrors: 0,
  evaluated: 400,
  limitReached: false,
  earliestReferenceDate: '2026-09-21T00:00:00Z',
  latestReferenceDate: '2026-10-20T00:00:00Z',
  fired: 40,
  notFired: 360,
  runtimeErrors: 0,
  aborted: false,
  abortReason: null,
  classDefined: true,
  positives: 30,
  falsePositives: 15,
  falseNegatives: 5,
  trueNegatives: 355,
  truePositives: 25,
  durationMicroseconds: 1200,
  tagsInSample: [{ name: 'Risky', count: 3 }],
  truePositiveSamples: [sample('aaa')],
  falsePositiveSamples: [],
  falseNegativeSamples: [],
  errorSamples: [],
  errors: [],
}

const explanation = {
  valid: true,
  passedFilter: true,
  filterError: null,
  fired: true,
  ruleError: null,
  positive: true,
  tags: ['Risky'],
  values: [{ name: 'Amount', value: '1500' }],
  errors: [],
}

const chart =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 100" role="img" aria-label="chart"><rect x="10" y="10" width="100" height="30"/></svg>'

const dependencies = {
  entity: { kind: 'ActivationRule', name: 'High value', active: true },
  dependencies: [
    {
      dependent: { kind: 'ActivationRule', name: 'High value', active: true },
      target: { kind: 'TtlCounter', name: 'Spend', active: true },
      dependencyKind: 'Counter',
      name: 'Spend',
      line: 1,
      depth: 1,
      via: null,
    },
  ],
}

const dependents = {
  entity: { kind: 'ActivationRule', name: 'High value', active: true },
  dependents: [
    {
      dependent: { kind: 'ActivationRule', name: 'Follow up', active: true },
      target: { kind: 'ActivationRule', name: 'High value', active: true },
      dependencyKind: 'Rule',
      name: 'High value',
      line: 3,
      depth: 1,
      via: 'Spend',
    },
  ],
}

const report = {
  entityAnalysisModelId: 1,
  modelName: 'Payments',
  checkedDate: '2026-10-21T10:00:00Z',
  engineSource: 'in-process',
  errors: 1,
  warnings: 0,
  infos: 0,
  checks: [
    {
      code: 'R1',
      title: 'Rule references a missing counter',
      severity: 'Error',
      category: 'Dependencies',
      entityKind: 'ActivationRule',
      entityId: 5,
      entityName: 'High value',
      message: 'Counter Spend does not exist.',
    },
    {
      code: 'R9',
      title: 'Another rule problem',
      severity: 'Error',
      category: 'Dependencies',
      entityKind: 'ActivationRule',
      entityId: 99,
      entityName: 'Other rule',
      message: 'Unrelated.',
    },
  ],
}

interface Seen {
  submits: Record<string, unknown>[]
  stops: number[]
  explains: Record<string, unknown>[]
  charts: string[]
}

async function stub(
  page: Page,
  options: {
    permissions?: number[]
    invalid?: boolean
    instances?: object[]
    outcome?: object
  } = {},
) {
  const seen: Seen = { submits: [], stops: [], explains: [], charts: [] }
  let list = options.instances ?? []

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [60, 61] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/EntityAnalysisModelIntegrity/Models', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Payments', active: true }] }),
  )
  await page.route('**/api/EntityAnalysisModelIntegrity/1', (route) =>
    route.fulfill({ json: report }),
  )
  await page.route('**/api/EntityAnalysisModelBacktest/Fields/1', (route) =>
    route.fulfill({
      json: {
        filterFields: [{ name: 'Amount', dataType: 'Double' }],
        classFields: [{ name: 'Fraud', dataType: 'Boolean' }],
      },
    }),
  )
  await page.route('**/api/EntityAnalysisModelBacktest/ByEntityAnalysisModelId/1**', (route) =>
    route.fulfill({ json: list }),
  )
  await page.route('**/api/EntityAnalysisModelBacktest', (route) => {
    if (route.request().method() !== 'POST') return route.fallback()

    seen.submits.push(route.request().postDataJSON())

    if (options.invalid) {
      return route.fulfill({
        json: {
          valid: false,
          instance: null,
          errors: [
            { propertyName: 'RuleText', errorCode: 'X', message: 'Rule text is invalid.', line: 1 },
          ],
        },
      })
    }

    list = [instance({ status: 'Running', progress: 10 })]

    return route.fulfill({ json: { valid: true, instance: list[0], errors: [] } })
  })
  await page.route('**/api/EntityAnalysisModelBacktest/7/Result', (route) =>
    route.fulfill({ json: options.outcome ?? result }),
  )
  await page.route('**/api/EntityAnalysisModelBacktest/7/Stop', (route) => {
    seen.stops.push(7)
    list = [instance({ status: 'Stopped' })]

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityAnalysisModelBacktest/Explain', (route) => {
    seen.explains.push(route.request().postDataJSON())

    return route.fulfill({ json: explanation })
  })
  await page.route('**/api/Chart/Backtest/**', (route) => {
    seen.charts.push(new URL(route.request().url()).pathname)

    return route.fulfill({ contentType: 'image/svg+xml', body: chart })
  })
  await page.route('**/api/EntityAnalysisModelDependency/Dependencies/**', (route) =>
    route.fulfill({ json: dependencies }),
  )
  await page.route('**/api/EntityAnalysisModelDependency/Dependents/**', (route) =>
    route.fulfill({ json: dependents }),
  )

  return seen
}

async function open(page: Page, ruleId = '5') {
  await page.goto('/tools?tool=rule-tools')
  await expect(page.getByTestId('model-picker')).toHaveValue('1')
  await page.getByTestId('bench-rule-id').fill(ruleId)
  await expect(page.getByTestId('rule-tools')).toBeVisible()
}

test.describe('Rule tools', () => {
  test.use({ viewport: { width: 1500, height: 1300 } })

  test('submits a backtest with the editor rule text and the chosen limits', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('rule-tools-backtest').locator('summary').click()
    await page.getByTestId('backtest-limit').fill('5000')
    await page.getByTestId('backtest-sample').fill('3')
    await page.getByTestId('backtest-run').click()

    await expect(page.getByTestId('backtest-note')).toBeVisible()
    expect(seen.submits).toHaveLength(1)
    expect(seen.submits[0]).toMatchObject({
      request: {
        entityAnalysisModelId: 1,
        ruleId: 5,
        ruleText: 'Amount > 1000',
        limit: 5000,
        sampleSize: 3,
      },
    })
  })

  test('shows the validation errors the server refuses a backtest with', async ({ page }) => {
    await stub(page, { invalid: true })
    await open(page)

    await page.getByTestId('rule-tools-backtest').locator('summary').click()
    await page.getByTestId('backtest-run').click()

    await expect(page.getByTestId('backtest-problems')).toContainText('Rule text is invalid.')
  })

  test('shows a finished backtest as four charts, and explains an example on demand', async ({
    page,
  }) => {
    const seen = await stub(page, { instances: [instance()] })
    await open(page)

    await page.getByTestId('rule-tools-backtest').locator('summary').click()
    await page.getByRole('button', { name: 'Show' }).click()

    await expect(page.getByTestId('backtest-result')).toBeVisible()
    await expect(page.getByTestId('backtest-charts').getByTestId('jube-chart')).toHaveCount(4)
    expect(seen.charts.sort()).toEqual([
      '/api/Chart/Backtest/Confusion',
      '/api/Chart/Backtest/Funnel',
      '/api/Chart/Backtest/Rates',
      '/api/Chart/Backtest/Tags',
    ])

    await page.getByTestId('examples-True positives').locator('summary').click()
    await page.getByRole('button', { name: 'Explain this transaction' }).click()
    await expect(page.getByTestId('backtest-explain')).toContainText('Amount')
    expect(seen.explains[0]).toMatchObject({ entityAnalysisModelInstanceEntryGuid: 'aaa' })
  })

  test('draws only the funnel when nothing was evaluated', async ({ page }) => {
    await stub(page, {
      instances: [instance({ evaluated: 0 })],
      outcome: { ...result, evaluated: 0, fired: 0, positives: 0, tagsInSample: [] },
    })
    await open(page)

    await page.getByTestId('rule-tools-backtest').locator('summary').click()
    await page.getByRole('button', { name: 'Show' }).click()

    await expect(page.getByTestId('backtest-nothing')).toBeVisible()
    await expect(page.getByTestId('backtest-charts').getByTestId('jube-chart')).toHaveCount(1)
  })

  test('stops a running backtest', async ({ page }) => {
    const seen = await stub(page, { instances: [instance({ status: 'Running', progress: 40 })] })
    await open(page)

    await page.getByTestId('rule-tools-backtest').locator('summary').click()
    await page.getByRole('button', { name: 'Stop' }).click()

    await expect.poll(() => seen.stops.length).toBe(1)
  })

  test('lists what the rule uses and what uses it', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('rule-tools-dependencies').locator('summary').click()
    const panel = page.getByTestId('rule-dependencies')

    await expect(panel).toContainText('Spend')
    await expect(panel).toContainText('Follow up')
  })

  test('asks for the rule to be saved before showing dependencies', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=rule-tools')
    await expect(page.getByTestId('model-picker')).toHaveValue('1')

    await page.getByTestId('rule-tools-dependencies').locator('summary').click()

    await expect(page.getByTestId('rule-dependencies')).toContainText('Save the rule')
  })

  test('shows only the integrity findings that concern this rule', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('rule-tools-integrity').locator('summary').click()
    const panel = page.getByTestId('rule-integrity')

    await expect(panel).toContainText('Rule references a missing counter')
    await expect(panel).not.toContainText('Another rule problem')
  })

  test('hides the sections the user has no permission for', async ({ page }) => {
    await stub(page, { permissions: [] })
    await open(page)

    await expect(page.getByTestId('rule-tools-backtest')).toHaveCount(0)
    await expect(page.getByTestId('rule-tools-integrity')).toHaveCount(0)
    await expect(page.getByTestId('rule-tools-dependencies')).toBeVisible()
  })

  test('has no accessibility violations with every section open', async ({ page }) => {
    await stub(page, { instances: [instance()] })
    await open(page)

    for (const name of ['backtest', 'dependencies', 'integrity'])
      await page.getByTestId(`rule-tools-${name}`).locator('summary').click()

    await expect(page.getByTestId('rule-integrity')).toBeVisible()
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })
})
