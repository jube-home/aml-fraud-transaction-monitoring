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
import { settleAnimations, setLanguage } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

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

const tagModel = {
  area: 'EntityAnalysisModelTag',
  endpoint: '/api/EntityAnalysisModelTag',
  operations: ['list', 'read', 'create', 'update', 'delete'],
  keys: { id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name' },
  approval: null,
  roles: null,
  values: null,
  editors: [],
  children: [],
  lockFields: ['locked'],
  groups: [
    { path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default' },
    { path: 'Audit', label: 'Audit', order: 90, collapsed: true, widget: 'default' },
  ],
  fields: [
    field({
      name: 'name',
      label: 'Name',
      description: 'Display name of this Tag. Unique within the Model.',
      required: true,
      maxLength: 256,
    }),
    field({ name: 'limit', label: 'Limit', type: 'integer', order: 15, min: 1, max: 50 }),
    field({ name: 'colour', label: 'Colour', order: 17, patterns: ['^#[0-9A-Fa-f]{6}$'] }),
    field({
      name: 'sample',
      label: 'Sample',
      type: 'number',
      order: 16,
      widget: 'slider',
      min: 0,
      max: 1,
    }),
    field({
      name: 'active',
      label: 'Active',
      type: 'boolean',
      widget: 'switch',
      order: 20,
      newDefault: false,
      nullable: false,
    }),
    field({
      name: 'locked',
      label: 'Locked',
      type: 'boolean',
      widget: 'switch',
      order: 30,
      newDefault: false,
      nullable: false,
    }),
    field({
      name: 'id',
      label: 'Id',
      type: 'integer',
      group: 'Audit',
      order: 10,
      readOnly: true,
      nullable: false,
    }),
    field({
      name: 'createdUser',
      label: 'Created user',
      group: 'Audit',
      order: 20,
      readOnly: true,
    }),
    field({
      name: 'createdDate',
      label: 'Created date',
      type: 'datetime',
      group: 'Audit',
      order: 30,
      readOnly: true,
      widget: 'date',
    }),
    field({
      name: 'version',
      label: 'Version',
      type: 'integer',
      group: 'Audit',
      order: 60,
      readOnly: true,
      nullable: false,
    }),
  ],
  listColumns: [
    { field: 'name', title: 'Name', order: 10, sortable: true, filterable: true, hidden: false },
    {
      field: 'active',
      title: 'Active',
      order: 20,
      sortable: true,
      filterable: true,
      hidden: false,
    },
    { field: 'id', title: 'Id', order: 0, sortable: true, filterable: true, hidden: true },
  ],
}

const choiceFields = [
  field({
    name: 'interval',
    label: 'Interval',
    type: 'string',
    order: 45,
    widget: 'radio',
    newDefault: 'h',
    options: [
      { value: 'n', label: 'Minutes' },
      { value: 'h', label: 'Hours' },
    ],
  }),
  field({
    name: 'kind',
    label: 'Kind',
    type: 'integer',
    order: 46,
    widget: 'select',
    options: [
      { value: 1, label: 'Count' },
      { value: 11, label: 'Mode' },
    ],
  }),
]

const ruleFields = [
  field({ name: 'functionScript', label: 'Function script', order: 50, editor: 'ruleBuilder' }),
]

const caseSqlFields = [field({ name: 'sql', label: 'SQL', order: 50, editor: 'caseSql' })]

const datasourceFields = [
  field({ name: 'command', label: 'Command', order: 50, editor: 'datasource' }),
  field({ name: 'includeGrid', label: 'Grid', type: 'boolean', order: 51, editor: 'datasource' }),
  field({
    name: 'displayKindId',
    label: 'Display',
    type: 'integer',
    order: 52,
    editor: 'datasource',
  }),
  field({ name: 'templateText', label: 'Template', order: 53, editor: 'datasource' }),
  field({ name: 'mapping', label: 'Mapping', type: 'json', order: 54, editor: 'datasource' }),
]

const caseFields = [
  field({ name: 'selectJson', label: 'Select', order: 50, editor: 'caseFilter' }),
  field({ name: 'filterJson', label: 'Filter', order: 51, editor: 'caseFilter' }),
]

const lookupField = field({
  name: 'ownerGuid',
  label: 'Owner',
  type: 'guid',
  order: 40,
  lookup: {
    source: '/api/RoleRegistry',
    textField: 'name',
    valueField: 'guid',
    parentField: null,
    allowEmpty: true,
  },
})

const parentLookupField = field({
  name: 'workflowGuid',
  label: 'Workflow',
  type: 'guid',
  order: 41,
  lookup: {
    source: '/api/Workflow/ByOwnerGuid',
    textField: 'name',
    valueField: 'guid',
    parentField: 'ownerGuid',
    allowEmpty: false,
  },
})

const record = (id: number, name: string, locked = false) => ({
  id,
  name,
  active: true,
  locked,
  entityAnalysisModelId: 1,
  functionScript: 'Return 1',
  command: 'select region, total from sales',
  sql: 'select 1 as n',
  includeGrid: false,
  displayKindId: 2,
  templateText: '',
  mapping: [],
  selectJson: '',
  filterJson: '',
  guid: `00000000-0000-0000-0000-00000000000${id}`,
  version: 3,
  createdUser: 'maker',
  createdDate: '2026-10-20T10:30:00+00:00',
})

interface Options {
  invalidSql?: boolean
  editor?: 'rule' | 'case' | 'datasource' | 'caseSql'
  relations?: boolean
  choices?: boolean
  lookup?: boolean
  colour?: boolean
  wide?: boolean
  lookupCalls?: string[]
  operations?: string[]
  modelStatus?: number
  saveStatus?: number
  saveBody?: unknown
}

async function stub(page: Page, calls: string[] = [], options: Options = {}) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/RoleRegistry', (route) =>
    route.fulfill({
      json: [
        { guid: 'aaaa', name: 'Analyst' },
        { guid: 'bbbb', name: 'Auditor' },
      ],
    }),
  )
  await page.route('**/api/Workflow/ByOwnerGuid/*', (route) => {
    const owner = new URL(route.request().url()).pathname.split('/').pop()!

    options.lookupCalls?.push(owner)

    return route.fulfill({
      json:
        owner === 'aaaa'
          ? [{ guid: 'w1', name: 'Alpha flow' }]
          : [{ guid: 'w2', name: 'Bravo flow' }],
    })
  })
  await page.route(
    '**/api/VisualisationRegistryParameter/ByVisualisationRegistryIdActiveOnly/1',
    (route) => route.fulfill({ json: [] }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/ChartKinds', (route) =>
    route.fulfill({
      json: [
        {
          kindId: 2,
          name: 'Bar',
          description: 'Compares values.',
          roles: [
            {
              roleId: 1,
              name: 'Category',
              description: 'Labels.',
              many: false,
              optional: false,
              acceptsId: 1,
            },
            {
              roleId: 2,
              name: 'Values',
              description: 'Heights.',
              many: true,
              optional: false,
              acceptsId: 2,
            },
          ],
        },
      ],
    }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/Introspect', (route) =>
    route.fulfill({
      json: {
        valid: !options.invalidSql,
        columns: [
          { name: 'region', ordinal: 1, dataTypeName: 'text', dataTypeId: 1 },
          { name: 'total', ordinal: 2, dataTypeName: 'numeric', dataTypeId: 3 },
        ],
        error: null,
        kinds: [{ kindId: 2, fitsByPosition: true, reason: null }],
        resolvedMapping: [],
        mappingError: null,
        templateError: null,
      },
    }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/Preview', (route) =>
    route.fulfill({
      json: {
        id: 0,
        guid: '00000000-0000-0000-0000-000000000000',
        name: '',
        columnSpan: 1,
        rowSpan: 1,
        priority: 0,
        stateId: 1,
        message: null,
        rowCount: 2,
        truncated: false,
        executionMilliseconds: 3,
        display: {
          kindId: 2,
          markupKindId: 1,
          markup:
            '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" role="img" aria-label="Totals"><rect width="10" height="10"/></svg>',
        },
        grid: null,
        style: { modeId: 1, version: 'v1', css: null },
      },
    }),
  )
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({ json: ['EntityAnalysisModelTag'] }),
  )
  await page.route('**/api/FormModel/EntityAnalysisModelTag', (route) =>
    options.modelStatus
      ? route.fulfill({ status: options.modelStatus, body: '' })
      : route.fulfill({
          json: {
            ...tagModel,
            operations: options.operations ?? tagModel.operations,
            ...(options.relations
              ? {
                  approval: {
                    kind: 17,
                    historyRoute: '/api/EntityAnalysisModelTagVersionHistory',
                  },
                  roles: {
                    endpoint: '/api/EntityAnalysisModelTagRole',
                    parentKey: 'entityAnalysisModelTagGuid',
                  },
                  values: {
                    area: 'TagValue',
                    endpoint: '/api/TagValue',
                    parentField: 'tagId',
                    parentKind: 17,
                    fields: [{ field: 'text', title: 'Text', type: 'string' }],
                  },
                }
              : {}),
            fields: [
              ...tagModel.fields.map((candidate) =>
                options.colour && candidate.name === 'colour'
                  ? { ...candidate, widget: 'colour' }
                  : options.wide && candidate.name === 'limit'
                    ? { ...candidate, span: 2 }
                    : candidate,
              ),
              ...(options.choices ? choiceFields : []),
              ...(options.lookup ? [lookupField, parentLookupField] : []),
              ...(options.editor === 'rule' ? ruleFields : []),
              ...(options.editor === 'case' ? caseFields : []),
              ...(options.editor === 'datasource' ? datasourceFields : []),
              ...(options.editor === 'caseSql' ? caseSqlFields : []),
            ],
            editors:
              options.editor === 'rule'
                ? [{ kind: 'ruleBuilder', parseType: 4, roles: { coder: 'functionScript' } }]
                : options.editor === 'caseSql'
                  ? [{ kind: 'caseSql', parseType: null, roles: { sql: 'sql' }, reads: {} }]
                  : options.editor === 'datasource'
                    ? [
                        {
                          kind: 'datasource',
                          parseType: null,
                          roles: {
                            command: 'command',
                            displayKind: 'displayKindId',
                            grid: 'includeGrid',
                            template: 'templateText',
                            mapping: 'mapping',
                          },
                          reads: {},
                        },
                      ]
                    : options.editor === 'case'
                      ? [
                          {
                            kind: 'caseFilter',
                            parseType: null,
                            roles: { select: 'selectJson', filter: 'filterJson' },
                          },
                        ]
                      : [],
          },
        }),
  )
  await page.route('**/api/EntityAnalysisModelTag**', (route) => {
    const request = route.request()
    const url = new URL(request.url())

    if (request.method() === 'GET' && url.pathname.endsWith('/ByEntityAnalysisModelId/1')) {
      return route.fulfill({ json: [record(7, 'alpha'), record(8, 'frozen', true)] })
    }

    if (request.method() === 'GET') {
      const id = Number(url.pathname.split('/').pop())

      return route.fulfill({ json: id === 8 ? record(8, 'frozen', true) : record(7, 'alpha') })
    }

    calls.push(`${request.method()} ${url.pathname} ${request.postData() ?? ''}`)

    if (options.saveStatus) {
      return route.fulfill({ status: options.saveStatus, json: options.saveBody ?? {} })
    }

    if (request.method() === 'POST') {
      return route.fulfill({
        json: { ...record(9, JSON.parse(request.postData()!).name), version: 1 },
      })
    }

    if (request.method() === 'PUT') return route.fulfill({ json: JSON.parse(request.postData()!) })

    return route.fulfill({ json: {} })
  })

  await page.route('**/api/EntityApproval/Status**', (route) =>
    options.relations
      ? route.fulfill({
          json: {
            kind: 17,
            entityId: 7,
            name: 'alpha',
            currentVersion: 3,
            effectiveVersion: 3,
            deleted: false,
            pending: false,
            rejected: false,
            makerUser: 'maker',
            approvalsRecorded: 1,
            approvalsRequired: 1,
            canApprove: false,
          },
        })
      : route.fulfill({ status: 404, body: '' }),
  )
  await page.route('**/api/EntityApproval/History**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/EntityAnalysisModelTagVersionHistory/**', (route) =>
    route.fulfill({ json: [] }),
  )
  await page.route('**/api/EntityAnalysisModelTagRole**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/TagValue**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/EntityApproval/ValueStatus', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/RuleVocabulary', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/Completions/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/CaseWorkflowStatus/**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/Parser', (route) => route.fulfill({ json: { message: 'Compiled' } }))
}

function changeEvent(overrides: Record<string, unknown>): string {
  const change = {
    area: 'EntityAnalysisModelTag',
    kind: 'Updated',
    entityId: 7,
    version: 4,
    actor: 'checker',
    at: '2026-10-21T09:00:00Z',
    correlationId: 'c1',
    ...overrides,
  }

  return `event: serviceChange\ndata: ${JSON.stringify(change)}\n\n`
}

async function serveEvent(page: Page, body: string) {
  let release: () => void = () => {}
  const released = new Promise<void>((resolve) => {
    release = resolve
  })

  await page.route('**/api/ServiceChange/Stream', async (route) => {
    await released

    return route.fulfill({ contentType: 'text/event-stream', body })
  })

  return release
}

async function serveRecord(page: Page, state: { name: string; version: number }) {
  await page.route('**/api/EntityAnalysisModelTag/7', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill({ json: { ...record(7, state.name), version: state.version } })
      : route.fallback(),
  )
}

async function open(page: Page) {
  await page.goto('/tools?tool=crud')
  await page.getByTestId('bench-load').click()
  await expect(page.getByTestId('crud-panel')).toBeVisible()
}

test.describe('CRUD form', () => {
  test.use({ viewport: { width: 1500, height: 1200 } })

  test('lists records by the declared list columns, hiding the hidden one', async ({ page }) => {
    await stub(page)
    await open(page)

    const headers = page.locator('.tabulator-col-title')

    await expect(page.locator('.tabulator-row')).toHaveCount(2)
    await expect(headers.filter({ hasText: 'Name' })).toHaveCount(1)
    await expect(headers.filter({ hasText: 'Active' })).toHaveCount(1)
    await expect(headers.filter({ hasText: /^Id$/ })).toHaveCount(0)
    await expect(page.locator('.tabulator-row').first()).toContainText('Yes')
  })

  test('draws the form from the model: groups, widgets, defaults, and a collapsed audit group', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('group-Identity')).toBeVisible()
    await expect(page.getByTestId('field-name')).toBeEditable()
    await expect(page.getByTestId('field-active')).not.toBeChecked()
    await expect(page.getByTestId('field-locked')).toBeDisabled()
    await expect(page.getByTestId('group-Audit')).not.toHaveAttribute('open', '')
    await expect(page.getByTestId('crud-save')).toBeDisabled()
  })

  test('creates a record with the parent key, then moves to editing the saved record', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('crud-new').click()
    await page.getByTestId('field-name').fill('bravo')
    await page.getByTestId('field-active').check()

    await expect(page.getByTestId('crud-dirty')).toBeVisible()

    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toMatch(/^POST \/api\/EntityAnalysisModelTag /)
    expect(calls[0]).toContain('"entityAnalysisModelId":1')
    expect(calls[0]).toContain('"name":"bravo"')
    expect(calls[0]).toContain('"active":true')
    expect(calls[0]).toContain('"locked":false')

    await expect(page.getByText('Edit EntityAnalysisModelTag')).toBeVisible()
    await expect(page.getByTestId('crud-dirty')).toHaveCount(0)
    await expect(page.getByTestId('crud-delete')).toBeVisible()
  })

  test('opens a record, carries its version on update, and shows audit fields read-only', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('field-name')).toHaveValue('alpha')
    await expect(page.getByTestId('crud-save')).toBeDisabled()

    await page.getByTestId('group-Audit').locator('summary').click()

    await expect(page.getByTestId('field-version')).toHaveText('3')
    await expect(page.getByTestId('field-createdDate')).toContainText('20 Oct 2026')

    await page.getByTestId('field-name').fill('alpha2')
    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toMatch(/^PUT /)
    expect(calls[0]).toContain('"id":7')
    expect(calls[0]).toContain('"version":3')
    expect(calls[0]).toContain('"name":"alpha2"')
  })

  test('shows server field errors against the field', async ({ page }) => {
    await stub(page, [], {
      saveStatus: 400,
      saveBody: { errors: [{ propertyName: 'Name', errorMessage: 'Name already exists.' }] },
    })
    await open(page)

    await page.getByTestId('crud-new').click()
    await page.getByTestId('field-name').fill('alpha')
    await page.getByTestId('crud-save').click()

    await expect(page.getByTestId('error-name')).toHaveText('Name already exists.')
    await expect(page.getByTestId('field-name')).toHaveClass(/input-error/)
    await expect(page.getByTestId('crud-problems')).toContainText('1 problem to fix')
    await expect(page.getByTestId('crud-problems')).toContainText('Name: Name already exists.')
  })

  test('reads the field out of an ASP.NET style error key and lists every problem', async ({
    page,
  }) => {
    await stub(page, [], {
      saveStatus: 400,
      saveBody: {
        errors: {
          '$.name': ['Name is bad.'],
          limit: ['Limit is too high.', 'Limit must be whole.'],
        },
      },
    })
    await open(page)

    await page.getByTestId('crud-new').click()
    await page.getByTestId('field-name').fill('alpha')
    await page.getByTestId('crud-save').click()

    await expect(page.getByTestId('crud-problems')).toContainText('3 problems to fix')
    await expect(page.getByTestId('error-name')).toHaveText('Name is bad.')
    await expect(page.getByTestId('error-limit')).toHaveCount(2)
    await expect(page.getByTestId('field-limit')).toHaveClass(/input-error/)
  })

  test('says which status refused a save that came back with no explanation', async ({ page }) => {
    await stub(page, [], { saveStatus: 400, saveBody: {} })
    await open(page)

    await page.getByTestId('crud-new').click()
    await page.getByTestId('field-name').fill('alpha')
    await page.getByTestId('crud-save').click()

    await expect(page.getByTestId('crud-reasons')).toContainText(
      'The request was refused. (HTTP 400)',
    )
  })

  test('hides save and delete for a locked record and disables its fields', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.locator('.tabulator-row').nth(1).getByTestId('grid-action-open').click()

    await expect(page.getByTestId('crud-locked')).toBeVisible()
    await expect(page.getByTestId('crud-save')).toHaveCount(0)
    await expect(page.getByTestId('crud-delete')).toHaveCount(0)
    await expect(page.getByTestId('field-name')).toBeDisabled()
  })

  test('asks before deleting and then deletes by id', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await page.getByTestId('crud-delete').click()
    await page.getByTestId('crud-confirm-no').click()

    expect(calls).toHaveLength(0)

    await page.getByTestId('crud-delete').click()
    await page.getByTestId('crud-confirm-yes').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toMatch(/^DELETE \/api\/EntityAnalysisModelTag\/7/)
    await expect(page.getByTestId('crud-form')).toHaveCount(0)
  })

  test('mounts approval, role allocation and values under an opened record, from the model alone', async ({
    page,
  }) => {
    await stub(page, [], { relations: true })
    await open(page)

    await expect(page.getByTestId('crud-roles')).toHaveCount(0)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('approval-panel')).toBeVisible()
    await expect(page.getByTestId('crud-roles')).toBeVisible()
    await expect(page.getByTestId('role-allocation')).toBeVisible()
    await expect(page.getByTestId('crud-values')).toBeVisible()
    await expect(page.getByTestId('value-grid')).toBeVisible()
  })

  test('draws a rule editor once in place of the property it edits, and saves what is typed', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls, { editor: 'rule' })
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('editor-ruleBuilder')).toBeVisible()
    await expect(page.getByTestId('schema-field-functionScript')).toHaveCount(0)
    await expect(page.getByTestId('rule-coder').locator('.cm-content')).toContainText('Return 1')
    await expect(page.getByTestId('crud-dirty')).toHaveCount(0)

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('Control+End')
    await page.keyboard.type(' OR True')

    await expect(page.getByTestId('crud-dirty')).toBeVisible()

    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"functionScript":"Return 1 OR True"')
  })

  test('draws the datasource builder once in place of the five properties it edits', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls, { editor: 'datasource' })
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('editor-datasource')).toBeVisible()
    await expect(page.getByTestId('datasource-builder')).toHaveCount(1)
    for (const name of ['command', 'includeGrid', 'displayKindId', 'templateText', 'mapping']) {
      await expect(page.getByTestId(`schema-field-${name}`)).toHaveCount(0)
    }

    await expect(page.getByTestId('datasource-status')).toContainText('Command is valid')
    await expect(page.getByTestId('datasource-sql').locator('.cm-content')).toContainText('sales')
    await expect(page.getByTestId('crud-dirty')).toHaveCount(0)

    await page.getByTestId('datasource-sql').locator('.cm-content').click()
    await page.keyboard.press('Control+End')
    await page.keyboard.type(' limit 5')

    await expect(page.getByTestId('crud-dirty')).toBeVisible()

    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"command":"select region, total from sales limit 5"')
    expect(calls[0]).toContain('"displayKindId":2')
    expect(calls[0]).toContain('"mapping":[]')
  })

  test('tries the case SQL against a case and shows the rows or why it was refused', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls, { editor: 'caseSql' })
    await page.route('**/api/CaseWorkflowDisplayExecution/TestSql', (route) => {
      const body = route.request().postDataJSON() as { sql: string; caseId: number }

      return route.fulfill({
        json:
          body.caseId === 7
            ? {
                ok: true,
                error: null,
                columns: [{ name: 'n', dataType: 'integer' }],
                rows: [{ n: 1 }],
                rowCount: 1,
                truncated: false,
                milliseconds: 4,
              }
            : {
                ok: false,
                error: 'The SQL was refused: nothing',
                columns: [],
                rows: [],
                rowCount: 0,
                truncated: false,
                milliseconds: 0,
              },
      })
    })
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('editor-caseSql')).toBeVisible()
    await expect(page.getByTestId('schema-field-sql')).toHaveCount(0)
    await expect(page.getByTestId('case-sql-run')).toBeDisabled()

    await page.getByTestId('case-sql-case-id').fill('9')
    await page.getByTestId('case-sql-run').click()
    await expect(page.getByTestId('case-sql-error')).toContainText('refused')

    await page.getByTestId('case-sql-case-id').fill('7')
    await page.getByTestId('case-sql-run').click()
    await expect(page.getByTestId('case-sql-summary')).toContainText('1 rows in 4 ms')
    await expect(page.getByTestId('case-sql-error')).toHaveCount(0)

    await page.getByTestId('case-sql-editor').locator('.cm-content').first().click()
    await page.keyboard.press('Control+End')
    await page.keyboard.type(' where true')
    await expect(page.getByTestId('crud-dirty')).toBeVisible()
  })

  test('holds Save back while the datasource editor is incomplete', async ({ page }) => {
    await stub(page, [], { editor: 'datasource', invalidSql: true })
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await expect(page.getByTestId('editor-datasource')).toBeVisible()
    await page.getByTestId('datasource-sql').locator('.cm-content').click()
    await page.keyboard.press('Control+End')
    await page.keyboard.type(' x')

    await expect(page.getByTestId('crud-dirty')).toBeVisible()
    await expect(page.getByTestId('crud-incomplete')).toBeVisible()
    await expect(page.getByTestId('crud-save')).toBeDisabled()
  })

  test('draws the case filter builder once in place of its two JSON properties', async ({
    page,
  }) => {
    await stub(page, [], { editor: 'case' })
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('editor-caseFilter')).toBeVisible()
    await expect(page.getByTestId('where-builder')).toBeVisible()
    await expect(page.getByTestId('schema-field-selectJson')).toHaveCount(0)
    await expect(page.getByTestId('schema-field-filterJson')).toHaveCount(0)
  })

  test('mounts none of them for an area that declares no relations', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('field-name')).toHaveValue('alpha')
    await expect(page.getByTestId('approval-panel')).toHaveCount(0)
    await expect(page.getByTestId('crud-roles')).toHaveCount(0)
    await expect(page.getByTestId('crud-values')).toHaveCount(0)
  })

  test('marks required fields and carries the validator limits to the inputs', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('field-name')).toHaveAttribute('maxlength', '256')
    await expect(page.getByTestId('field-name')).toHaveAttribute('aria-required', 'true')
    await expect(page.getByTestId('schema-field-name').getByText('Required')).toBeVisible()
    await expect(page.getByTestId('field-limit')).toHaveAttribute('min', '1')
    await expect(page.getByTestId('field-limit')).toHaveAttribute('max', '50')
    await expect(page.getByTestId('field-limit')).not.toHaveAttribute('aria-required', 'true')
    await expect(page.getByTestId('field-colour')).toHaveAttribute('pattern', '^#[0-9A-Fa-f]{6}$')
    await expect(page.getByTestId('field-name')).not.toHaveAttribute('pattern', /.*/)
  })

  test('draws radio and select choices from the model and stores the value, not the label', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls, { choices: true })
    await open(page)

    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('option-interval-h')).toBeChecked()
    await expect(page.getByTestId('option-interval-n')).not.toBeChecked()
    await expect(page.getByText('Hours')).toBeVisible()

    await page.getByTestId('option-interval-n').check()

    const kind = page.getByTestId('field-kind')

    await expect(kind.locator('option')).toHaveText(['Choose', 'Count', 'Mode'])

    await kind.selectOption('11')
    await page.getByTestId('field-name').fill('x')
    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"interval":"n"')
    expect(calls[0]).toContain('"kind":11')
  })

  test('draws a slider from the validator range and stores what is chosen', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('crud-new').click()

    const slider = page.getByTestId('field-sample')

    await expect(slider).toHaveAttribute('min', '0')
    await expect(slider).toHaveAttribute('max', '1')
    await expect(page.getByTestId('readout-sample')).toHaveText('0')

    await slider.fill('0.5')
    await page.getByTestId('field-name').fill('x')

    await expect(page.getByTestId('readout-sample')).toHaveText('0.5')

    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"sample":0.5')
  })

  test('fills a lookup from the route the model names and stores the chosen value', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls, { lookup: true })
    await open(page)

    await page.getByTestId('crud-new').click()

    const owner = page.getByTestId('field-ownerGuid')

    await expect(owner.locator('option')).toHaveText(['Choose', 'Analyst', 'Auditor'])

    await owner.selectOption('bbbb')
    await page.getByTestId('field-name').fill('x')
    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"ownerGuid":"bbbb"')
  })

  test('a lookup with a parent waits for it, asks the route for it, and clears when it changes', async ({
    page,
  }) => {
    const asked: string[] = []
    const calls: string[] = []

    await stub(page, calls, { lookup: true, lookupCalls: asked })
    await open(page)

    await page.getByTestId('crud-new').click()

    const owner = page.getByTestId('field-ownerGuid')
    const workflow = page.getByTestId('field-workflowGuid')

    await expect(workflow).toBeDisabled()
    expect(asked).toEqual([])

    await owner.selectOption('aaaa')

    await expect(workflow).toBeEnabled()
    await expect(workflow.locator('option')).toHaveText(['Choose', 'Alpha flow'])
    await workflow.selectOption('w1')
    await expect(workflow).toHaveValue('w1')

    await owner.selectOption('bbbb')

    await expect(workflow.locator('option')).toHaveText(['Choose', 'Bravo flow'])
    await expect(workflow).toHaveValue('')
    expect(asked).toEqual(['aaaa', 'bbbb'])

    await page.getByTestId('field-name').fill('x')
    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"ownerGuid":"bbbb"')
    expect(calls[0]).toContain('"workflowGuid":null')
  })

  test('explains a field from its description in a popover opened by the info button', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('crud-new').click()

    const schemaName = page.getByTestId('schema-field-name')

    await expect(schemaName.getByTestId('field-help-text')).toBeHidden()

    await schemaName.getByTestId('field-help').click()

    await expect(schemaName.getByTestId('field-help-text')).toBeVisible()
    await expect(schemaName.getByTestId('field-help-text')).toContainText(
      'Display name of this Tag. Unique within the Model.',
    )
    await expect(page.getByTestId('schema-field-limit').getByTestId('field-help')).toHaveCount(0)
  })

  test('draws a colour field as a picker beside its hex text and keeps them in step', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls, { colour: true })
    await open(page)

    await page.getByTestId('crud-new').click()
    await page.getByTestId('swatch-colour').fill('#336699')

    await expect(page.getByTestId('field-colour')).toHaveValue('#336699')

    await page.getByTestId('field-colour').fill('#abcdef')

    await expect(page.getByTestId('swatch-colour')).toHaveValue('#abcdef')

    await page.getByTestId('field-name').fill('x')
    await page.getByTestId('crud-save').click()

    await expect.poll(() => calls.length).toBe(1)
    expect(calls[0]).toContain('"colour":"#abcdef"')
  })

  test('another user changing the open record reloads it and shows what changed, in blue', async ({
    page,
  }) => {
    await stub(page)

    const release = await serveEvent(page, changeEvent({}))
    const server = { name: 'alpha', version: 3 }

    await serveRecord(page, server)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await expect(page.getByTestId('field-name')).toHaveValue('alpha')

    server.name = 'alpha by checker'
    server.version = 4
    release()

    await expect(page.getByTestId('crud-external')).toContainText('checker updated this record')
    await expect(page.getByTestId('crud-external')).toContainText('Version 3 to 4')
    await expect(page.getByTestId('crud-external')).toContainText('2 fields changed')
    await expect(page.getByTestId('field-name')).toHaveValue('alpha by checker')
    await expect(page.getByTestId('field-name')).toHaveClass(/input-info/)
    await expect(page.getByTestId('schema-field-name')).toContainText('Changed')
    await expect(page.getByTestId('crud-save')).toBeDisabled()

    await page.getByTestId('crud-external-dismiss').click()

    await expect(page.getByTestId('crud-external')).toHaveCount(0)
  })

  test('a change by someone else while there are unsaved edits asks before touching them', async ({
    page,
  }) => {
    await stub(page)

    const release = await serveEvent(page, changeEvent({}))
    const server = { name: 'alpha', version: 3 }

    await serveRecord(page, server)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await page.getByTestId('field-name').fill('my edit')

    server.name = 'alpha by checker'
    server.version = 4
    release()

    await expect(page.getByTestId('crud-external')).toContainText(
      'checker changed this record while you have unsaved edits',
    )
    await expect(page.getByTestId('field-name')).toHaveValue('my edit')

    await page.getByTestId('crud-external-keep').click()

    await expect(page.getByTestId('crud-external')).toHaveCount(0)
    await expect(page.getByTestId('field-name')).toHaveValue('my edit')
  })

  test('reloading after someone else changed the record drops the edits and takes theirs', async ({
    page,
  }) => {
    await stub(page)

    const release = await serveEvent(page, changeEvent({}))
    const server = { name: 'alpha', version: 3 }

    await serveRecord(page, server)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await page.getByTestId('field-name').fill('my edit')

    server.name = 'alpha by checker'
    server.version = 4
    release()

    await page.getByTestId('crud-external-reload').click()

    await expect(page.getByTestId('field-name')).toHaveValue('alpha by checker')
    await expect(page.getByTestId('crud-dirty')).toHaveCount(0)
    await expect(page.getByTestId('field-name')).toHaveClass(/input-info/)
  })

  test('says so when someone else deleted the open record, and closes it on request', async ({
    page,
  }) => {
    await stub(page)

    const release = await serveEvent(page, changeEvent({ kind: 'Deleted', version: null }))

    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await expect(page.getByTestId('crud-form')).toBeVisible()

    release()

    await expect(page.getByTestId('crud-external')).toContainText('checker deleted this record')

    await page.getByTestId('crud-external-close').click()

    await expect(page.getByTestId('crud-form')).toHaveCount(0)
  })

  test('says nothing about a change the signed-in user made themselves', async ({ page }) => {
    await stub(page)

    const release = await serveEvent(page, changeEvent({ actor: 'alex' }))
    const server = { name: 'alpha', version: 3 }

    await serveRecord(page, server)
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await expect(page.getByTestId('field-name')).toHaveValue('alpha')

    server.name = 'alpha by me elsewhere'
    server.version = 4
    release()

    await page.waitForTimeout(500)

    await expect(page.getByTestId('crud-external')).toHaveCount(0)
    await expect(page.getByTestId('field-name')).toHaveValue('alpha')
  })

  test('a save the server refuses because the version moved says so and offers the reload', async ({
    page,
  }) => {
    await stub(page, [], { saveStatus: 409, saveBody: { code: 'VersionConflict' } })
    await open(page)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await page.getByTestId('field-name').fill('my edit')
    await page.getByTestId('crud-save').click()

    await expect(page.getByTestId('crud-external')).toContainText('your save was refused')
    await expect(page.getByTestId('crud-external-reload')).toBeVisible()
    await expect(page.getByTestId('field-name')).toHaveValue('my edit')
  })

  test('fields sit two to a row, and a field that asks for two columns takes its own row', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('crud-new').click()

    const before = await page.getByTestId('schema-field-name').boundingBox()
    const beside = await page.getByTestId('schema-field-limit').boundingBox()

    expect(Math.abs(before!.y - beside!.y)).toBeLessThan(4)

    await page.getByTestId('crud-close').click()

    await stub(page, [], { wide: true })
    await page.reload()
    await open(page)
    await page.getByTestId('crud-new').click()

    const name = await page.getByTestId('schema-field-name').boundingBox()
    const limit = await page.getByTestId('schema-field-limit').boundingBox()

    expect(limit!.y).toBeGreaterThan(name!.y + name!.height - 4)
    expect(limit!.width).toBeGreaterThan(name!.width * 1.8)
  })

  test('hides what the model says the area cannot do', async ({ page }) => {
    await stub(page, [], { operations: ['list', 'read'] })
    await open(page)

    await expect(page.getByTestId('crud-new')).toHaveCount(0)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('field-name')).toBeDisabled()
    await expect(page.getByTestId('crud-save')).toHaveCount(0)
    await expect(page.getByTestId('crud-delete')).toHaveCount(0)
  })

  test('shows no list for an area that cannot be listed', async ({ page }) => {
    await stub(page, [], { operations: ['create'] })
    await open(page)

    await expect(page.getByTestId('crud-no-list')).toBeVisible()
    await expect(page.getByTestId('crud-new')).toBeVisible()
  })

  test('says so when there is no form model for the area', async ({ page }) => {
    await stub(page, [], { modelStatus: 404 })
    await open(page)

    await expect(page.getByTestId('crud-model-error')).toBeVisible()
  })

  test('asks for the model in the chosen language and shows the labels the server returns', async ({
    page,
  }) => {
    const languages: string[] = []

    await stub(page)
    await page.route('**/api/FormModel/EntityAnalysisModelTag', (route) => {
      const language = route.request().headers()['accept-language'] ?? ''
      const polish = language === 'pl'

      languages.push(language)

      return route.fulfill({
        json: {
          ...tagModel,
          fields: tagModel.fields.map((candidate) =>
            candidate.name === 'name' && polish ? { ...candidate, label: 'Nazwa' } : candidate,
          ),
          listColumns: tagModel.listColumns.map((column) =>
            column.field === 'name' && polish ? { ...column, title: 'Nazwa' } : column,
          ),
        },
      })
    })
    await open(page)

    await expect(page.locator('.tabulator-col-title', { hasText: 'Name' })).toBeVisible()
    expect(languages).toEqual(['en'])

    await page.getByRole('button', { name: /^Account/ }).click()
    await setLanguage(page, 'pl')

    await expect.poll(() => languages.at(-1)).toBe('pl')
    await expect(page.locator('.tabulator-col-title', { hasText: 'Nazwa' })).toBeVisible()
  })

  test('has no accessibility violations with the form open', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
    await expect(page.getByTestId('field-name')).toHaveValue('alpha')
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
