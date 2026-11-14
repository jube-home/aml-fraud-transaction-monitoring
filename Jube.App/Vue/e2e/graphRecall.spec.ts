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

import {expect, type Page} from '@playwright/test'
import {test} from './fixtures'
import {modelOf, stubFormModels} from './formModels'
import {me, models as baseModels, stubHierarchy} from './hierarchyStub'

const area = 'EntityAnalysisModelGraphRecall'

const child = (
  childArea: string,
  label: string,
  hub: string,
  order: number,
  endpoint = childArea,
) => ({
  area: childArea,
  label,
  hub,
  order,
  parentKey: 'entityAnalysisModelId',
  parentField: 'id' as const,
  listRoute: `/api/${endpoint}/ByEntityAnalysisModelId`,
})

const modelForm = modelOf({
  area: 'EntityAnalysisModel',
  keys: {id: 'id', parent: null, naturalKey: 'name'},
  fields: [{name: 'active', type: 'boolean'}],
  lockFields: ['locked'],
  children: [
    child('EntityAnalysisModelTag', 'Tags', 'References', 60),
    child('EntityAnalysisModelHttpAdaptation', 'HTTP Adaptation', 'Machine Learning', 100),
    child(area, 'Graph Recall', 'Network', 112),
    child('CaseWorkflow', 'Cases Workflows', '', 130),
  ],
})

