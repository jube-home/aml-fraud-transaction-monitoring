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
import { expect, test, type Page } from '@playwright/test'
import { instance, stubExhaustive } from './exhaustiveStubs'

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

const model = {
  area: 'ExhaustiveSearchInstance',
  endpoint: '/api/ExhaustiveSearchInstance',
  operations: ['list', 'read', 'create', 'update', 'delete'],
  keys: { id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name' },
  approval: null,
  roles: null,
  values: null,
  editors: [],
  children: [],
  lockFields: ['locked'],
  groups: [{ path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default' }],
  fields: [field({ name: 'name', label: 'Name', required: true })],
  listColumns: [
    { field: 'name', title: 'Name', order: 10, sortable: true, filterable: true, hidden: false },
  ],
}

async function stub(page: Page, statusId: number) {
  await stubExhaustive(page, { instanceStatus: statusId })
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({ json: ['ExhaustiveSearchInstance'] }),
  )
  await page.route('**/api/FormModel/ExhaustiveSearchInstance', (route) =>
    route.fulfill({ json: model }),
  )
  await page.route('**/api/ExhaustiveSearchInstance/ByEntityAnalysisModelId/1', (route) =>
    route.fulfill({ json: [instance({ statusId })] }),
  )
}

async function openRecord(page: Page) {
  await page.goto('/tools?tool=crud&area=ExhaustiveSearchInstance&parent=1')
  await expect(page.getByTestId('crud-panel')).toBeVisible()
  await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
  await expect(page.getByTestId('crud-extra')).toBeVisible()
}

test.describe('Exhaustive adaptation record', () => {
  test.use({ viewport: { width: 1500, height: 1200 } })

  test('shows the results below the form and folds a trained configuration to a summary', async ({
    page,
  }) => {
    await stub(page, 16)
    await openRecord(page)

    await expect(page.getByTestId('crud-form-fold')).toBeVisible()
    await expect(page.getByTestId('crud-form-summary')).toContainText('Archive data')
    await expect(page.getByTestId('crud-form-summary')).toContainText('class definition on')
    await expect(page.getByTestId('field-name')).toBeHidden()
    await expect(page.getByTestId('exhaustive-status')).toContainText('Finished')

    await page.getByTestId('crud-form-fold').locator('summary').click()

    await expect(page.getByTestId('field-name')).toBeVisible()
  })

  test('leaves the configuration open while the training is still early', async ({ page }) => {
    await stub(page, 9)
    await openRecord(page)

    await expect(page.getByTestId('crud-form-fold')).toHaveCount(0)
    await expect(page.getByTestId('field-name')).toBeVisible()
    await expect(page.getByTestId('exhaustive-waiting')).toBeVisible()
  })

  test('draws no results for a record that has not been created', async ({ page }) => {
    await stub(page, 16)
    await page.goto('/tools?tool=crud&area=ExhaustiveSearchInstance&parent=1')
    await expect(page.getByTestId('crud-panel')).toBeVisible()
    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('field-name')).toBeVisible()
    await expect(page.getByTestId('crud-extra')).toHaveCount(0)
  })
})

const condition = (property: string, value: unknown) => ({
  property,
  op: 'equals',
  values: [value],
  matchMode: 'all',
})

const richModel = {
  ...model,
  groups: [
    { path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default' },
    {
      path: 'Training Data',
      label: 'Training Data',
      order: 15,
      collapsed: false,
      widget: 'default',
    },
    {
      path: 'Training Data Filter',
      label: 'Training Data Filter',
      order: 30,
      collapsed: false,
      widget: 'default',
    },
  ],
  editors: [
    {
      kind: 'exhaustiveDataset',
      parseType: null,
      roles: { csv: 'exhaustiveInstanceCsvId', json: 'exhaustiveInstanceJsonId' },
      reads: { source: 'dataSourceId', status: 'statusId' },
    },
    {
      kind: 'exhaustiveFilter',
      parseType: null,
      roles: { filterJson: 'filterJson', filterSql: 'filterSql', filterTokens: 'filterTokens' },
      reads: {
        source: 'dataSourceId',
        csv: 'exhaustiveInstanceCsvId',
        json: 'exhaustiveInstanceJsonId',
      },
    },
  ],
  fields: [
    field({ name: 'name', label: 'Name', required: true }),
    field({
      name: 'dataSourceId',
      label: 'Data source',
      type: 'integer',
      group: 'Training Data',
      order: 10,
      widget: 'select',
      options: [
        { value: 0, label: 'Archive' },
        { value: 2, label: 'Uploaded dataset' },
      ],
    }),
    field({
      name: 'exhaustiveInstanceCsvId',
      label: 'Csv dataset',
      type: 'integer',
      group: 'Training Data',
      order: 20,
      editor: 'exhaustiveDataset',
    }),
    field({
      name: 'exhaustiveInstanceJsonId',
      label: 'Json dataset',
      type: 'integer',
      group: 'Training Data',
      order: 25,
      editor: 'exhaustiveDataset',
    }),
    field({
      name: 'filter',
      label: 'Class definition',
      type: 'boolean',
      widget: 'switch',
      group: 'Training Data Filter',
      order: 10,
      nullable: false,
    }),
    ...['filterJson', 'filterSql', 'filterTokens'].map((name, index) =>
      field({
        name,
        label: name,
        group: 'Training Data Filter',
        order: 20 + index,
        editor: 'exhaustiveFilter',
        visibleWhen: [condition('filter', true)],
      }),
    ),
    field({
      name: 'statusId',
      label: 'Status',
      type: 'integer',
      group: 'Training Data',
      order: 90,
      readOnly: true,
    }),
  ],
}

const completion = {
  name: 'Payload.Amount',
  value: 'Payload.Amount',
  meta: 'Amount:double',
  score: 1000,
  dataType: 'double',
  group: 'Payload',
  field: `"Json" ->> 'Amount'`,
}

interface EditorSeen {
  puts: Record<string, unknown>[]
  completions: string[]
}

async function stubEditors(page: Page, values: Record<string, unknown>): Promise<EditorSeen> {
  const seen: EditorSeen = { puts: [], completions: [] }

  await stubExhaustive(page, { instanceStatus: Number(values.statusId ?? 0) })
  await page.route('**/api/FormModel', (route) =>
    route.fulfill({ json: ['ExhaustiveSearchInstance'] }),
  )
  await page.route('**/api/FormModel/ExhaustiveSearchInstance', (route) =>
    route.fulfill({ json: richModel }),
  )
  await page.route('**/api/ExhaustiveSearchInstance/ByEntityAnalysisModelId/1', (route) =>
    route.fulfill({ json: [instance(values)] }),
  )
  await page.route('**/api/ExhaustiveSearchInstance/1', (route) =>
    route.fulfill({ json: { ...instance(values), ...values } }),
  )
  await page.route('**/api/ExhaustiveSearchInstance', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback()

    seen.puts.push(route.request().postDataJSON())

    return route.fulfill({ json: route.request().postDataJSON() })
  })
  await page.route('**/api/Completions/**', (route) => {
    const url = new URL(route.request().url())

    seen.completions.push(url.pathname + url.search)

    return route.fulfill({ json: [completion] })
  })
  await page.route('**/api/ExhaustiveInstanceDataset', (route) =>
    route.fulfill({ json: [{ id: 3, name: 'Labelled sample', kind: 'Csv', statusId: 1 }] }),
  )
  await page.route('**/api/ExhaustiveInstanceCsv/3', (route) =>
    route.fulfill({
      json: {
        id: 3,
        name: 'Labelled sample',
        kind: 'Csv',
        fileName: 'labels.csv',
        rowCount: 10,
        sizeBytes: 100,
        columnCount: 1,
        statusId: 1,
        delimiter: ',',
        warnings: [],
        columns: [],
      },
    }),
  )

  return seen
}

test.describe('Exhaustive adaptation editors in the form', () => {
  test.use({ viewport: { width: 1500, height: 1400 } })

  test('offers the dataset picker only for the uploaded data source, in place of the two id fields', async ({
    page,
  }) => {
    await stubEditors(page, { statusId: 0, dataSourceId: 0, filter: false })
    await openRecord(page)

    await expect(page.getByTestId('editor-exhaustiveDataset')).toHaveCount(0)
    await expect(page.getByTestId('field-exhaustiveInstanceCsvId')).toHaveCount(0)
    await expect(page.getByTestId('field-exhaustiveInstanceJsonId')).toHaveCount(0)
  })

  test('writes the chosen dataset and the uploaded source back to the record', async ({ page }) => {
    const seen = await stubEditors(page, { statusId: 0, dataSourceId: 2, filter: false })
    await openRecord(page)

    await expect(page.getByTestId('editor-exhaustiveDataset')).toBeVisible()
    await expect(page.getByTestId('crud-dirty')).toHaveCount(0)

    await page.getByTestId('dataset-picker').selectOption('Csv:3')
    await expect(page.getByTestId('crud-dirty')).toBeVisible()
    await page.getByTestId('crud-save').click()

    await expect.poll(() => seen.puts.length).toBe(1)
    expect(seen.puts[0]).toMatchObject({ exhaustiveInstanceCsvId: 3, dataSourceId: 2 })
    expect(seen.puts[0]!.exhaustiveInstanceJsonId ?? null).toBeNull()
  })

  test('freezes the dataset picker once training has begun', async ({ page }) => {
    await stubEditors(page, {
      statusId: 9,
      dataSourceId: 2,
      exhaustiveInstanceCsvId: 3,
      filter: false,
    })
    await openRecord(page)

    await expect(page.getByTestId('dataset-picker')).toBeDisabled()
    await expect(page.getByTestId('dataset-new')).toBeDisabled()
  })

  test('draws the class definition builder from the model fields without marking the record changed', async ({
    page,
  }) => {
    const seen = await stubEditors(page, { statusId: 0, dataSourceId: 0, filter: true })
    await openRecord(page)

    await expect(page.getByTestId('exhaustive-filter-builder')).toBeVisible()
    await expect(page.getByTestId('where-builder')).toBeVisible()
    expect(seen.completions).toEqual([
      '/api/Completions/ByEntityAnalysisModelId?entityAnalysisModelId=1',
    ])
    await expect(page.getByTestId('field-filterJson')).toHaveCount(0)
    await expect(page.getByTestId('field-filterSql')).toHaveCount(0)
  })

  test('draws the class definition builder from the uploaded dataset when one is chosen', async ({
    page,
  }) => {
    const seen = await stubEditors(page, {
      statusId: 0,
      dataSourceId: 2,
      exhaustiveInstanceCsvId: 3,
      filter: true,
    })
    await openRecord(page)

    await expect(page.getByTestId('exhaustive-filter-builder')).toBeVisible()
    expect(seen.completions).toEqual(['/api/Completions/ByExhaustiveInstanceDataset?kind=Csv&id=3'])
  })
})
