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
import { setLanguage, setTheme, settleAnimations } from './support'

const activation = (overrides: Record<string, unknown> = {}) => ({
  id: 1,
  key: 'AccountId',
  keyValue: 'ACC-1',
  latitude: 51.5,
  longitude: -0.12,
  activationRuleSummary: 'High value',
  responseElevation: 5,
  responseElevationContent: 'Review',
  backColor: '#cc6e38',
  foreColor: '#ffffff',
  createdDate: '2026-10-21T10:00:00Z',
  ...overrides,
})

const london = activation()
const sydney = activation({
  id: 2,
  latitude: -33.87,
  longitude: 151.21,
  createdDate: '2026-10-21T10:30:00Z',
})

const style = {
  version: 8,
  sources: {},
  layers: [{ id: 'background', type: 'background', paint: { 'background-color': '#dde3e8' } }],
}

interface Options {
  permissions?: number[]
  streamed?: unknown[]
  replayStatus?: number
  replayed?: unknown[]
}

async function stub(page: Page, options: Options = {}) {
  const replays: URLSearchParams[] = []

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [30] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('https://tiles.openfreemap.org/**', (route) => route.fulfill({ json: style }))
  await page.route('**/api/Watcher/Stream', (route) =>
    route.fulfill({
      contentType: 'text/event-stream',
      body:
        'retry: 600000\n\n' +
        (options.streamed ?? [london, sydney])
          .map((item) => `event: activation\ndata: ${JSON.stringify(item)}\n\n`)
          .join(''),
    }),
  )
  await page.route('**/api/ActivationWatcher/Replay**', (route) => {
    replays.push(new URL(route.request().url()).searchParams)

    return options.replayStatus
      ? route.fulfill({ status: options.replayStatus, json: {} })
      : route.fulfill({ json: options.replayed ?? [] })
  })
  await page.route('**/api/Chart/Watcher/ActivationSummary', (route) =>
    route.fulfill({
      contentType: 'image/svg+xml',
      body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10"/></svg>',
    }),
  )

  return { replays }
}