const httpAdaptationForm = modelOf({
  area: 'EntityAnalysisModelHttpAdaptation',
  keys: {id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name'},
})

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

const fieldsLookup = {
  source: `/api/${area}/Fields`,
  textField: 'label',
  valueField: 'key',
  parentField: 'entityAnalysisModelId',
  allowEmpty: false,
}

const graphRecallForm = {
  area,
  endpoint: `/api/${area}`,
  operations: ['list', 'read', 'create', 'update', 'delete'],
  keys: {id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name'},
  approval: null,
  roles: null,
  values: null,
  editors: [],
  children: [],
  lockFields: ['locked'],
  groups: [
    {path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default'},
    {path: 'Entity', label: 'Entity', order: 20, collapsed: false, widget: 'default'},
    {path: 'Output', label: 'Output', order: 30, collapsed: false, widget: 'default'},
    {path: 'Audit', label: 'Audit', order: 90, collapsed: true, widget: 'default'},
  ],
  fields: [
    field({name: 'name', label: 'Name', order: 10, placeholder: 'e.g. Payee'}),
    field({
      name: 'active',
      label: 'Active',
      type: 'boolean',
      order: 20,
      widget: 'switch',
      newDefault: true,
    }),
    field({
      name: 'locked',
      label: 'Locked',
      type: 'boolean',
      order: 30,
      widget: 'switch',
      newDefault: false,
    }),
    field({
      name: 'requestXPathName',
      label: 'Request XPath Name',
      group: 'Entity',
      order: 10,
      widget: 'select',
      lookup: fieldsLookup,
    }),
    field({
      name: 'nodeKey',
      label: 'Node Key',
      group: 'Entity',
      order: 20,
      widget: 'select',
      lookup: {...fieldsLookup, allowEmpty: true},
    }),
    field({
      name: 'counterpartName',
      label: 'Counterpart Name',
      group: 'Entity',
      order: 30,
      widget: 'select',
      lookup: {
        source: `/api/${area}/ByEntityAnalysisModelId`,
        textField: 'name',
        valueField: 'name',
        parentField: 'entityAnalysisModelId',
        allowEmpty: true,
      },
    }),
    field({
      name: 'graphRegistryGuid',
      label: 'Graph Registry',
      type: 'guid',
      group: 'Entity',
      order: 40,
      widget: 'select',
      lookup: {
        source: '/api/GraphRegistry',
        textField: 'name',
        valueField: 'guid',
        parentField: null,
        allowEmpty: true,
      },
    }),
    field({
      name: 'responsePayload',
      label: 'Response Payload',
      type: 'boolean',
      group: 'Output',
      order: 10,
      widget: 'switch',
      newDefault: false,
    }),
    field({
      name: 'reportTable',
      label: 'Report Table',
      type: 'boolean',
      group: 'Output',
      order: 20,
      widget: 'switch',
      newDefault: true,
    }),
    field({
      name: 'createdDate',
      label: 'Created Date',
      type: 'datetime',
      group: 'Audit',
      order: 10,
      readOnly: true,
    }),
    field({
      name: 'createdUser',
      label: 'Created User',
      group: 'Audit',
      order: 20,
      readOnly: true,
    }),
    field({
      name: 'version',
      label: 'Version',
      type: 'integer',
      group: 'Audit',
      order: 50,
      readOnly: true,
    }),
  ],
  listColumns: [
    {field: 'name', title: 'Name', order: 10, sortable: true, filterable: true, hidden: false},
    {
      field: 'active',
      title: 'Active',
      order: 20,
      sortable: true,
      filterable: true,
      hidden: false,
    },
    {
      field: 'requestXPathName',
      title: 'Field',
      order: 30,
      sortable: true,
      filterable: true,
      hidden: false,
    },
    {
      field: 'counterpartName',
      title: 'Counterpart',
      order: 40,
      sortable: true,
      filterable: true,
      hidden: false,
    },
  ],
}

const existing = {
  id: 3,
  guid: 'gr3',
  entityAnalysisModelId: 12,
  name: 'AccountId',
  active: true,
  locked: false,
  requestXPathName: 'AccountId',
  nodeKey: null,
  counterpartName: 'Payee',
  graphRegistryGuid: null,
  responsePayload: false,
  reportTable: true,
  version: 1,
}

const created = {
  id: 7,
  guid: 'gr7',
  entityAnalysisModelId: 12,
  name: 'Payee',
  active: true,
  locked: false,
  requestXPathName: 'ToAccountId',
  nodeKey: 'AccountId',
  counterpartName: '',
  graphRegistryGuid: 'r1',
  responsePayload: false,
  reportTable: true,
  version: 1,
}

interface Captured {
  fieldsAsked: string[]
  posted: string[]
}

async function stub(page: Page, captured: Captured) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await stubHierarchy(page)
  await stubFormModels(page, [
    modelForm,
    httpAdaptationForm,
    ...baseModels.filter((model) => model.area !== 'EntityAnalysisModel'),
  ])
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({
      json: [...baseModels.map((model) => model.area), httpAdaptationForm.area, area],
    }),
  )
  await page.route(`**/api/FormModel/${area}`, (route) => route.fulfill({json: graphRecallForm}))
  await page.route('**/api/Me', (route) =>
    route.fulfill({json: {...me, permissions: [5, 6, 40, 60]}}),
  )
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({
      json: [
        {
          id: 12,
          guid: 'g12',
          name: 'Fraud model',
          version: 14,
          active: true,
          locked: false,
          updatedUser: 'alex',
          updatedDate: '2026-10-21T09:12:00Z',
        },
      ],
    }),
  )
  await page.route('**/api/EntityApproval/Status**', (route) =>
    route.fulfill({
      json: {
        kind: 1,
        entityId: 12,
        name: 'Fraud model',
        currentVersion: 14,
        effectiveVersion: 14,
        deleted: false,
        pending: false,
        rejected: false,
        makerUser: null,
        approvalsRecorded: 0,
        approvalsRequired: 0,
        canApprove: false,
      },
    }),
  )
  await page.route('**/api/EntityApproval/Pending**', (route) => route.fulfill({json: []}))
  await page.route('**/api/EntityAnalysisModelIntegrity/12', (route) =>
    route.fulfill({
      json: {
        entityAnalysisModelId: 12,
        modelName: 'Fraud model',
        checkedDate: '2026-10-21T09:12:00Z',
        engineSource: 'x',
        errors: 0,
        warnings: 0,
        infos: 0,
        checks: [],
      },
    }),
  )
  await page.route('**/api/EntityAnalysisModelHttpAdaptation/ByEntityAnalysisModelId/*', (route) =>
    route.fulfill({json: []}),
  )
  await page.route(`**/api/${area}/ByEntityAnalysisModelId/*`, (route) =>
    route.fulfill({json: [existing]}),
  )
  await page.route(`**/api/${area}/Fields/*`, (route) => {
    captured.fieldsAsked.push(new URL(route.request().url()).pathname.split('/').pop()!)

    return route.fulfill({
      json: [
        {key: 'AccountId', label: 'AccountId (Report Table)', reportTable: true},
        {key: 'ToAccountId', label: 'ToAccountId (Report Table)', reportTable: true},
      ],
    })
  })
  await page.route('**/api/GraphRegistry', (route) =>
    route.fulfill({
      json: [
        {guid: 'r1', name: 'Payments graph'},
        {guid: 'r2', name: 'Cards graph'},
      ],
    }),
  )
  await page.route(`**/api/${area}`, (route) => {
    if (route.request().method() !== 'POST') return route.fulfill({json: [existing]})

    captured.posted.push(route.request().postData() ?? '')

    return route.fulfill({json: created})
  })
  await page.route(`**/api/${area}/3`, (route) => route.fulfill({json: existing}))
  await page.route(`**/api/${area}/7`, (route) => route.fulfill({json: created}))
}

async function openModel(page: Page) {
  await page.goto('/models')
  await page
    .locator('.tabulator-row', {hasText: 'Fraud model'})
    .getByTestId('grid-action-open')
    .click()
  await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'working')
}

