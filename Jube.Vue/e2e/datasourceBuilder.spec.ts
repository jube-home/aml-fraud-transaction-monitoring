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

const kinds = [
  {
    kindId: 2,
    name: 'Bar',
    description: 'Compares values across categories.',
    roles: [
      {
        roleId: 1,
        name: 'Category',
        description: 'Labels the bars.',
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
  {
    kindId: 11,
    name: 'Pie',
    description: 'Shares of a whole.',
    roles: [
      {
        roleId: 1,
        name: 'Category',
        description: 'Slices.',
        many: false,
        optional: false,
        acceptsId: 1,
      },
      {
        roleId: 2,
        name: 'Value',
        description: 'Size.',
        many: false,
        optional: false,
        acceptsId: 2,
      },
    ],
  },
]

const templateKind = {
  kindId: 29,
  name: 'Template',
  description: 'Lays the rows out in your own markup.',
  roles: [],
}

const introspection = {
  valid: true,
  columns: [
    { name: 'region', ordinal: 1, dataTypeName: 'text', dataTypeId: 1 },
    { name: 'total', ordinal: 2, dataTypeName: 'numeric', dataTypeId: 3 },
  ],
  error: null,
  kinds: [
    { kindId: 2, fitsByPosition: true, reason: null },
    { kindId: 11, fitsByPosition: true, reason: null },
    { kindId: 29, fitsByPosition: true, reason: null },
  ],
  resolvedMapping: [
    { roleId: 1, columns: ['region'], explicit: false },
    { roleId: 2, columns: ['total'], explicit: false },
  ],
  mappingError: null,
  templateError: null,
}

const svg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 40" role="img" aria-label="Totals"><rect width="100" height="40"/></svg>'

const card = {
  id: 0,
  guid: '00000000-0000-0000-0000-000000000000',
  name: 'Preview',
  columnSpan: 1,
  rowSpan: 1,
  priority: 0,
  stateId: 1,
  message: null,
  rowCount: 2,
  truncated: false,
  executionMilliseconds: 7,
  display: { kindId: 2, markupKindId: 1, markup: svg },
  grid: {
    columns: [
      { name: 'region', ordinal: 1, dataTypeId: 1 },
      { name: 'total', ordinal: 2, dataTypeId: 3 },
    ],
    rows: [
      ['North', 10.5],
      ['South', 20],
    ],
  },
  style: { modeId: 1, version: 'v1', css: null },
}

async function stub(page: Page, calls: string[] = [], invalid = false, parameters: unknown[] = []) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/VisualisationRegistry', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Sales' }] }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/ByVisualisationRegistryId/1', (route) =>
    route.fulfill({ json: [] }),
  )
  await page.route(
    '**/api/VisualisationRegistryParameter/ByVisualisationRegistryIdActiveOnly/1',
    (route) => route.fulfill({ json: parameters }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/ChartKinds', (route) =>
    route.fulfill({ json: [...kinds, templateKind] }),
  )
  await page.route('**/api/VisualisationRegistryDatasource/Introspect', (route) => {
    calls.push(`Introspect ${route.request().postData() ?? ''}`)

    const body = JSON.parse(route.request().postData() ?? '{}')

    return route.fulfill({
      json: invalid
        ? {
            valid: false,
            columns: [],
            error: 'syntax error at or near "selec"',
            errorLine: 1,
            errorColumn: 1,
            kinds: [],
          }
        : {
            ...introspection,
            templateError: body.templateText?.includes('nope')
              ? 'Template line 1, column 4: "nope" is not known here.'
              : null,
          },
    })
  })
  await page.route('**/api/VisualisationRegistryDatasource/Preview', (route) => {
    calls.push(`Preview ${route.request().postData() ?? ''}`)

    const body = JSON.parse(route.request().postData() ?? '{}')

    return route.fulfill({
      json:
        body.displayKindId === 29
          ? {
              ...card,
              display: {
                kindId: 29,
                markupKindId: 2,
                markup: '<ul><li>North</li><li>South</li></ul><script>window.__hacked = 1</script>',
              },
            }
          : card,
    })
  })
}

async function open(page: Page) {
  await page.goto('/tools?tool=datasource-builder')
  await page.getByTestId('bench-registry').selectOption('1')
  await expect(page.getByTestId('datasource-builder')).toBeVisible()
}

async function type(page: Page, text: string) {
  await page.getByTestId('datasource-sql').locator('.cm-content').click()
  await page.keyboard.type(text)
}