test.describe('Watcher page', () => {
  test.use({ viewport: { width: 1500, height: 1100 } })

  test('is the Watcher entry in the menu and plots the stream', async ({ page }) => {
    await stub(page)
    await page.goto('/watcher')

    const link = page
      .getByRole('navigation', { name: 'Main navigation' })
      .getByRole('link', { name: 'Watcher' })

    await expect(link).toHaveAttribute('href', '/watcher')
    await link.click()

    await expect(page).toHaveURL(/\/watcher$/)
    await expect(page).toHaveTitle(/Watcher/)
    await expect(page.getByTestId('watcher-counts')).toContainText('Received 2')
    await expect(page.getByTestId('activation-map')).toHaveAttribute('data-points', '1')
  })

  test('keeps the view in the address and restores it from a link', async ({ page }) => {
    await stub(page)
    await page.goto('/watcher')
    await page.getByTestId('watcher-mode-all').check()
    await page.getByTestId('watcher-window').selectOption('15')
    await page.getByTestId('watcher-heat').check()

    await expect(page).toHaveURL(/mode=all/)
    await expect(page).toHaveURL(/window=15/)
    await expect(page).toHaveURL(/heat=1/)

    const link = page.url()

    await page.goto('/watcher')
    await page.goto(link)

    await expect(page.getByTestId('watcher-mode-all')).toBeChecked()
    await expect(page.getByTestId('watcher-window')).toHaveValue('15')
    await expect(page.getByTestId('watcher-heat')).toBeChecked()
  })

  test('a filter goes in the address, back removes it, forward restores it', async ({ page }) => {
    await stub(page)
    await page.goto('/watcher')
    await page.getByTestId('watcher-page-key').fill('AccountId')
    await page.getByTestId('watcher-page-value').fill('ACC-1')
    await page.getByTestId('watcher-page-apply').click()

    await expect(page).toHaveURL(/key=AccountId&value=ACC-1/)
    await expect(page.getByTestId('watcher-page-filtered')).toHaveText('AccountId:ACC-1')

    await page.goBack()

    await expect(page).not.toHaveURL(/key=/)
    await expect(page.getByTestId('watcher-page-filtered')).toHaveCount(0)
    await expect(page.getByTestId('watcher-page-key')).toHaveValue('')

    await page.goForward()

    await expect(page.getByTestId('watcher-page-filtered')).toHaveText('AccountId:ACC-1')

    await page.getByTestId('watcher-page-clear').click()

    await expect(page).not.toHaveURL(/key=/)
  })

  test('a replay window in the address is replayed on arrival and written back by Replay', async ({
    page,
  }) => {
    const seen = await stub(page, { streamed: [], replayed: [london] })

    await page.goto('/watcher?live=0&from=2026-10-21T10:00&to=2026-10-21T11:00')

    await expect(page.getByTestId('activation-map')).toHaveAttribute('data-points', '1')
    expect(seen.replays).toHaveLength(1)
    expect(seen.replays[0]!.get('dateFrom')).toBe(new Date('2026-10-21T10:00').toISOString())
    await expect(page.getByTestId('watcher-live')).not.toBeChecked()

    await page.getByTestId('watcher-from').fill('2026-10-21T12:00')
    await page.getByTestId('watcher-to').fill('2026-10-21T13:00')
    await page.getByTestId('watcher-replay').click()

    await expect(page).toHaveURL(/from=2026-10-21T12(:|%3A)00&to=2026-10-21T13(:|%3A)00/)
  })

  test('shows the not permitted state without the permission and never opens the stream', async ({
    page,
  }) => {
    let streams = 0

    await stub(page, { permissions: [] })
    await page.route('**/api/Watcher/Stream', (route) => {
      streams += 1

      return route.abort()
    })
    await page.goto('/watcher')

    await expect(page.getByTestId('watcher-page-refused')).toContainText('do not have permission')
    await expect(page.getByTestId('activation-watcher')).toHaveCount(0)
    expect(streams).toBe(0)
  })

  test('never reports streaming when the server refuses the stream, and keeps the page usable', async ({
    page,
  }) => {
    await stub(page)
    await page.route('**/api/Watcher/Stream', (route) => route.fulfill({ status: 403, json: {} }))
    await page.goto('/watcher')

    await expect(page.getByTestId('watcher-status')).toContainText('Connecting')
    await expect(page.getByTestId('watcher-status')).not.toContainText('Streaming')
    await expect(page.getByTestId('watcher-counts')).toBeVisible()
  })

  test('says so when a replay fails and keeps the page usable', async ({ page }) => {
    await stub(page, { replayStatus: 500 })
    await page.goto('/watcher?from=2026-10-21T10:00&to=2026-10-21T11:00')

    await expect(
      page.getByRole('alert').filter({ hasText: 'replay could not be loaded' }),
    ).toBeVisible()
    await expect(page.getByTestId('watcher-counts')).toBeVisible()
  })

  test('has no page-level horizontal scroll at phone width', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 })
    await stub(page)
    await page.goto('/watcher')
    await expect(page.getByTestId('watcher-counts')).toBeVisible()

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )

    expect(overflow).toBeLessThanOrEqual(0)
  })

  const appearances = {
    light: (page: Page) => setTheme(page, 'light'),
    dark: (page: Page) => setTheme(page, 'dark'),
    arabic: (page: Page) => setLanguage(page, 'ar'),
  }

  for (const [mode, apply] of Object.entries(appearances)) {
    test(`has no accessibility violations in ${mode}`, async ({ page }) => {
      await stub(page)
      await page.goto('/watcher')
      await page.getByTestId('account-button').click()

      await apply(page)

      await page.keyboard.press('Escape')
      await expect(page.getByTestId('watcher-counts')).toBeVisible()
      await settleAnimations(page)

      const result = await new AxeBuilder({ page })
        .include('[data-testid="watcher-page"]')
        .exclude('[data-testid="activation-map"]')
        .analyze()

      expect(
        result.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90))}`),
      ).toEqual([])
    })
  }
})
