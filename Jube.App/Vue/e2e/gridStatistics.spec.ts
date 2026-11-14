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
import {me} from './hierarchyStub'
import {settleAnimations} from './support'

const model = {
  area: 'HttpProcessingCounter',
  endpoint: '/api/HttpProcessingCounter',
  title: 'Http processing counter',
  snapshot: false,
  sortField: 'createdDate',
  sortDirection: 'desc',
  take: 2000,
  timeField: 'createdDate',
  windowMinutes: 60,
  search: null,
  filters: [],
  columns: [
    {
      field: 'queueDepth',
      title: 'Queue depth',
      description: '',
      type: 'integer',
      format: 'integer',
      unit: null,
      order: 10,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
  ],
}

const statistics = {
  columns: {
    queueDepth: {
      min: 0,
      max: 412,
      mean: 96.4,
      median: 60,
      standardDeviation: 88.12,
      histogram: [
        {bin: 0, frequency: 120},
        {bin: 41.2, frequency: 80},
        {bin: 82.4, frequency: 54},
        {bin: 123.6, frequency: 31},
        {bin: 164.8, frequency: 18},
        {bin: 206, frequency: 9},
        {bin: 247.2, frequency: 5},
        {bin: 288.4, frequency: 3},
        {bin: 329.6, frequency: 1},
        {bin: 370.8, frequency: 1},
      ],
    },
    droppedCount: {
      min: 0,
      max: 0,
      mean: 0,
      median: 0,
      standardDeviation: 0,
      histogram: [],
    },
  },
}

async function open(page: Page) {
  await page.route('**/api/Me', (route) => route.fulfill({json: {...me, landlord: true}}))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({json: {id: 1, name: 'Tenant one'}}),
  )
  await page.route('**/api/TenantRegistry/ByFilter*', (route) =>
    route.fulfill({json: [{id: 1, name: 'Tenant one'}]}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/GridModel', (route) =>
    route.fulfill({json: ['HttpProcessingCounter']}),
  )
  await page.route('**/api/GridModel/HttpProcessingCounter', (route) =>
    route.fulfill({json: model}),
  )
  await page.route('**/api/HttpProcessingCounter**', (route) =>
    route.fulfill({
      json: {
        rows: [{queueDepth: 5}],
        total: 1,
        statistics,
      },
    }),
  )
  await page.goto('/administration/performance?path=family:Engine,area:HttpProcessingCounter')
  await page.getByTestId('payload-statistics').locator('summary').click()
  await expect(page.getByTestId('payload-statistics').locator('.tabulator-row')).toHaveCount(2)
}

const statisticRows = (page: Page) =>
  page.getByTestId('payload-statistics').locator('.tabulator-row')

test.describe('GridStatistics', () => {
  test.use({viewport: {width: 1600, height: 1000}})

  test('shows a histogram and figures per column, and says when a column has no values', async ({
                                                                                                  page,
                                                                                                }) => {
    await open(page)

    const rows = statisticRows(page)

    await expect(rows.nth(0)).toContainText('droppedCount')
    await expect(rows.nth(0)).toContainText('No values')
    await expect(rows.nth(0)).not.toContainText('0.00')
    await expect(rows.nth(0)).toContainText('–')
    await expect(rows.nth(1)).toContainText('Queue depth')
    await expect(rows.nth(1)).toContainText('412.00')
    await expect(rows.nth(1)).toContainText('96.40')
    await expect(rows.nth(1).locator('svg rect')).toHaveCount(10)
  })

  test('describes each histogram to assistive technology and gives each bar a title', async ({
                                                                                               page,
                                                                                             }) => {
    await open(page)

    const svg = statisticRows(page).nth(1).locator('svg')

    await expect(svg).toHaveAttribute('role', 'img')
    await expect(svg).toHaveAttribute('aria-label', /10 bins from 0\.00 to 412\.00/)
    await expect(svg.locator('rect title').first()).toHaveText('0.00: 120')
  })

  test('shows the display name, and the column name when there is none', async ({page}) => {
    await open(page)

    const rows = statisticRows(page)

    await expect(rows.nth(1)).toContainText('Queue depth')
    await expect(rows.nth(1)).not.toContainText('queueDepth')
    await expect(rows.nth(0)).toContainText('droppedCount')
  })

  test('sorts by a figure from the column header', async ({page}) => {
    await open(page)

    const panel = page.getByTestId('payload-statistics')

    await panel.locator('.tabulator-col', {hasText: 'Mean'}).click()
    await panel.locator('.tabulator-col', {hasText: 'Mean'}).click()

    await expect(statisticRows(page).first()).toContainText('Queue depth')
  })

  test('has no accessibility violations', async ({page}) => {
    await open(page)
    await settleAnimations(page)

    const results = await new AxeBuilder({page}).analyze()

    expect(results.violations).toEqual([])
  })
})
