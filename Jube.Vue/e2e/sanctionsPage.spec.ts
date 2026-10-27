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

const aggregate = {
  sum: 6.5,
  average: 2.1667,
  count: 3,
  max: 3,
  min: 1,
  first: 1,
  last: 2,
  confidence: 0.8125,
}

const response = {
  entries: [
    {
      id: 11,
      distance: 1,
      value: 'IVAN PETROV',
      sanctionEntrySourceId: 4,
      source: 'OFAC',
      reference: 'SDN-1',
    },
  ],
  aggregations: {
    total: aggregate,
    bySource: [{ sourceId: 4, sourceName: 'OFAC', ...aggregate }],
  },
}

const empty = { entries: [], aggregations: { total: null, bySource: [] } }

async function stub(
  page: Page,
  options: { permissions?: number[]; status?: number; body?: unknown } = {},
) {
  const requests: URLSearchParams[] = []

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [29] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/Invoke/Sanction**', (route) => {
    requests.push(new URL(route.request().url()).searchParams)

    return route.fulfill({ status: options.status ?? 200, json: options.body ?? response })
  })

  return requests
}

test.describe('Sanctions page', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('is reached from the menu and asks nothing until there is text', async ({ page }) => {
    const requests = await stub(page)

    await page.goto('/tools')
    const link = page.getByRole('link', { name: 'Sanctions' }).first()

    await expect(link).toHaveAttribute('href', '/sanctions')
    await link.dispatchEvent('click')

    await expect(page).toHaveURL(/\/sanctions$/)
    await expect(page.getByTestId('sanctions-page')).toBeVisible()
    await expect(page.getByTestId('sanction-prompt')).toBeVisible()
    await expect(page.getByTestId('sanction-check')).toBeDisabled()
    expect(requests).toHaveLength(0)
  })

  test('searches from the form and writes the query to the address', async ({ page }) => {
    const requests = await stub(page)

    await page.goto('/sanctions')
    await page.getByTestId('sanction-text').fill('Ivan Petrov')
    await page.getByTestId('sanction-distance').fill('3')
    await page.getByTestId('sanction-check').click()

    await expect(page.getByTestId('sanction-count')).toHaveText('1 match')
    await expect(page).toHaveURL(/\/sanctions\?q=Ivan\+Petrov&d=3$|q=Ivan%20Petrov&d=3/)
    expect(requests).toHaveLength(1)
    expect(requests[0]!.get('distance')).toBe('3')
  })

  test('searches on load from a deep link and restores the settings', async ({ page }) => {
    const requests = await stub(page)

    await page.goto('/sanctions?q=Ivan&d=4&r=0.5&c=1')

    await expect(page.getByTestId('sanction-count')).toHaveText('1 match')
    await expect(page.getByTestId('sanction-text')).toHaveValue('Ivan')
    await expect(page.getByTestId('sanction-distance-value')).toHaveText('4')
    expect(Object.fromEntries(requests[0]!)).toEqual({
      multiPartString: 'Ivan',
      distance: '4',
      maxDistanceRatio: '0.5',
      maxCoverageRatio: '1',
    })
  })

  test('back and forward follow the searches, back to an empty form included', async ({ page }) => {
    await stub(page)

    await page.goto('/sanctions')
    await page.getByTestId('sanction-text').fill('Ivan')
    await page.getByTestId('sanction-check').click()
    await expect(page.getByTestId('sanction-count')).toBeVisible()
    await page.getByTestId('sanction-text').fill('Olga')
    await page.getByTestId('sanction-check').click()
    await expect(page).toHaveURL(/q=Olga/)

    await page.goBack()
    await expect(page.getByTestId('sanction-text')).toHaveValue('Ivan')
    await expect(page).toHaveURL(/q=Ivan/)

    await page.goBack()
    await expect(page.getByTestId('sanction-text')).toHaveValue('')
    await expect(page.getByTestId('sanction-prompt')).toBeVisible()

    await page.goForward()
    await expect(page.getByTestId('sanction-text')).toHaveValue('Ivan')
    await expect(page.getByTestId('sanction-count')).toBeVisible()
  })

  test('explains zero matches as a search that ran', async ({ page }) => {
    await stub(page, { body: empty })
    await page.goto('/sanctions?q=Nobody')

    await expect(page.getByTestId('sanction-zero')).toContainText('nothing matched')
    await expect(page.getByTestId('sanction-error')).toHaveCount(0)
  })

  test('shows a refused search as an error, not an empty result', async ({ page }) => {
    await stub(page, {
      status: 400,
      body: { errors: [{ propertyName: 'MultiPartString', errorMessage: 'Too long.' }] },
    })
    await page.goto('/sanctions?q=x')

    await expect(page.getByTestId('sanction-error')).toContainText('The search was refused:')
    await expect(page.getByTestId('sanction-error')).toContainText('Too long.')
    await expect(page.getByTestId('sanction-zero')).toHaveCount(0)
  })

  test('says so when the server forbids the search', async ({ page }) => {
    await stub(page, { status: 403, body: {} })
    await page.goto('/sanctions?q=x')

    await expect(page.getByTestId('sanction-error')).toContainText(
      'You do not have permission to search sanctions.',
    )
  })

  test('says so plainly when the search fails', async ({ page }) => {
    await stub(page, { status: 500, body: {} })
    await page.goto('/sanctions?q=x')

    await expect(page.getByTestId('sanction-error')).toContainText('could not be completed')
  })

  test('shows a not-permitted state and sends no search without the permission', async ({
    page,
  }) => {
    const requests = await stub(page, { permissions: [] })

    await page.goto('/sanctions?q=Ivan')

    await expect(page.getByTestId('sanctions-refused')).toContainText(
      'You do not have permission to search sanctions.',
    )
    await expect(page.getByTestId('sanction-text')).toHaveCount(0)
    expect(requests).toHaveLength(0)
  })

  test('has no page-level horizontal scroll on a phone', async ({ page }) => {
    await stub(page)
    await page.setViewportSize({ width: 390, height: 800 })
    await page.goto('/sanctions?q=Ivan')
    await expect(page.getByTestId('sanction-count')).toBeVisible()

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    )

    expect(overflow).toBe(false)
  })

  for (const mode of ['light', 'dark', 'arabic'] as const) {
    test(`has no accessibility violations with results showing in ${mode}`, async ({ page }) => {
      await stub(page)
      await page.goto('/sanctions?q=Ivan')
      await expect(page.getByTestId('sanction-count')).toBeVisible()
      await page.getByTestId('account-button').click()

      const apply = {
        light: () => setTheme(page, 'light'),
        dark: () => setTheme(page, 'dark'),
        arabic: () => setLanguage(page, 'ar'),
      }

      await apply[mode]()

      await page.keyboard.press('Escape')
      await expect(page.getByTestId('sanction-count')).toBeVisible()
      await settleAnimations(page)

      expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
    })
  }
})