test.describe('datasource builder', () => {
  test('asks for a visualisation first', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=datasource-builder')

    await expect(page.getByTestId('bench-pick')).toBeVisible()
  })

  test('checks the command and shows its columns', async ({ page }) => {
    await stub(page)
    await open(page)
    await type(page, 'select region, total from sales')

    await expect(page.getByTestId('datasource-status')).toContainText('Command is valid')
    await expect(page.getByTestId('datasource-column-1')).toContainText('region')
    await expect(page.getByTestId('datasource-column-2')).toContainText('numeric')
  })

  test('reports a rejected command', async ({ page }) => {
    await stub(page, [], true)
    await open(page)
    await type(page, 'selec 1')

    await expect(page.getByTestId('datasource-sql-error')).toContainText('syntax error')
    await expect(page.getByTestId('datasource-sql-error-position')).toContainText(
      'line 1, column 1',
    )
    await expect(page.getByTestId('datasource-sql').locator('.cm-lintRange-error')).toHaveCount(1)
    await expect(page.getByTestId('datasource-preview-waiting')).toBeVisible()
  })

  test('runs the command on its own before any display is chosen', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await expect(page.getByTestId('datasource-run-sql')).toBeDisabled()
    await type(page, 'select region, total from sales')
    await expect(page.getByTestId('datasource-status')).toContainText('Command is valid')

    await page.getByTestId('datasource-run-sql').click()

    await expect(page.getByTestId('datasource-card').first()).toContainText('2 rows')

    const run = calls.find(
      (call) => call.startsWith('Preview') && call.includes('"includeGrid":true'),
    )

    expect(run).toContain('"displayKindId":0')
  })

  test('previews the chart and the grid from one response', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await page.getByTestId('datasource-grid-toggle').check()

    await expect(page.getByTestId('datasource-card-rows')).toContainText('2')
    await expect(page.getByRole('img', { name: 'Totals' })).toBeVisible()
    await expect(page.getByText('North')).toBeVisible()
    expect(
      calls.some((call) => call.startsWith('Preview') && call.includes('"includeGrid":true')),
    ).toBe(true)
  })

  test('emits the consolidated value', async ({ page }) => {
    await stub(page)
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await expect(page.getByTestId('datasource-status')).toContainText('Command is valid')

    const value = JSON.parse((await page.getByTestId('bench-value').textContent()) ?? '{}')

    expect(value).toMatchObject({ displayKindId: 2, includeGrid: false, valid: true })
    expect(value.command).toContain('select region')
  })

  test('assigns a column to a role', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await page.getByTestId('datasource-kind').selectOption('11')
    await page.getByTestId('datasource-role-1').selectOption('total')

    await expect
      .poll(() =>
        calls.some((call) => call.startsWith('Introspect') && call.includes('"columns":["total"]')),
      )
      .toBe(true)
  })

  test('writes a template and previews its markup', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await page.getByTestId('datasource-kind').selectOption('29')
    await page.getByTestId('datasource-template').locator('.cm-content').click()
    await page.keyboard.type('<ul></ul>')

    await expect(page.getByTestId('datasource-markup').locator('li')).toHaveCount(2)
    await expect(page.getByTestId('datasource-markup').locator('script')).toHaveCount(0)
    expect(
      await page.evaluate(() => (window as unknown as Record<string, unknown>).__hacked),
    ).toBeUndefined()
    expect(
      calls.some((call) => call.startsWith('Preview') && call.includes('"templateText"')),
    ).toBe(true)
  })

  test('shows a template error and holds the preview back', async ({ page }) => {
    await stub(page)
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await page.getByTestId('datasource-kind').selectOption('29')
    await page.getByTestId('datasource-template').locator('.cm-content').click()
    await page.keyboard.type('<p>nope</p>')

    await expect(page.getByTestId('datasource-template-error')).toContainText('not known here')
    await expect(page.getByTestId('datasource-preview-waiting')).toBeVisible()
  })

  test('is not valid with an empty template', async ({ page }) => {
    await stub(page)
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await page.getByTestId('datasource-kind').selectOption('29')

    await expect(page.getByTestId('datasource-preview-waiting')).toBeVisible()
  })

  test('previews with the registry parameters and reruns them when changed', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls, false, [
      { id: 11, name: 'Region', dataTypeId: 1, defaultValue: 'North', required: true },
    ])
    await open(page)
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()

    await expect(page.getByTestId('visualisation-parameters')).toBeVisible()
    await expect.poll(() => calls.some((call) => call.startsWith('Preview'))).toBe(true)
    expect(calls.filter((call) => call.startsWith('Preview'))[0]).toContain('"parameters":[]')

    await page.getByTestId('parameter-11').fill('South')
    await page.getByTestId('visualisation-run').click()

    await expect
      .poll(() => calls.some((call) => call.startsWith('Preview') && call.includes('"South"')))
      .toBe(true)
  })

  test('is not valid until a chart or a grid is on', async ({ page }) => {
    await stub(page)
    await open(page)
    await type(page, 'select region, total from sales')

    await expect(page.getByTestId('datasource-needs-display')).toBeVisible()
    await expect(page.getByTestId('datasource-preview-waiting')).toBeVisible()
  })
})

test.describe('datasource builder accessibility', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`has no violations in the ${theme} theme`, async ({ page }) => {
      await stub(page)
      await open(page)
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await type(page, 'select region, total from sales')
      await page.getByTestId('datasource-chart-toggle').check()
      await page.getByTestId('datasource-grid-toggle').check()
      await expect(page.getByTestId('datasource-card-rows')).toBeVisible()
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="datasource-builder"]')
        .analyze()

      expect(
        results.violations.map(
          (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
        ),
      ).toEqual([])
    })
  }

  test('has no violations with the template editor in the dark theme', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('account-button').click()
    await setTheme(page, 'dark')
    await page.keyboard.press('Escape')
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await page.getByTestId('datasource-kind').selectOption('29')
    await page.getByTestId('datasource-template').locator('.cm-content').click()
    await page.keyboard.type('<ul class="list"><li v-for="row in rows">[@ row.region @]</li></ul>')
    await expect(page.getByTestId('datasource-markup').locator('li')).toHaveCount(2)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="datasource-builder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })

  test('has no violations in Arabic', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await type(page, 'select region, total from sales')
    await page.getByTestId('datasource-chart-toggle').check()
    await expect(page.getByTestId('datasource-card-rows')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="datasource-builder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
