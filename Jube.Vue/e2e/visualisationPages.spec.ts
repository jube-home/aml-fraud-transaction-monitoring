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
import { setLanguage, setTheme, settleAnimations } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [28],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const registry = {
  id: 5,
  guid: 'reg-guid',
  name: 'Sales overview',
  columns: 2,
  columnWidth: 400,
  rowHeight: 260,
}

const parameters = [
  { id: 11, name: 'Region', dataTypeId: 1, defaultValue: 'North', required: true },
  { id: 12, name: 'Since', dataTypeId: 4, defaultValue: '7', required: false },
  { id: 13, name: 'Include refunds', dataTypeId: 5, defaultValue: '0', required: false },
]

const tiles = [
  { id: 21, name: 'Totals', columnSpan: 1, rowSpan: 1, priority: 2, displayKindId: 2 },
  { id: 22, name: 'Table', columnSpan: 2, rowSpan: 1, priority: 1, displayKindId: 7 },
]

const directory = [
  { id: 5, guid: 'reg-guid', name: 'Sales overview', contextKindId: 1 },
  { id: 6, guid: 'other-guid', name: 'Case volumes', contextKindId: 1 },
  { id: 7, guid: 'hidden-guid', name: 'Hidden board', contextKindId: 2 },
]

const summary = [
  {
    registryId: 5,
    datasourceCount: 2,
    parameterCount: 3,
    displayKindIds: [2, 7],
    executionsInWindow: 42,
    failuresInWindow: 3,
    averageResponseMilliseconds: 123.4,
    lastExecutionDate: '2026-10-21T09:12:00Z',
    lastExecutionUser: 'alex',
    windowDays: 30,
  },
  {
    registryId: 6,
    datasourceCount: 1,
    parameterCount: 0,
    displayKindIds: [2],
    executionsInWindow: 0,
    failuresInWindow: 0,
    averageResponseMilliseconds: null,
    lastExecutionDate: null,
    lastExecutionUser: null,
    windowDays: 30,
  },
]

const chartKinds = [
  { kindId: 2, name: 'Bar chart', description: '', roles: [] },
  { kindId: 7, name: 'Map', description: '', roles: [] },
]

const svg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 40" role="img" aria-label="Totals chart"><rect width="100" height="40"/></svg>'

const card = (id: number) => ({
  id,
  guid: '00000000-0000-0000-0000-000000000000',
  name: id === 21 ? 'Totals' : 'Table',
  columnSpan: 1,
  rowSpan: 1,
  priority: 0,
  stateId: 1,
  message: null,
  rowCount: 2,
  truncated: false,
  executionMilliseconds: 4,
  display: id === 21 ? { kindId: 2, markupKindId: 1, markup: svg } : null,
  grid:
    id === 22
      ? {
          columns: [
            { name: 'region', ordinal: 1, dataTypeId: 1 },
            { name: 'total', ordinal: 2, dataTypeId: 3 },
          ],
          rows: [
            ['North', 10.5],
            ['South', 20],
          ],
        }
      : null,
  style: { modeId: 1, version: 'v1', css: null },
})

interface Options {
  noParameters?: boolean
  registryStatus?: number
  directoryStatus?: number
  directoryFailures?: number
  failTile?: number
  failTileOnce?: number
  entries?: typeof directory
  summaryStatus?: number
}

