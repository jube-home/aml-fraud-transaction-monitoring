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

const column = (overrides: Record<string, unknown> = {}) => ({
  index: 0,
  header: 'Amount',
  kind: 'Numeric',
  numericCount: 10,
  textCount: 0,
  missingCount: 1,
  min: 1,
  max: 900,
  mean: 100,
  isCategorical: false,
  include: true,
  name: 'Amount',
  processingTypeId: 1,
  distinctValues: [],
  ...overrides,
})

const dataset = (overrides: Record<string, unknown> = {}) => ({
  id: 9,
  guid: 'g-9',
  name: 'Fraud labels',
  kind: 'Csv',
  fileName: 'labels.csv',
  rowCount: 1200,
  sizeBytes: 34000,
  columnCount: 3,
  statusId: 0,
  delimiter: ',',
  warnings: ['Column Notes looks like free text.'],
  columns: [
    column(),
    column({
      index: 1,
      header: 'Country',
      kind: 'Text',
      isCategorical: true,
      name: 'Country',
      distinctValues: [
        { value: 'GB', count: 700 },
        { value: 'PL', count: 500 },
      ],
    }),
    column({
      index: 2,
      header: 'Notes',
      kind: 'Text',
      isCategorical: false,
      include: false,
      name: 'Notes',
    }),
  ],
  ...overrides,
})

interface Seen {
  uploads: { path: string; body: string }[]
  confirms: Record<string, unknown>[]
  deletes: string[]
}

async function stub(
  page: Page,
  options: {
    permissions?: number[]
    uploadStatus?: number
    confirmStatus?: number
    deleteStatus?: number
  } = {},
) {
  const seen: Seen = { uploads: [], confirms: [], deletes: [] }
  let confirmed = [{ id: 3, name: 'Existing', kind: 'Csv', statusId: 1 }]

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [16] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/ExhaustiveInstanceDataset', (route) =>
    route.fulfill({
      json: [...confirmed, { id: 8, name: 'Draft', kind: 'Csv', statusId: 0 }],
    }),
  )
  await page.route('**/api/ExhaustiveInstanceCsv/3', (route) => {
    if (route.request().method() === 'DELETE') {
      seen.deletes.push('Csv/3')

      return route.fulfill({ status: options.deleteStatus ?? 200, json: {} })
    }

    return route.fulfill({
      json: dataset({ id: 3, name: 'Existing', statusId: 1, warnings: [] }),
    })
  })
  await page.route('**/api/ExhaustiveInstanceCsv/9', (route) => {
    seen.deletes.push('Csv/9')

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/ExhaustiveInstance*/Upload', (route) => {
    seen.uploads.push({
      path: new URL(route.request().url()).pathname,
      body: route.request().postData() ?? '',
    })

    return options.uploadStatus && options.uploadStatus !== 200
      ? route.fulfill({ status: options.uploadStatus, json: {} })
      : route.fulfill({
          json: dataset(route.request().url().includes('Json') ? { kind: 'Json', id: 12 } : {}),
        })
  })
  await page.route('**/api/ExhaustiveInstanceCsv/Confirm', (route) => {
    seen.confirms.push(route.request().postDataJSON())

    if (options.confirmStatus && options.confirmStatus !== 200) {
      return route.fulfill({
        status: options.confirmStatus,
        json: { errors: [{ propertyName: 'Columns', errorMessage: 'Too many variables.' }] },
      })
    }

    confirmed = [...confirmed, { id: 9, name: 'Fraud labels', kind: 'Csv', statusId: 1 }]

    return route.fulfill({ json: dataset({ statusId: 1 }) })
  })

  return seen
}

const csv = (name = 'labels.csv', text = 'a,b\n1,2\n') => ({
  name,
  mimeType: 'text/csv',
  buffer: Buffer.from(text),
})

async function open(page: Page) {
  await page.goto('/tools?tool=dataset-upload')
  await expect(page.getByTestId('dataset-upload')).toBeVisible()
  await expect(page.getByTestId('dataset-picker')).toBeEnabled()
}

async function upload(page: Page, file = csv()) {
  await page.getByTestId('dataset-new').click()
  await page.getByTestId('file-upload-input').setInputFiles(file)
  await page.getByTestId('file-upload-send').click()
  await expect(page.getByTestId('dataset-inspection')).toBeVisible()
}

