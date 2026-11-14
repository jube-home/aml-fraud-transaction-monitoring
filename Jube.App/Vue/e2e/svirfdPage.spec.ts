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
import {svirfdLookup, wireSchema} from './svirfdStub'
import {setLanguage, setTheme, settleAnimations} from './support'

const style = {
  version: 8,
  sources: {},
  layers: [{id: 'background', type: 'background', paint: {'background-color': '#dde3e8'}}],
}

async function stub(
  page: Page,
  options: {
    permissions?: number[]
    enabled?: boolean
    status?: number
    body?: unknown
    state?: string
    tenantRemote?: boolean
  } = {},
) {
  const requests: string[] = []

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await page.route('**/api/Me', (route) =>
    route.fulfill({
      json: {
        ...me,
        permissions: options.permissions ?? [72],
        svirfd: options.enabled ?? true,
      },
    }),
  )
  await page.route('**/api/WireSchema**', (route) => route.fulfill({json: wireSchema}))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('https://tiles.openfreemap.org/**', (route) => route.fulfill({json: style}))
  await page.route('**/api/Svirfd/Status', (route) =>
    route.fulfill({
      json: {
        enabled: true,
        ready: true,
        mode: 'Remote',
        seq: -1,
        remoteEngineLookup: true,
        tenantRemoteLookupsEnabled: options.tenantRemote ?? true,
        attribution: 'Data: svirfd.com',
        licenceUrl: 'https://svirfd.com/licence',
        state: options.state ?? 'LocalReady',
        lastError: 'SvirfdRateLimitedException: slow down',
        nextRetryAt: new Date(Date.now() + 5 * 60_000).toISOString(),
        stalenessSeconds: null,
      },
    }),
  )
  await page.route('**/api/Svirfd/Lookup/**', (route) => {
    requests.push(decodeURIComponent(new URL(route.request().url()).pathname.split('/').pop()!))

    return route.fulfill({status: options.status ?? 200, json: options.body ?? svirfdLookup})
  })

  return requests
}