async function stub(page: Page, calls: string[] = [], options: Options = {}) {
  let directoryCalls = 0
  let tileFailures = 0

  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/VisualisationRegistry/Summary', (route) =>
    options.summaryStatus
      ? route.fulfill({ status: options.summaryStatus, body: '' })
      : route.fulfill({ json: summary }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/ChartKinds', (route) =>
    route.fulfill({ json: chartKinds }),
  )
  await page.route('**/api/VisualisationRegistry/GetByShowInDirectory', (route) => {
    directoryCalls += 1

    if (options.directoryStatus) return route.fulfill({ status: options.directoryStatus, body: '' })
    if (directoryCalls <= (options.directoryFailures ?? 0)) {
      return route.fulfill({ status: 500, body: '' })
    }

    return route.fulfill({ json: options.entries ?? directory })
  })
  await page.route('**/api/VisualisationRegistry/GetByGuidActiveOnly/*', (route) =>
    options.registryStatus
      ? route.fulfill({ status: options.registryStatus, body: '' })
      : route.fulfill({ json: registry }),
  )
  await page.route(
    '**/api/VisualisationRegistryParameter/ByVisualisationRegistryIdActiveOnly/5',
    (route) => route.fulfill({ json: options.noParameters ? [] : parameters }),
  )
  await page.route(
    '**/api/VisualisationRegistryDatasource/ByVisualisationRegistryIdActiveOnly/5',
    (route) => route.fulfill({ json: tiles }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/*/Card', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').slice(-2)[0])

    calls.push(`${id} ${route.request().postData() ?? ''}`)

    if (options.failTile === id) return route.fulfill({ status: 500, body: '' })

    if (options.failTileOnce === id && tileFailures === 0) {
      tileFailures += 1

      return route.fulfill({ status: 500, body: '' })
    }

    return route.fulfill({ json: card(id) })
  })
}

const cardCalls = (calls: string[], id: number) => calls.filter((call) => call.startsWith(`${id} `))

const valueOf = (call: string, parameter: number) =>
  (JSON.parse(call.replace(/^\d+ /, '')).parameters as { id: number; value: string }[]).find(
    (entry) => entry.id === parameter,
  )?.value

test.describe('visualisation directory', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('lists only the entries shown in the directory and filters by name', async ({ page }) => {
    await stub(page)
    await page.goto('/visualisation')

    const list = page.getByTestId('directory-list')

    await expect(list).toContainText('Sales overview')
    await expect(list).toContainText('Case volumes')
    await expect(list).not.toContainText('Hidden board')
    await expect(page.getByTestId('directory-count')).toHaveText('2 visualisations')
    await expect(page.getByTestId('story-trail')).toHaveCount(0)

    await page.getByTestId('directory-search').fill('case')

    await expect(page.getByTestId('directory-count')).toHaveText('1 visualisation')
    await expect(list).not.toContainText('Sales overview')
  })

  test('shows what each dashboard holds and how it has been used', async ({ page }) => {
    await stub(page)
    await page.goto('/visualisation')

    const sales = page.locator('.tabulator-row', { hasText: 'Sales overview' })

    await expect(sales).toContainText('42')
    await expect(sales).toContainText('alex')
    await expect(sales).toContainText('123 ms')
    await expect(sales.locator('.badge', { hasText: '3' })).toBeVisible()

    const quiet = page.locator('.tabulator-row', { hasText: 'Case volumes' })

    await expect(quiet).toContainText('Never run')
    await expect(page.getByTestId('directory-window-note')).toContainText('30 days')
  })

  test('expands a row to list its tiles with their kinds and its parameters', async ({ page }) => {
    await stub(page)
    await page.goto('/visualisation')
    await page
      .locator('.tabulator-row', { hasText: 'Sales overview' })
      .getByTestId('grid-expand')
      .click()

    const detail = page.getByTestId('directory-detail-5')

    await expect(detail).toContainText('Totals')
    await expect(detail).toContainText('Bar chart')
    await expect(detail).toContainText('Table')
    await expect(detail).toContainText('Map')
    await expect(detail).toContainText('Region')
    await expect(detail).toContainText('Date and time')
    await expect(detail.locator('.badge', { hasText: 'Required' })).toHaveCount(1)
  })

  test('still lists the dashboards when the usage summary cannot be loaded', async ({ page }) => {
    await stub(page, [], { summaryStatus: 500 })
    await page.goto('/visualisation')

    const sales = page.locator('.tabulator-row', { hasText: 'Sales overview' })

    await expect(sales).toBeVisible()
    await expect(sales).toContainText('n/a')
    await expect(page.getByTestId('directory-open-failed')).toHaveCount(0)
  })

  test('says so when the directory is empty', async ({ page }) => {
    await stub(page, [], { entries: [] })
    await page.goto('/visualisation')

    await expect(page.getByTestId('directory-empty')).toBeVisible()
    await expect(page.getByTestId('directory-count')).toHaveText('no visualisations')
  })

  test('reports a failed load and recovers on retry', async ({ page }) => {
    await stub(page, [], { directoryFailures: 1 })
    await page.goto('/visualisation')

    await expect(page.getByTestId('directory-failed')).toContainText('could not be loaded')

    await page.getByTestId('directory-retry').click()

    await expect(page.getByTestId('directory-list')).toContainText('Sales overview')
  })

  test('says so when the user may not see the directory', async ({ page }) => {
    await stub(page, [], { directoryStatus: 403 })
    await page.goto('/visualisation')

    await expect(page.getByTestId('directory-failed')).toContainText('not permitted')
    await expect(page.getByTestId('directory-retry')).toHaveCount(0)
  })
})

test.describe('visualisation dashboard page', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('opens a dashboard as the next beat and goes back to the directory from its receipt', async ({
    page,
  }) => {
    await stub(page, [], { noParameters: true })
    await page.goto('/visualisation')
    await page
      .getByRole('row', { name: /Sales overview/ })
      .getByRole('button', { name: 'Open' })
      .click()

    await expect(page).toHaveURL(/\/visualisation\/reg-guid$/)
    await expect(page.getByTestId('beat-dashboard-reg-guid')).toContainText('Sales overview')
    await expect(page.getByTestId('beat-dashboard-reg-guid')).toContainText('2 tiles')
    await expect(page.getByTestId('story-trail')).toBeVisible()
    await expect(
      page.getByTestId('tile-21').getByRole('img', { name: 'Totals chart' }),
    ).toBeVisible()

    await page.getByTestId('receipt-directory').click()

    await expect(page).toHaveURL(/\/visualisation$/)
    await expect(page.getByTestId('directory-list')).toBeVisible()
  })

  test('keeps the parameters in the address so a link and back and forward reproduce the dashboard', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls)
    await page.goto('/visualisation/reg-guid')
    await expect(page.getByTestId('dashboard-waiting')).toBeVisible()

    await page.getByTestId('parameter-11').fill('South')
    await page.getByTestId('parameter-13').check()
    await page.getByTestId('visualisation-run').click()

    await expect(page).toHaveURL(/Region=South/)
    await expect(page).toHaveURL(/Include\+refunds=1|Include%20refunds=1/)
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
    await expect.poll(() => calls.length).toBe(2)

    const link = page.url()
    const before = calls.length

    await page.goBack()

    await expect(page.getByTestId('parameter-11')).toHaveValue('North')
    await expect(page.getByTestId('dashboard-waiting')).toBeVisible()

    await page.goForward()

    await expect(page.getByTestId('parameter-11')).toHaveValue('South')
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
    expect(calls).toHaveLength(before)

    await page.goto(link)

    await expect(page.getByTestId('parameter-11')).toHaveValue('South')
    await expect(page.getByTestId('parameter-13')).toBeChecked()
    await expect.poll(() => calls.length).toBe(before + 2)
    expect(valueOf(calls[calls.length - 1]!, 11)).toBe('South')
  })

  test('re-requests every tile with the new value when a parameter changes', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await page.goto('/visualisation/reg-guid?Region=West')
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
    await expect.poll(() => calls.length).toBe(2)

    await page.getByTestId('parameter-11').fill('East')
    await page.getByTestId('visualisation-run').click()

    await expect.poll(() => calls.length).toBe(4)
    expect(valueOf(cardCalls(calls, 21)[1]!, 11)).toBe('East')
    expect(valueOf(cardCalls(calls, 22)[1]!, 11)).toBe('East')
    await expect(page).toHaveURL(/Region=East/)
  })

  test('has no refresh button, because running is what draws the tiles again', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls, { noParameters: true })
    await page.goto('/visualisation/reg-guid')
    await expect.poll(() => calls.length).toBe(2)

    await expect(page.getByTestId('dashboard-refresh')).toHaveCount(0)
  })

  test('a failing tile says so and retries alone while the others stay drawn', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls, { noParameters: true, failTileOnce: 21 })
    await page.goto('/visualisation/reg-guid')

    await expect(page.getByTestId('tile-21-failure')).toContainText('could not be drawn')
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
    expect(cardCalls(calls, 22)).toHaveLength(1)

    await page.getByTestId('tile-21-retry').click()

    await expect(
      page.getByTestId('tile-21').getByRole('img', { name: 'Totals chart' }),
    ).toBeVisible()
    expect(cardCalls(calls, 21)).toHaveLength(2)
    expect(cardCalls(calls, 22)).toHaveLength(1)
  })

  test('says so when the visualisation is forbidden or missing and offers the way back', async ({
    page,
  }) => {
    await stub(page, [], { registryStatus: 403 })
    await page.goto('/visualisation/reg-guid')

    await expect(page.getByTestId('dashboard-unavailable')).toContainText('not permitted')

    await page.getByTestId('visualisation-back').click()

    await expect(page).toHaveURL(/\/visualisation$/)
  })

  test('does not scroll the page sideways at phone width', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 })
    await stub(page)
    await page.goto('/visualisation')
    await expect(page.getByTestId('directory-list')).toContainText('Sales overview')

    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
    ).toBe(true)

    await page.goto('/visualisation/reg-guid?Region=West')
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()

    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
    ).toBe(true)
  })
})

test.describe('visualisation pages accessibility', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  async function audit(page: Page) {
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="visualisation-page"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  }

  for (const theme of ['light', 'dark'] as const) {
    test(`has no violations in the ${theme} theme`, async ({ page }) => {
      await stub(page)
      await page.goto('/visualisation')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await expect(page.getByTestId('directory-list')).toContainText('Sales overview')
      await audit(page)

      await page.goto('/visualisation/reg-guid?Region=West')
      await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
      await audit(page)
    })
  }

  test('has no violations in Arabic', async ({ page }) => {
    await stub(page)
    await page.goto('/visualisation')
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await expect(page.getByTestId('directory-list')).toBeVisible()
    await audit(page)

    await page.goto('/visualisation/reg-guid?Region=West')
    await expect(page.getByTestId('tile-22')).toBeVisible()
    await audit(page)
  })
})
