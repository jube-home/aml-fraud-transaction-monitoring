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
import {modelOf} from './formModels'
import {setLanguage, settleAnimations, stubRuleBuilder} from './support'

const field = (overrides: Record<string, unknown>) => ({
  name: 'name',
  label: 'Name',
  description: '',
  type: 'string',
  nullable: true,
  required: false,
  minLength: null,
  maxLength: null,
  min: null,
  max: null,
  exclusiveMin: false,
  exclusiveMax: false,
  patterns: [],
  options: [],
  group: 'Identity',
  order: 10,
  widget: 'default',
  span: 1,
  readOnly: false,
  placeholder: null,
  help: null,
  editor: null,
  lookup: null,
  newDefault: null,
  visibleWhen: [],
  requiredWhen: [],
  enabledWhen: [],
  ...overrides,
})

const claimed = (name: string, type = 'string', order = 50) =>
  field({name, label: name, type, order, editor: 'ruleBuilder'})

const rule = {
  id: 7,
  guid: '00000000-0000-0000-0000-000000000007',
  name: 'Large amount',
  active: true,
  locked: false,
  version: 3,
  entityAnalysisModelId: 1,
  ruleScriptTypeId: 2,
  builderRuleScript: '',
  json: '',
  coderRuleScript: 'return Payload.Amount > 5',
}