test.describe('Svirfd page', () => {
  test.use({viewport: {width: 1400, height: 1000}})

  test('is reached from the menu and asks nothing until there is an address', async ({page}) => {
    const requests = await stub(page)

    await page.goto('/sanctions')
    const link = page.getByRole('link', {name: 'Svirfd'}).first()

    await expect(link).toHaveAttribute('href', '/svirfd')
    await link.dispatchEvent('click')

    await expect(page).toHaveURL(/\/svirfd$/)
    await expect(page.getByTestId('svirfd-page')).toBeVisible()
    await expect(page.getByTestId('svirfd-prompt')).toBeVisible()
    await expect(page.getByTestId('svirfd-check')).toBeDisabled()
    expect(requests).toHaveLength(0)
  })

  test('looks up from the form and writes the address to the location', async ({page}) => {
    const requests = await stub(page)

    await page.goto('/svirfd')
    await page.getByTestId('svirfd-text').fill('8.8.8.8')
    await page.getByTestId('svirfd-check').click()

    await expect(page.getByTestId('svirfd-record')).toBeVisible()
    await expect(page.getByTestId('svirfd-ip')).toHaveText('8.8.8.8')
    await expect(page).toHaveURL(/\/svirfd\?ip=8\.8\.8\.8$/)
    expect(requests).toEqual(['8.8.8.8'])
  })

  test('looks up on load from a deep link and shows the record, map and credit', async ({
                                                                                          page,
                                                                                        }) => {
    const requests = await stub(page)

    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-record')).toBeVisible()
    await expect(page.getByTestId('svirfd-text')).toHaveValue('8.8.8.8')
    await expect(page.getByTestId('svirfd-headline')).toContainText('Google LLC')
    await expect(page.getByTestId('svirfd-map')).toBeVisible()
    await expect(page.getByTestId('svirfd-narrative-identity')).toContainText(
      'belongs to Google LLC',
    )
    await expect(page.getByTestId('svirfd-attribution')).toContainText('svirfd.com')
    await expect(page.getByTestId('svirfd-section-privacy')).toBeVisible()
    await expect(page.getByTestId('svirfd-fact-Privacy.Proxy')).toContainText('Not claimed')
    expect(requests).toEqual(['8.8.8.8'])
  })

  test('refuses an address that is not an address without asking the server', async ({page}) => {
    const requests = await stub(page)

    await page.goto('/svirfd')
    await page.getByTestId('svirfd-text').fill('123.456.789.200')

    await expect(page.getByTestId('svirfd-check')).toBeDisabled()
    await expect(page.getByText('That is not an IP address.')).toBeVisible()
    expect(requests).toHaveLength(0)
  })

  test('says so when the service is rate limiting', async ({page}) => {
    await stub(page, {status: 429, body: {}})
    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-error')).toContainText('rate limiting')
    await expect(page.getByTestId('svirfd-record')).toHaveCount(0)
  })

  test('says so when the server forbids the lookup', async ({page}) => {
    await stub(page, {status: 403, body: {}})
    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-error')).toContainText('do not have permission')
  })

  test('says when it is answering from the public API while the local index retries', async ({
                                                                                               page,
                                                                                             }) => {
    await stub(page, {state: 'FellBackToRemote'})
    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-state')).toContainText('public API')
    await expect(page.getByTestId('svirfd-record')).toBeVisible()
  })

  test('tells a tenant its remote lookups are switched off and blocks the check', async ({
                                                                                           page,
                                                                                         }) => {
    const requests = await stub(page, {tenantRemote: false})
    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-tenant-remote-off')).toBeVisible()
    await expect(page.getByTestId('svirfd-status-tenant-remote')).toHaveText('No')
    await expect(page.getByTestId('svirfd-check')).toBeDisabled()
    expect(requests).toHaveLength(0)
  })

  test('shows no switched-off notice when the tenant allows remote lookups', async ({page}) => {
    await stub(page)
    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-record')).toBeVisible()
    await expect(page.getByTestId('svirfd-tenant-remote-off')).toHaveCount(0)
    await expect(page.getByTestId('svirfd-status-tenant-remote')).toHaveText('Yes')
  })

  test('shows no state notice when the local index is ready', async ({page}) => {
    await stub(page, {state: 'LocalReady'})
    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-record')).toBeVisible()
    await expect(page.getByTestId('svirfd-state')).toHaveCount(0)
  })

  test('shows a not-permitted state and sends no lookup without the permission', async ({
                                                                                          page,
                                                                                        }) => {
    const requests = await stub(page, {permissions: []})

    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-refused')).toBeVisible()
    await expect(page.getByTestId('svirfd-text')).toHaveCount(0)
    expect(requests).toHaveLength(0)
  })

  test('explains that Svirfd is turned off and sends no lookup', async ({page}) => {
    const requests = await stub(page, {enabled: false})

    await page.goto('/svirfd?ip=8.8.8.8')

    await expect(page.getByTestId('svirfd-disabled')).toContainText('EnableSvirfd')
    await expect(page.getByTestId('svirfd-text')).toHaveCount(0)
    expect(requests).toHaveLength(0)
  })

  test('has no page-level horizontal scroll on a phone', async ({page}) => {
    await stub(page)
    await page.setViewportSize({width: 390, height: 800})
    await page.goto('/svirfd?ip=8.8.8.8')
    await expect(page.getByTestId('svirfd-record')).toBeVisible()

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    )

    expect(overflow).toBe(false)
  })

  for (const mode of ['light', 'dark', 'arabic'] as const) {
    test(`has no accessibility violations with a record showing in ${mode}`, async ({page}) => {
      await stub(page)
      await page.goto('/svirfd?ip=8.8.8.8')
      await expect(page.getByTestId('svirfd-record')).toBeVisible()
      await page.getByTestId('account-button').click()

      const apply = {
        light: () => setTheme(page, 'light'),
        dark: () => setTheme(page, 'dark'),
        arabic: () => setLanguage(page, 'ar'),
      }

      await apply[mode]()

      await page.keyboard.press('Escape')
      await expect(page.getByTestId('svirfd-record')).toBeVisible()
      await settleAnimations(page)

      expect((await new AxeBuilder({page}).analyze()).violations).toEqual([])
    })
  }
})
