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

const option = (key: string, value: number, label: string) => ({ key, value, label })

const model = {
  area: 'ArchiverStagePerformanceCounter',
  endpoint: '/api/ArchiverStagePerformanceCounter',
  title: 'Archiver stage performance',
  snapshot: false,
  sortField: 'createdDate',
  sortDirection: 'desc',
  take: 2000,
  timeField: 'createdDate',
  windowMinutes: 60,
  search: null,
  filters: [
    {
      param: 'stageId',
      label: 'Stage',
      kind: 'select',
      lookup: null,
      options: [
        option('ArchiverStage.BuildArchiveJson', 1, 'Build archive JSON'),
        option('ArchiverStage.RdbmsArchiveWrite', 3, 'RDBMS archive write'),
      ],
    },
  ],
  columns: [
    {
      field: 'id',
      title: 'Id',
      description: '',
      type: 'integer',
      format: 'integer',
      unit: null,
      order: 10,
      sortable: true,
      hidden: true,
      numerator: null,
      denominator: null,
    },
    {
      field: 'stageName',
      title: 'Stage',
      description: '',
      type: 'string',
      format: 'default',
      unit: null,
      order: 20,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
    {
      field: 'createdDate',
      title: 'Written',
      description: '',
      type: 'datetime',
      format: 'dateTime',
      unit: null,
      order: 30,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
    {
      field: 'totalMicroseconds',
      title: 'Total',
      description: '',
      type: 'integer',
      format: 'integer',
      unit: 'µs',
      order: 40,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
    {
      field: 'invokeCount',
      title: 'Count',
      description: '',
      type: 'integer',
      format: 'integer',
      unit: null,
      order: 70,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
    {
      field: 'averageMicroseconds',
      title: 'Average',
      description: '',
      type: 'number',
      format: 'integer',
      unit: 'µs',
      order: 75,
      sortable: true,
      hidden: false,
      numerator: 'totalMicroseconds',
      denominator: 'invokeCount',
    },
  ],
}

const rows = [
  {
    id: 1,
    stageName: 'RdbmsArchiveWrite',
    createdDate: '2026-10-21T09:00:00Z',
    totalMicroseconds: 120000,
    invokeCount: 40,
  },
  {
    id: 2,
    stageName: 'BuildArchiveJson',
    createdDate: '2026-10-21T09:01:00Z',
    totalMicroseconds: 5000,
    invokeCount: 0,
  },
]

const statistics = {
  columns: {
    totalMicroseconds: {
      min: 5000,
      max: 120000,
      mean: 62500,
      median: 62500,
      standardDeviation: 57500,
      histogram: [
        { bin: 5000, frequency: 1 },
        { bin: 120000, frequency: 1 },
      ],
    },
  },
}

const rowsOf = (page: Page) =>
  page.locator('[aria-label="Archiver stage performance"] .tabulator-row')

async function stub(page: Page, requests: URLSearchParams[] = [], total = rows.length) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/GridModel', (route) =>
    route.fulfill({ json: ['ArchiverStagePerformanceCounter'] }),
  )
  await page.route('**/api/GridModel/ArchiverStagePerformanceCounter', (route) =>
    route.fulfill({ json: model }),
  )
  await page.route('**/api/ArchiverStagePerformanceCounter**', (route) => {
    requests.push(new URL(route.request().url()).searchParams)

    return route.fulfill({ json: { rows, total, statistics } })
  })
}

async function open(page: Page) {
  await page.goto('/tools?tool=performance')
  await page.getByTestId('bench-area').selectOption('ArchiverStagePerformanceCounter')
  await page.getByTestId('bench-load').click()
  await expect(page.getByTestId('payload-grid-ArchiverStagePerformanceCounter')).toBeVisible()
  await expect(rowsOf(page).first()).toBeVisible()
}

test.describe('Performance grid', () => {
  test.use({ viewport: { width: 1500, height: 1100 } })

  test('draws the columns, filters and counts the DTO described, and asks for the last hour', async ({
    page,
  }) => {
    const requests: URLSearchParams[] = []

    await stub(page, requests)
    await open(page)

    const titles = page.locator('.tabulator-col-title')

    await expect(titles.filter({ hasText: 'Total (µs)' })).toHaveCount(1)
    await expect(titles.filter({ hasText: 'Average (µs)' })).toHaveCount(1)
    await expect(titles.filter({ hasText: /^Id$/ })).toHaveCount(0)
    await expect(page.getByTestId('payload-count')).toContainText('2 of 2 rows')
    await expect(rowsOf(page).first()).toContainText('5,000')
    await expect(rowsOf(page).nth(1)).toContainText('120,000')
    await expect(rowsOf(page).nth(1)).toContainText('3,000')

    const first = requests[0]!
    const from = new Date(first.get('from')!)
    const to = new Date(first.get('to')!)

    expect(to.getTime() - from.getTime()).toBe(60 * 60_000)
    expect(first.get('take')).toBe('2000')
    expect(first.get('sortField')).toBe('createdDate')
    expect(first.get('sortDirection')).toBe('desc')
    expect(first.has('stageId')).toBe(false)
  })

  test('a ratio with nothing to divide by is zero, not a failure', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(rowsOf(page).nth(1)).toContainText('0')
  })

  test('choosing a stage and applying sends it, and clearing takes it away again', async ({
    page,
  }) => {
    const requests: URLSearchParams[] = []

    await stub(page, requests)
    await open(page)

    await page.getByTestId('payload-filter-stageId').selectOption('3')
    await page.getByTestId('payload-apply').click()

    await expect.poll(() => requests.at(-1)?.get('stageId')).toBe('3')

    await page.getByTestId('payload-clear').click()

    await expect(page.getByTestId('payload-filter-stageId')).toHaveValue('')

    await page.getByTestId('payload-refresh').click()

    await expect.poll(() => requests.at(-1)?.has('stageId')).toBe(false)
  })

  test('sorting by a heading asks the server to sort, not just the browser', async ({ page }) => {
    const requests: URLSearchParams[] = []

    await stub(page, requests)
    await open(page)

    await page.locator('.tabulator-col-title', { hasText: 'Count' }).click()

    await expect.poll(() => requests.at(-1)?.get('sortField')).toBe('invokeCount')
  })

  test('says when the rows are only the first of more, and shows the statistics on request', async ({
    page,
  }) => {
    await stub(page, [], 5000)
    await open(page)

    await expect(page.getByTestId('payload-count')).toContainText('2 of 5,000 rows shown')

    await page.getByTestId('payload-statistics').locator('summary').click()

    await expect(page.getByTestId('payload-statistics')).toContainText('Total (µs)')
  })

  test('hovering a distribution explodes it into the full server-drawn chart and leaving puts it away', async ({
    page,
  }) => {
    const bodies: unknown[] = []

    await page.route('**/api/Chart/PayloadStatistics/Histogram', (route) => {
      bodies.push(route.request().postDataJSON())

      return route.fulfill({
        contentType: 'image/svg+xml',
        body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" data-testid="drawn"><rect width="10" height="10"/><script>window.hacked=1</script></svg>',
      })
    })
    await stub(page, [], 5000)
    await open(page)
    await page.getByTestId('payload-statistics').locator('summary').click()

    const sparkline = page.getByTestId('payload-statistics').locator('svg[role=img]').first()
    const popover = page.getByTestId('statistics-popover')

    await expect(popover).toBeHidden()

    await sparkline.hover()

    await expect(popover).toBeVisible()
    await expect(popover.getByTestId('jube-chart').locator('svg')).toHaveCount(1)
    await expect(popover.locator('script')).toHaveCount(0)
    expect(await page.evaluate(() => (window as unknown as { hacked?: number }).hacked)).toBe(
      undefined,
    )
    expect(bodies).toHaveLength(1)
    expect(bodies[0]).toMatchObject({
      color: expect.stringMatching(/^#[0-9a-f]{6}$/),
      statistics: { mean: 62500, median: 62500 },
    })

    await page.mouse.move(5, 5)

    await expect(popover).toBeHidden()
  })

  test('a chart that fails says so and draws on retry', async ({ page }) => {
    let calls = 0

    await page.route('**/api/Chart/PayloadStatistics/Histogram', (route) => {
      calls += 1

      return calls === 1
        ? route.fulfill({ status: 500, body: '' })
        : route.fulfill({
            contentType: 'image/svg+xml',
            body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10"/></svg>',
          })
    })
    await stub(page, [], 5000)
    await open(page)
    await page.getByTestId('payload-statistics').locator('summary').click()
    await page.getByTestId('payload-statistics').locator('svg[role=img]').first().hover()

    const popover = page.getByTestId('statistics-popover')

    await expect(popover).toContainText('could not be drawn')
    await popover.getByRole('button', { name: 'Try again' }).click()

    await expect(popover.getByTestId('jube-chart').locator('svg')).toHaveCount(1)
    expect(calls).toBe(2)
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await open(page)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