test.describe('Dataset upload', () => {
  test.use({ viewport: { width: 1500, height: 1300 } })

  test('offers only confirmed datasets and shows the chosen one read only', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('dataset-picker').locator('option')).toHaveText([
      'Select a dataset',
      'Existing (Csv)',
    ])

    await page.getByTestId('dataset-picker').selectOption('Csv:3')

    await expect(page.getByTestId('dataset-bench-value')).toHaveText('Csv:3')
    await expect(page.getByTestId('dataset-summary')).toContainText('1,200 rows')
    await expect(page.getByTestId('dataset-confirm')).toHaveCount(0)
    await expect(page.getByTestId('dataset-include-0')).toBeDisabled()
  })

  test('uploads a file, lets the mapping be edited and confirms it', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await upload(page)

    expect(seen.uploads[0]!.path).toBe('/api/ExhaustiveInstanceCsv/Upload')
    expect(seen.uploads[0]!.body).toContain('filename="labels.csv"')
    await expect(page.getByTestId('dataset-warnings')).toContainText('free text')

    await page.getByTestId('grid-input-name').first().fill('Amount_gbp')
    await page.getByTestId('dataset-include-1').uncheck()
    await page.getByTestId('dataset-processing-0').selectOption('5')
    await page.getByTestId('dataset-confirm').click()

    await expect(page.getByTestId('dataset-bench-value')).toHaveText('Csv:9')
    expect(seen.confirms).toEqual([
      {
        id: 9,
        columns: [
          { index: 0, include: true, name: 'Amount_gbp', processingTypeId: 5 },
          { index: 1, include: false, name: 'Country', processingTypeId: 1 },
          { index: 2, include: false, name: 'Notes', processingTypeId: 1 },
        ],
      },
    ])
    await expect(page.getByTestId('dataset-picker').locator('option')).toHaveCount(3)
  })

  test('sends a JSON file to the JSON store', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await upload(page, csv('rows.json', '[{"a":1}]'))

    expect(seen.uploads[0]!.path).toBe('/api/ExhaustiveInstanceJson/Upload')
  })

  test('only lets a column that can be a variable be included', async ({ page }) => {
    await stub(page)
    await open(page)
    await upload(page)

    await expect(page.getByTestId('dataset-include-0')).toBeEnabled()
    await expect(page.getByTestId('dataset-include-1')).toBeEnabled()
    await expect(page.getByTestId('dataset-include-2')).toBeDisabled()
  })

  test('names a bad mapping without calling the server', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await upload(page)

    await page.getByTestId('grid-input-name').first().fill('1 bad name')
    await page.getByTestId('dataset-confirm').click()
    await expect(page.getByTestId('dataset-problems')).toContainText('Amount: the variable name')

    await page.getByTestId('grid-input-name').first().fill('Country')
    await page.getByTestId('dataset-confirm').click()
    await expect(page.getByTestId('dataset-problems')).toContainText('Country: another included')

    await page.getByTestId('dataset-include-0').uncheck()
    await page.getByTestId('dataset-include-1').uncheck()
    await page.getByTestId('dataset-confirm').click()
    await expect(page.getByTestId('dataset-problems')).toContainText('at least one column')

    expect(seen.confirms).toHaveLength(0)
  })

  test('shows the reasons the server refuses a mapping with', async ({ page }) => {
    await stub(page, { confirmStatus: 400 })
    await open(page)
    await upload(page)

    await page.getByTestId('dataset-confirm').click()

    await expect(page.getByTestId('dataset-problems')).toContainText('Too many variables.')
    await expect(page.getByTestId('dataset-confirm')).toBeEnabled()
  })

  test('discards the draft on cancel', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await upload(page)

    await page.getByTestId('dataset-cancel').click()

    await expect(page.getByTestId('dataset-inspection')).toHaveCount(0)
    expect(seen.deletes).toEqual(['Csv/9'])
  })

  test('says when a file is too large', async ({ page }) => {
    await stub(page, { uploadStatus: 413 })
    await open(page)

    await page.getByTestId('dataset-new').click()
    await page.getByTestId('file-upload-input').setInputFiles(csv())
    await page.getByTestId('file-upload-send').click()

    await expect(page.getByTestId('dataset-problems')).toContainText(
      'larger than the permitted size',
    )
  })

  test('deletes the chosen dataset after confirmation', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('dataset-picker').selectOption('Csv:3')
    await expect(page.getByTestId('dataset-summary')).toBeVisible()
    await page.getByTestId('dataset-delete').click()
    await page.getByTestId('dataset-delete-yes').click()

    await expect(page.getByTestId('dataset-bench-value')).toHaveText('–')
    expect(seen.deletes).toEqual(['Csv/3'])
  })

  test('says when the dataset is still in use', async ({ page }) => {
    await stub(page, { deleteStatus: 409 })
    await open(page)

    await page.getByTestId('dataset-picker').selectOption('Csv:3')
    await page.getByTestId('dataset-delete').click()
    await page.getByTestId('dataset-delete-yes').click()

    await expect(page.getByTestId('dataset-problems')).toContainText('still in use')
    await expect(page.getByTestId('dataset-bench-value')).toHaveText('Csv:3')
  })

  test('shows the distinct values of a column when its row is opened', async ({ page }) => {
    await stub(page)
    await open(page)
    await upload(page)

    await page.getByRole('button', { name: 'Show the distinct values' }).nth(1).click()

    await expect(page.getByText('GB')).toBeVisible()
    await expect(page.getByText('700')).toBeVisible()
  })

  test('freezes the controls while the adaptation is locked', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('dataset-bench-locked').check()

    await expect(page.getByTestId('dataset-picker')).toBeDisabled()
    await expect(page.getByTestId('dataset-new')).toBeDisabled()
    await expect(page.getByTestId('dataset-delete')).toBeDisabled()
  })

  test('offers nothing without the permission', async ({ page }) => {
    await stub(page, { permissions: [] })
    await page.goto('/tools?tool=dataset-upload')

    await expect(page.getByTestId('dataset-refused')).toBeVisible()
    await expect(page.getByTestId('dataset-picker')).toHaveCount(0)
  })

  test('has no accessibility violations while a mapping is being edited', async ({ page }) => {
    await stub(page)
    await open(page)
    await upload(page)
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })
})