test.describe('Graph Recall under a model', () => {
  test.use({viewport: {width: 1500, height: 1100}})

  let captured: Captured

  test.beforeEach(async ({page}) => {
    captured = {fieldsAsked: [], posted: []}
    await stub(page, captured)
  })

  test('the model offers a Network hub carrying the Graph Recall card', async ({page}) => {
    await openModel(page)

    const hubs = page.getByTestId('beat-model')

    await expect(hubs.getByText('Network', {exact: true})).toBeVisible()
    await expect(hubs.getByText('Machine Learning', {exact: true})).toBeVisible()
    await expect(page.getByTestId(`hub-${area}`)).toContainText('Graph Recall')
    await expect(page.getByTestId(`hub-total-${area}`)).toHaveText('1')
    await expect(page.getByTestId(`hub-${area}`)).toContainText('1 active')
  })

  test('opening the card lists the graph recall entities of the model', async ({page}) => {
    await openModel(page)
    await page.getByTestId(`hub-open-${area}`).click()

    await expect(page.getByTestId('receipt-model')).toContainText('Fraud model')
    await expect(page.getByTestId(`beat-${area}:12`)).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId(`beat-${area}:12`).locator('.tabulator-row')).toHaveCount(1)
    await expect(page.getByTestId(`beat-${area}:12`).locator('.tabulator-row')).toContainText(
      'AccountId',
    )
    await expect(page.getByTestId(`beat-${area}:12`).locator('.tabulator-row')).toContainText(
      'Payee',
    )
  })

  test('a new entity renders Identity, Entity and Output, with the field and registry lookups filled', async ({
                                                                                                                page,
                                                                                                              }) => {
    await openModel(page)
    await page.getByTestId(`hub-add-${area}`).click()

    const beat = page.getByTestId(`beat-${area}:12#new`)

    await expect(beat).toHaveAttribute('data-state', 'working')
    await expect(beat.getByTestId('group-Identity')).toBeVisible()
    await expect(beat.getByTestId('group-Entity')).toBeVisible()
    await expect(beat.getByTestId('group-Output')).toBeVisible()
    await expect(beat.getByTestId('field-name')).toHaveAttribute('placeholder', 'e.g. Payee')
    await expect(beat.getByTestId('field-active')).toBeChecked()
    await expect(beat.getByTestId('field-reportTable')).toBeChecked()
    await expect(beat.getByTestId('field-responsePayload')).not.toBeChecked()

    const requestXPathName = beat.getByTestId('field-requestXPathName')
    const nodeKey = beat.getByTestId('field-nodeKey')
    const registry = beat.getByTestId('field-graphRegistryGuid')

    await expect(requestXPathName).toBeEnabled()
    await expect(requestXPathName.locator('option')).toHaveText([
      'Choose',
      'AccountId (Report Table)',
      'ToAccountId (Report Table)',
    ])
    await expect(nodeKey.locator('option')).toHaveText([
      'Choose',
      'AccountId (Report Table)',
      'ToAccountId (Report Table)',
    ])
    await expect(registry.locator('option')).toHaveText(['Choose', 'Payments graph', 'Cards graph'])
    await expect(beat.getByTestId('field-counterpartName').locator('option')).toHaveText([
      'Choose',
      'AccountId',
    ])
    expect(captured.fieldsAsked.every((id) => id === '12')).toBe(true)
    expect(captured.fieldsAsked.length).toBeGreaterThan(0)
  })

  test('saving a new entity posts it under the model and opens the record', async ({page}) => {
    await openModel(page)
    await page.getByTestId(`hub-add-${area}`).click()

    const beat = page.getByTestId(`beat-${area}:12#new`)

    await beat.getByTestId('field-name').fill('Payee')
    await beat.getByTestId('field-requestXPathName').selectOption('ToAccountId')
    await beat.getByTestId('field-nodeKey').selectOption('AccountId')
    await beat.getByTestId('field-graphRegistryGuid').selectOption('r1')
    await page.getByTestId('crud-save').click()

    await expect.poll(() => captured.posted.length).toBe(1)

    const body = JSON.parse(captured.posted[0]!) as Record<string, unknown>

    expect(body.entityAnalysisModelId).toBe(12)
    expect(body.name).toBe('Payee')
    expect(body.requestXPathName).toBe('ToAccountId')
    expect(body.nodeKey).toBe('AccountId')
    expect(body.graphRegistryGuid).toBe('r1')
    expect(body.active).toBe(true)
    expect(body.reportTable).toBe(true)
    expect(body.responsePayload).toBe(false)

    await expect(page.getByTestId(`beat-${area}:12#record`)).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId(`beat-${area}:12#record`)).toContainText('Payee')
    await expect(page.getByTestId(`beat-${area}:12#new`)).toHaveCount(0)
  })
})
