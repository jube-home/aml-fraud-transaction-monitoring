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

export type HostEditor = 'rule' | 'case' | 'exhaustive' | 'datasource'

export interface HostOptions {
  editor: HostEditor
  parseType?: number
  values?: Record<string, unknown>
}

export interface Sent {
  method: string
  path: string
  body: Record<string, unknown>
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

const claimed = (name: string, editor: string, type = 'string', order = 50) =>
  field({name, label: name, type, order, editor})

const editorFields: Record<HostEditor, unknown[]> = {
  rule: [
    claimed('functionScript', 'ruleBuilder'),
    claimed('builderText', 'ruleBuilder', 'string', 51),
    claimed('builderJson', 'ruleBuilder', 'string', 52),
    claimed('scriptType', 'ruleBuilder', 'integer', 53),
  ],
  case: [claimed('selectJson', 'caseFilter'), claimed('filterJson', 'caseFilter', 'string', 51)],
  exhaustive: [
    claimed('filterJson', 'exhaustiveFilter'),
    claimed('filterSql', 'exhaustiveFilter', 'string', 51),
    claimed('filterTokens', 'exhaustiveFilter', 'string', 52),
    claimed('dataSourceId', 'exhaustiveFilter', 'integer', 53),
    claimed('csvId', 'exhaustiveFilter', 'integer', 54),
    claimed('jsonId', 'exhaustiveFilter', 'integer', 55),
  ],
  datasource: [
    claimed('command', 'datasource'),
    claimed('includeGrid', 'datasource', 'boolean', 51),
    claimed('displayKindId', 'datasource', 'integer', 52),
    claimed('templateText', 'datasource', 'string', 53),
    claimed('mapping', 'datasource', 'json', 54),
  ],
}

function editorsOf(options: HostOptions) {
  if (options.editor === 'rule') {
    return [
      {
        kind: 'ruleBuilder',
        parseType: options.parseType ?? 5,
        roles: {
          coder: 'functionScript',
          builder: 'builderText',
          builderJson: 'builderJson',
          scriptType: 'scriptType',
        },
      },
    ]
  }

  if (options.editor === 'case') {
    return [
      {
        kind: 'caseFilter',
        parseType: null,
        roles: {select: 'selectJson', filter: 'filterJson'},
      },
    ]
  }

  if (options.editor === 'exhaustive') {
    return [
      {
        kind: 'exhaustiveFilter',
        parseType: null,
        roles: {filterJson: 'filterJson', filterSql: 'filterSql', filterTokens: 'filterTokens'},
        reads: {source: 'dataSourceId', csv: 'csvId', json: 'jsonId'},
      },
    ]
  }

  return [
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
}

const record = (options: HostOptions) => ({
  id: 7,
  name: 'alpha',
  active: true,
  locked: false,
  entityAnalysisModelId: 1,
  functionScript: '',
  builderText: '',
  builderJson: '',
  scriptType: 1,
  selectJson: '',
  filterJson: '',
  filterSql: '',
  filterTokens: '',
  dataSourceId: 1,
  csvId: null,
  jsonId: null,
  command: '',
  includeGrid: false,
  displayKindId: 0,
  templateText: '',
  mapping: [],
  guid: '00000000-0000-0000-0000-000000000007',
  version: 3,
  ...options.values,
})

export async function stubEditorHost(page: Page, options: HostOptions) {
  const sent: Sent[] = []
  const model = {
    area: 'OpenTelemetryLogCounter',
    endpoint: '/api/OpenTelemetryLogCounter',
    operations: ['list', 'read', 'create', 'update', 'delete'],
    keys: {id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name'},
    approval: null,
    roles: null,
    values: null,
    editors: editorsOf(options),
    children: [],
    lockFields: ['locked'],
    groups: [
      {path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default'},
    ],
    fields: [
      field({name: 'name', label: 'Name', required: true, maxLength: 256}),
      field({name: 'id', label: 'Id', type: 'integer', order: 90, readOnly: true}),
      ...editorFields[options.editor],
    ],
    listColumns: [
      {field: 'name', title: 'Name', order: 10, sortable: true, filterable: true, hidden: false},
      {field: 'id', title: 'Id', order: 0, sortable: true, filterable: true, hidden: true},
    ],
  }

  await page.route('**/api/Me', (route) =>
    route.fulfill({
      json: {
        userName: 'alex',
        landlord: true,
        permissions: [],
        tenantRegistryId: 1,
        canChangePassword: true,
        askJoobyInference: true,
        sessionExpiry: null,
      },
    }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/TenantRegistry/ByFilter*', (route) =>
    route.fulfill({json: [{id: 1, name: 'Tenant one'}]}),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({json: {id: 1, name: 'Tenant one'}}),
  )
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({json: ['OpenTelemetryLogCounter']}),
  )
  await page.route('**/api/FormModel/OpenTelemetryLogCounter', (route) =>
    route.fulfill({json: model}),
  )
  await page.route('**/api/OpenTelemetryLogCounter**', (route) => {
    const request = route.request()
    const url = new URL(request.url())

    if (request.method() === 'GET') {
      return route.fulfill({
        json: url.pathname === '/api/OpenTelemetryLogCounter' ? [record(options)] : record(options),
      })
    }

    const body = JSON.parse(request.postData() ?? '{}') as Record<string, unknown>

    sent.push({method: request.method(), path: url.pathname, body})

    return route.fulfill({json: {...record(options), ...body}})
  })
  await page.route('**/api/EntityApproval/Status**', (route) =>
    route.fulfill({status: 404, body: ''}),
  )
  await page.route('**/api/Completions/**', (route) => route.fulfill({json: []}))
  await page.route('**/api/CaseWorkflowStatus/**', (route) => route.fulfill({json: []}))
  await page.route('**/api/RuleVocabulary*', (route) => route.fulfill({json: []}))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({json: []}))
  await page.route('**/api/Parser', (route) => route.fulfill({json: {message: 'Compiled'}}))

  return sent
}

export async function openEditorHost(page: Page) {
  await page.goto('/administration/telemetry')
  await expect(page.getByTestId('crud-panel')).toBeVisible()
  await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
}
