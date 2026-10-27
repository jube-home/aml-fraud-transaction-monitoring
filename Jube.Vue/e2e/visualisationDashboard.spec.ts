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
  permissions: [],
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
  { id: 21, name: 'Totals', columnSpan: 1, rowSpan: 1, priority: 2 },
  { id: 22, name: 'Table', columnSpan: 2, rowSpan: 1, priority: 1 },
]

const svg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 40" role="img" aria-label="Totals chart"><rect width="100" height="40"/></svg>'

const card = (id: number, overrides: Record<string, unknown> = {}) => ({
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
  ...overrides,
})

interface Options {
  permissions?: number[]
  noParameters?: boolean
  registryStatus?: number
  failTile?: number
  noTiles?: boolean
}

async function stub(page: Page, calls: string[] = [], options: Options = {}) {
  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/VisualisationRegistry/Summary', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/VisualisationRegistry/GetByShowInDirectory', (route) =>
    route.fulfill({
      json: [{ id: 5, guid: 'reg-guid', name: 'Sales overview', contextKindId: 1 }],
    }),
  )
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
    (route) => route.fulfill({ json: options.noTiles ? [] : tiles }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/*/Card', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').slice(-2)[0])

    calls.push(`Card ${id} ${route.request().postData() ?? ''}`)

    if (options.failTile === id) return route.fulfill({ status: 500, body: '' })

    return route.fulfill({ json: card(id) })
  })
}

async function open(page: Page) {
  await page.goto('/tools?tool=visualisation-dashboard')
  await page.getByTestId('bench-directory').selectOption('reg-guid')
}

test.describe('visualisation dashboard', () => {
  test('asks for a visualisation first', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=visualisation-dashboard')

    await expect(page.getByTestId('bench-pick')).toBeVisible()
  })

  test('waits for the parameters, then draws every tile from its own card', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await expect(page.getByTestId('dashboard-title')).toHaveText('Sales overview')
    await expect(page.getByTestId('dashboard-waiting')).toBeVisible()
    expect(calls).toHaveLength(0)

    await page.getByTestId('visualisation-run').click()

    await expect(
      page.getByTestId('tile-21').getByRole('img', { name: 'Totals chart' }),
    ).toBeVisible()
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
    expect(calls).toHaveLength(2)
  })

  test('sends the typed parameters as strings with a date as an instant and a flag as 1 or 0', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page.getByTestId('parameter-11').fill('South')
    await page.getByTestId('parameter-13').check()
    await page.getByTestId('visualisation-run').click()

    await expect.poll(() => calls.length).toBe(2)

    const body = JSON.parse(calls[0]!.replace(/^Card \d+ /, ''))

    expect(body.parameters).toHaveLength(3)
    expect(body.parameters[0]).toEqual({ id: 11, value: 'South' })
    expect(body.parameters[1].value).toMatch(/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{3}Z$/)
    expect(body.parameters[2]).toEqual({ id: 13, value: '1' })
    expect(body.width).toBeGreaterThan(0)
    expect(body.height).toBeGreaterThan(0)
  })

  test('refuses to run while a required parameter is blank', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page.getByTestId('parameter-11').fill('')
    await page.getByTestId('visualisation-run').click()

    await expect(page.getByTestId('parameter-11-missing')).toBeVisible()
    await expect(page.getByTestId('dashboard-waiting')).toBeVisible()
    expect(calls).toHaveLength(0)
  })

  test('runs at once when there are no parameters', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls, { noParameters: true })
    await open(page)

    await expect(page.getByTestId('visualisation-parameters')).toHaveCount(0)
    await expect(page.getByTestId('tile-21')).toBeVisible()
    await expect.poll(() => calls.length).toBe(2)
  })

  test('draws the other tiles when one fails', async ({ page }) => {
    await stub(page, [], { noParameters: true, failTile: 21 })
    await open(page)

    await expect(page.getByTestId('tile-21-failure')).toContainText('could not be drawn')
    await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
  })

  test('says so when the visualisation is missing or not permitted', async ({ page }) => {
    await stub(page, [], { registryStatus: 404 })
    await open(page)

    await expect(page.getByTestId('dashboard-unavailable')).toContainText('could not be found')
  })

  test('says so when the user may not see it', async ({ page }) => {
    await stub(page, [], { registryStatus: 403 })
    await open(page)

    await expect(page.getByTestId('dashboard-unavailable')).toContainText('not permitted')
  })

  test('says so when there are no active datasources', async ({ page }) => {
    await stub(page, [], { noParameters: true, noTiles: true })
    await open(page)

    await expect(page.getByTestId('dashboard-empty')).toBeVisible()
  })

  test('hides the parameter form when the host asks and runs with the defaults', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls)
    await page.goto('/tools?tool=visualisation-dashboard')
    await page.getByTestId('bench-hide-parameters').check()
    await page.getByTestId('bench-directory').selectOption('reg-guid')

    await expect(page.getByTestId('visualisation-parameters')).toHaveCount(0)
    await expect(page.getByTestId('tile-21')).toBeVisible()
    await expect.poll(() => calls.length).toBe(2)
  })
})

test.describe('visualisation pages', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('lists the directory and opens a visualisation from it', async ({ page }) => {
    await stub(page, [], { noParameters: true })
    await page.goto('/visualisation')

    await expect(page.getByTestId('directory-list')).toContainText('Sales overview')

    await page.getByTestId('grid-action-open').click()

    await expect(page).toHaveURL(/\/visualisation\/reg-guid$/)
    await expect(page.getByTestId('beat-dashboard-reg-guid')).toContainText('Sales overview')
    await expect(page.getByTestId('tile-21')).toBeVisible()
  })

  test('fills the parameters from the address by name', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await page.goto('/visualisation/reg-guid?Region=West')

    await expect(page.getByTestId('parameter-11')).toHaveValue('West')
  })

  test('says so when the directory is empty', async ({ page }) => {
    await stub(page)
    await page.route('**/api/VisualisationRegistry/Summary', (route) => route.fulfill({ json: [] }))
    await page.route('**/api/VisualisationRegistry/GetByShowInDirectory', (route) =>
      route.fulfill({ json: [] }),
    )
    await page.goto('/visualisation')

    await expect(page.getByTestId('directory-empty')).toBeVisible()
  })

  test('is in the menu and leads to the directory', async ({ page }) => {
    await stub(page, [], { permissions: [28], noParameters: true })
    await page.goto('/visualisation/reg-guid')
    await page
      .getByRole('navigation', { name: 'Main navigation' })
      .getByRole('link', { name: 'Visualisation' })
      .click()

    await expect(page).toHaveURL(/\/visualisation$/)
  })
})

test.describe('visualisation dashboard accessibility', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`has no violations in the ${theme} theme`, async ({ page }) => {
      await stub(page)
      await open(page)
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await page.getByTestId('visualisation-run').click()
      await expect(page.getByTestId('tile-22').getByText('North')).toBeVisible()
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="visualisation-dashboard"]')
        .analyze()

      expect(
        results.violations.map(
          (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
        ),
      ).toEqual([])
    })
  }

  test('has no violations in Arabic', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await page.getByTestId('visualisation-run').click()
    await expect(page.getByTestId('tile-22')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="visualisation-dashboard"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
