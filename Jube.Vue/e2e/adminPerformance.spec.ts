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
  userName: 'root',
  landlord: true,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

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
      field: 'createdDate',
      title: 'Written',
      description: '',
      type: 'datetime',
      format: 'dateTime',
      unit: null,
      order: 10,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
    {
      field: 'requests',
      title: 'Requests',
      description: '',
      type: 'integer',
      format: 'integer',
      unit: null,
      order: 20,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
  ],
}

async function stub(page: Page, landlord = true) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await page.route('**/api/Me', (route) => route.fulfill({ json: { ...me, landlord } }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/GridModel', (route) =>
    route.fulfill({ json: ['HttpProcessingCounter'] }),
  )
  await page.route('**/api/GridModel/HttpProcessingCounter', (route) =>
    route.fulfill({ json: model }),
  )
  await page.route('**/api/HttpProcessingCounter**', (route) =>
    route.fulfill({
      json: {
        rows: [{ createdDate: '2026-10-21T09:00:00Z', requests: 4242 }],
        total: 1,
        statistics: { columns: {} },
      },
    }),
  )
}

async function inMode(page: Page, name: string): Promise<void> {
  await page.getByTestId('account-button').click()

  if (name === 'arabic') await setLanguage(page, 'ar')
  else await setTheme(page, name === 'dark' ? 'dark' : 'light')

  await page.keyboard.press('Escape')
}

const modes = ['light', 'dark', 'arabic']

test.describe('Performance hub', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('opens from the menu on the family cards with their counts', async ({ page }) => {
    await stub(page)
    await page.goto('/ask-jooby')

    await page.getByText('Administration').first().click()
    await page.getByRole('link', { name: 'Performance', exact: true }).click()

    await expect(page).toHaveURL(/\/administration\/performance$/)
    await expect(page.getByTestId('beat-families')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('perf-families').locator('.card')).toHaveCount(6)
    await expect(page.getByTestId('perf-pill-Engine')).toHaveText('12 counters')
    await expect(page.getByTestId('perf-pill-Telemetry')).toHaveText('3 counters')
  })

  test('drills family then counter into the grid and the address follows', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/performance')

    await page.getByTestId('perf-open-Engine').click()

    await expect(page).toHaveURL(/path=family:Engine$/)
    await expect(page.getByTestId('perf-areas').locator('.card')).toHaveCount(12)
    await expect(page.getByTestId('receipt-families')).toBeVisible()

    await page.getByTestId('perf-open-HttpProcessingCounter').click()

    await expect(page).toHaveURL(/path=family:Engine,area:HttpProcessingCounter$/)
    await expect(page.getByTestId('payload-grid-HttpProcessingCounter')).toBeVisible()
    await expect(page.getByTestId('receipt-family:Engine')).toContainText('12 counters')
    await expect(page.locator('.tabulator-row').first()).toContainText('4,242')
  })

  test('receipts go back one beat at a time', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/performance?path=family:Engine,area:HttpProcessingCounter')

    await page.getByTestId('receipt-family:Engine').click()

    await expect(page.getByTestId('perf-areas')).toBeVisible()
    await expect(page).toHaveURL(/path=family:Engine$/)

    await page.getByTestId('receipt-families').click()

    await expect(page.getByTestId('perf-families')).toBeVisible()
    await expect(page).not.toHaveURL(/path=/)
  })

  test('a deep link rebuilds the story and survives a reload', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/performance?path=family:Engine,area:HttpProcessingCounter')

    await expect(page.getByTestId('beat-area:HttpProcessingCounter')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('receipt-families')).toBeVisible()

    await page.reload()

    await expect(page.getByTestId('payload-grid-HttpProcessingCounter')).toBeVisible()
  })

  test('browser back returns to the previous beat', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/performance')
    await page.getByTestId('perf-open-Logs').click()
    await expect(page.getByTestId('perf-areas')).toBeVisible()

    await page.goBack()

    await expect(page.getByTestId('perf-families')).toBeVisible()
  })

  test('an unknown address falls back to the families', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/performance?path=family:Nope,area:Nothing')

    await expect(page.getByTestId('perf-families')).toBeVisible()
  })

  test('says so when the user is not the landlord', async ({ page }) => {
    await stub(page, false)
    await page.goto('/administration/performance')

    await expect(page.getByTestId('perf-denied')).toBeVisible()
  })

  for (const name of modes) {
    test(`has no accessibility violations in ${name} across the three beats`, async ({ page }) => {
      await stub(page)

      await page.goto('/administration/performance')
      await expect(page.getByTestId('story')).toBeVisible()
      await inMode(page, name)

      for (const path of [
        '',
        '?path=family:Engine',
        '?path=family:Engine,area:HttpProcessingCounter',
      ]) {
        await page.evaluate((target) => {
          window.history.pushState({}, '', `/administration/performance${target}`)
          window.dispatchEvent(new PopStateEvent('popstate'))
        }, path)
        await expect(page.getByTestId('story')).toBeVisible()
        await settleAnimations(page)

        const results = await new AxeBuilder({ page })
          .include('[data-testid="adminPerformance-page"]')
          .analyze()

        expect(
          results.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90))}`),
        ).toEqual([])
      }
    })
  }

  test('fits a phone without sideways scrolling', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 })
    await stub(page)

    for (const path of ['', '?path=family:Infrastructure']) {
      await page.goto(`/administration/performance${path}`)
      await expect(page.getByTestId('story')).toBeVisible()
      await settleAnimations(page)

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
      )

      expect(overflow).toBe(false)
    }
  })
})