const model = {
  area: 'EntityAnalysisModelActivationRule',
  endpoint: '/api/EntityAnalysisModelActivationRule',
  operations: ['list', 'read', 'create', 'update', 'delete'],
  keys: {id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name'},
  approval: null,
  roles: null,
  values: null,
  editors: [
    {
      kind: 'ruleBuilder',
      parseType: 5,
      roles: {
        scriptType: 'ruleScriptTypeId',
        builder: 'builderRuleScript',
        builderJson: 'json',
        coder: 'coderRuleScript',
      },
      reads: {},
    },
    {kind: 'ruleTools', parseType: 1, roles: {}, reads: {}},
  ],
  children: [],
  lockFields: ['locked'],
  groups: [{path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default'}],
  fields: [
    field({name: 'name', label: 'Name', required: true}),
    claimed('coderRuleScript'),
    claimed('builderRuleScript', 'string', 51),
    claimed('json', 'string', 52),
    claimed('ruleScriptTypeId', 'integer', 53),
  ],
  listColumns: [
    {field: 'name', title: 'Name', order: 10, sortable: true, filterable: true, hidden: false},
  ],
}

const modelForm = modelOf({
  area: 'EntityAnalysisModel',
  keys: {id: 'id', parent: null, naturalKey: 'name'},
  children: [
    {
      area: 'EntityAnalysisModelActivationRule',
      label: 'Activation rules',
      hub: '',
      order: 10,
      parentKey: 'entityAnalysisModelId',
      parentField: 'id',
      listRoute: '/api/EntityAnalysisModelActivationRule/ByEntityAnalysisModelId',
    },
  ],
})

const me = (permissions: number[]) => ({
  userName: 'alex',
  landlord: false,
  permissions,
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: false,
  svirfd: false,
  sessionExpiry: null,
})

const completions = [
  {
    name: 'Payload.Amount',
    value: 'Payload.Amount',
    meta: 'Amount:double',
    score: 1000,
    dataType: 'double',
    group: 'Payload',
  },
]

const operators = [
  {
    type: 'equal',
    label: 'is equal to',
    group: 'Comparison',
    arguments: [{name: 'Value', kind: 'value', many: false}],
    applyTo: ['string', 'integer', 'double', 'datetime', 'boolean'],
    template: ' = ?',
  },
]

interface Sent {
  submitted: Record<string, unknown>[]
  tuned: Record<string, unknown>[]
  shadowed: Record<string, unknown>[]
}

async function stub(
  page: Page,
  options: { permissions?: number[]; fieldsStatus?: number } = {},
): Promise<Sent> {
  const sent: Sent = {submitted: [], tuned: [], shadowed: []}

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await stubRuleBuilder(page)
  await page.route('**/api/Me', (route) =>
    route.fulfill({json: me(options.permissions ?? [61, 60, 26])}),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({json: operators}))
  await page.route('**/api/Completions/ByEntityAnalysisModelIdParseTypeId**', (route) =>
    route.fulfill({json: completions}),
  )
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({json: ['EntityAnalysisModel', 'EntityAnalysisModelActivationRule']}),
  )
  await page.route('**/api/FormModel/EntityAnalysisModel', (route) =>
    route.fulfill({json: modelForm}),
  )
  await page.route('**/api/FormModel/EntityAnalysisModelActivationRule', (route) =>
    route.fulfill({json: model}),
  )
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({json: [{id: 1, guid: 'g1', name: 'Fraud model'}]}),
  )
  await page.route('**/api/EntityAnalysisModel/1', (route) =>
    route.fulfill({json: {id: 1, guid: 'g1', name: 'Fraud model'}}),
  )
  await page.route('**/api/EntityAnalysisModelActivationRule/ByEntityAnalysisModelId/1', (route) =>
    route.fulfill({json: [rule]}),
  )
  await page.route('**/api/EntityAnalysisModelActivationRule/7', (route) =>
    route.fulfill({json: rule}),
  )
  await page.route('**/api/EntityApproval/Status**', (route) =>
    route.fulfill({status: 404, body: ''}),
  )
  await page.route('**/api/EntityAnalysisModelBacktest/Fields/1', (route) =>
    options.fieldsStatus && options.fieldsStatus !== 200
      ? route.fulfill({status: options.fieldsStatus, json: {}})
      : route.fulfill({
        json: {
          filterFields: [{name: 'Payload.Amount', dataType: 'Double'}],
          classFields: [{name: 'Tag.Mule', dataType: 'Boolean'}],
        },
      }),
  )
  await page.route('**/api/EntityAnalysisModelBacktest/ByEntityAnalysisModelId/1**', (route) =>
    route.fulfill({json: []}),
  )
  await page.route('**/api/EntityAnalysisModelBacktest', (route) => {
    sent.submitted.push(route.request().postDataJSON())

    return route.fulfill({json: {valid: false, instance: null, errors: []}})
  })
  await page.route('**/api/EntityAnalysisModelBacktest/Tune', (route) => {
    sent.tuned.push(route.request().postDataJSON())

    return route.fulfill({
      json: {
        valid: true,
        errors: [],
        variants: [
          {
            value: '100',
            evaluated: 1000,
            fired: 40,
            runtimeErrors: 0,
            truePositives: 30,
            falsePositives: 10,
            falseNegatives: 20,
            trueNegatives: 940,
            precision: 0.75,
            recall: 0.6,
          },
          {
            value: '250.5',
            evaluated: 1000,
            fired: 12,
            runtimeErrors: 0,
            truePositives: 10,
            falsePositives: 2,
            falseNegatives: 40,
            trueNegatives: 948,
            precision: 0.8333,
            recall: 0.2,
          },
        ],
      },
    })
  })
  await page.route('**/api/EntityAnalysisModelActivationRule/SaveShadowVariant', (route) => {
    sent.shadowed.push(route.request().postDataJSON())

    return route.fulfill({json: {id: 99, guid: 'g99', name: 'Large amount shadow 250.5'}})
  })
  await page.route(
    '**/api/EntityAnalysisModelDependency/Dependencies/1/ActivationRule/7',
    (route) => route.fulfill({json: {entity: null, dependencies: []}}),
  )
  await page.route('**/api/EntityAnalysisModelDependency/Dependents/1/ActivationRule/7', (route) =>
    route.fulfill({json: {entity: null, dependents: []}}),
  )
  await page.route('**/api/EntityAnalysisModelIntegrity/1', (route) =>
    route.fulfill({
      json: {
        entityAnalysisModelId: 1,
        modelName: 'Fraud model',
        checkedDate: '2026-11-01T08:00:00Z',
        checks: [],
      },
    }),
  )

  return sent
}

async function openRule(page: Page) {
  await page.goto('/models')
  await page
    .locator('.tabulator-row', {hasText: 'Fraud model'})
    .getByTestId('grid-action-open')
    .click()
  await page.getByTestId('hub-open-EntityAnalysisModelActivationRule').click()
  await expect(page.getByTestId('crud-panel')).toBeVisible()
  await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
}

async function expand(page: Page, name: string) {
  await page.getByTestId(`rule-tools-${name}`).locator('summary').click()
}

test.describe('Rule tools on an activation rule', () => {
  test.use({viewport: {width: 1500, height: 1400}})

  test('offers the permitted tools on a saved rule, each loading when opened', async ({page}) => {
    await stub(page)
    await openRule(page)

    await expect(page.getByTestId('crud-rule-tools')).toBeVisible()
    await expect(page.getByTestId('backtest-panel')).toHaveCount(0)

    await expand(page, 'dependencies')
    await expect(page.getByTestId('rule-dependencies')).toContainText('Nothing uses this rule.')

    await expand(page, 'integrity')
    await expect(page.getByTestId('rule-integrity-none')).toBeVisible()

    await expand(page, 'backtest')
    await expect(page.getByTestId('backtest-run')).toBeVisible()
    await expect(page.getByTestId('backtest-panel')).toContainText('No backtests of this rule yet.')
  })

  test('backtests the rule as it is in the editor', async ({page}) => {
    const sent = await stub(page)
    await openRule(page)

    await expand(page, 'backtest')
    await page.getByTestId('backtest-run').click()

    await expect.poll(() => sent.submitted.length).toBe(1)
    expect(sent.submitted[0]).toMatchObject({
      request: {
        entityAnalysisModelId: 1,
        ruleType: 'ActivationRule',
        ruleId: 7,
        ruleText: 'return Payload.Amount > 5',
      },
    })
  })

  test('backtests the unsaved text typed into the editor, not the stored rule', async ({
                                                                                         page,
                                                                                       }) => {
    const sent = await stub(page)
    await openRule(page)

    await expect(page.getByTestId('rule-coder')).toContainText('return Payload.Amount > 5')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('ControlOrMeta+A')
    await page.keyboard.type('return Payload.Amount > 987')

    await expand(page, 'backtest')
    await page.getByTestId('backtest-run').click()

    await expect.poll(() => sent.submitted.length).toBe(1)
    expect(sent.submitted[0]).toMatchObject({
      request: {ruleId: 7, ruleText: 'return Payload.Amount > 987'},
    })
  })

  test('says a rule with no placeholder cannot be tuned', async ({page}) => {
    await stub(page)
    await openRule(page)

    await expand(page, 'backtest')
    await expect(page.getByTestId('tune-no-placeholder')).toBeVisible()
    await expect(page.getByTestId('tune-run')).toHaveCount(0)
  })

  test('sweeps candidate thresholds and saves one as a shadow rule', async ({page}) => {
    const sent = await stub(page)
    await openRule(page)

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('ControlOrMeta+A')
    await page.keyboard.type('return Payload.Amount > {{threshold}}')

    await expand(page, 'backtest')
    await expect(page.getByTestId('tune-placeholder')).toHaveText('{{threshold}}')
    await expect(page.getByTestId('tune-run')).toBeDisabled()
    await page.getByTestId('tune-candidates').fill('100, 250.5')
    await page.getByTestId('tune-run').click()

    await expect(page.getByTestId('tune-row-250.5')).toContainText('83.3%')
    expect(sent.tuned[0]).toMatchObject({
      placeholder: '{{threshold}}',
      candidates: ['100', '250.5'],
      request: {ruleId: 7, ruleText: 'return Payload.Amount > {{threshold}}'},
    })

    await page.getByTestId('tune-shadow-250.5').click()
    await expect(page.getByTestId('tune-saved')).toContainText('Large amount shadow 250.5')
    expect(sent.shadowed[0]).toEqual({
      baseRuleId: 7,
      ruleText: 'return Payload.Amount > {{threshold}}',
      placeholder: '{{threshold}}',
      value: '250.5',
    })
  })

  test('refuses candidates that are not plain numbers', async ({page}) => {
    await stub(page)
    await openRule(page)

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('ControlOrMeta+A')
    await page.keyboard.type('return Payload.Amount > {{threshold}}')
    await expand(page, 'backtest')

    await page.getByTestId('tune-candidates').fill('10, abc')
    await expect(page.getByTestId('tune-run')).toBeDisabled()
    await page.getByTestId('tune-candidates').fill('1e5')
    await expect(page.getByTestId('tune-run')).toBeDisabled()
  })

  test('says so when the backtest fields cannot be loaded', async ({page}) => {
    await stub(page, {fieldsStatus: 500})
    await openRule(page)

    await expand(page, 'backtest')
    await expect(page.getByTestId('backtest-panel')).toContainText(
      'The backtest fields could not be loaded.',
    )
  })

  test('shows only the tools the user holds permission for, and says when there are none', async ({
                                                                                                    page,
                                                                                                  }) => {
    await stub(page, {permissions: [60]})
    await openRule(page)

    await expect(page.getByTestId('rule-tools-integrity')).toBeVisible()
    await expect(page.getByTestId('rule-tools-backtest')).toHaveCount(0)
    await expect(page.getByTestId('rule-tools-dependencies')).toHaveCount(0)
  })

  test('is denied entirely without any of the permissions', async ({page}) => {
    await stub(page, {permissions: []})
    await openRule(page)

    await expect(page.getByTestId('rule-tools-denied')).toBeVisible()
    await expect(page.getByTestId('rule-tools-backtest')).toHaveCount(0)
  })

  test('is hidden for a rule that is not saved yet', async ({page}) => {
    await stub(page)
    await openRule(page)
    await expect(page.getByTestId('crud-rule-tools')).toBeVisible()

    await page.getByTestId('crud-close').click()
    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('crud-form')).toBeVisible()
    await expect(page.getByTestId('crud-rule-tools')).toHaveCount(0)
  })

  test('opens and closes with the keyboard', async ({page}) => {
    await stub(page)
    await openRule(page)

    const summary = page.getByTestId('rule-tools-dependencies').locator('summary')

    await summary.focus()
    await page.keyboard.press('Enter')
    await expect(page.getByTestId('rule-dependencies')).toBeVisible()
    await page.keyboard.press('Enter')
    await expect(page.getByTestId('rule-dependencies')).toBeHidden()
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme`, async ({page}) => {
      await stub(page)
      await page.addInitScript((stored) => localStorage.setItem('jube.theme', stored), theme)
      await openRule(page)
      await expand(page, 'dependencies')
      await expand(page, 'integrity')
      await expand(page, 'backtest')
      await expect(page.getByTestId('backtest-run')).toBeVisible()
      await settleAnimations(page)

      const results = await new AxeBuilder({page})
        .include('[data-testid="crud-rule-tools"]')
        .analyze()

      expect(results.violations.map((v) => v.id)).toEqual([])
    })
  }

  test('has no accessibility violations in Arabic and mirrors', async ({page}) => {
    await stub(page)
    await openRule(page)
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await expand(page, 'dependencies')
    await expand(page, 'integrity')
    await expand(page, 'backtest')
    await expect(page.getByTestId('backtest-run')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({page})
      .include('[data-testid="crud-rule-tools"]')
      .analyze()

    expect(results.violations.map((violation) => violation.id)).toEqual([])
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  })
})
