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

import { expect, test, type Locator, type Page } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { settleAnimations, setTheme, stubRuleBuilder } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [1, 6, 29],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null as string | null,
}

function group(nav: Locator, name: string): Locator {
  return nav.locator('summary').filter({ hasText: new RegExp(`^\\s*${name}\\s*$`) })
}

async function signedIn(page: Page, overrides: Partial<typeof me> = {}): Promise<void> {
  await stubRuleBuilder(page)
  await page.route('**/api/Me', (route) => route.fulfill({ json: { ...me, ...overrides } }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({ json: { id: 1, name: 'Acme' } }),
  )
  await page.route('**/api/TenantRegistry/ByFilter*', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Acme' }] }),
  )
}

test.describe('signed-in shell', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('lands on the welcome page with a side drawer limited to the permissions held', async ({
    page,
  }) => {
    await signedIn(page)
    await page.goto('/tools')

    await expect(page.getByRole('heading', { level: 1, name: 'Tools workbench' })).toBeVisible()

    const nav = page.getByRole('navigation', { name: 'Main navigation' })

    await expect(nav).toBeVisible()
    await expect(nav.getByRole('link')).toHaveText([
      'Ask Jooby',
      'Cases',
      'Models',
      'Sanctions',
      'TEMP: Tools',
    ])
    await expect(nav.getByRole('link', { name: 'Models', exact: true })).toBeVisible()
    await expect(group(nav, 'Models')).toHaveCount(0)
  })

  test('expands a group to reveal only the entries the permissions allow', async ({ page }) => {
    await signedIn(page, { permissions: [35] })
    await page.goto('/tools')

    const nav = page.getByRole('navigation', { name: 'Main navigation' })

    await group(nav, 'Administration').click()
    await expect(nav.getByRole('link')).toHaveText(['Ask Jooby', 'Security', 'TEMP: Tools'])
  })

  test('shows Administration with its landlord-only branches to a landlord', async ({ page }) => {
    await signedIn(page, { landlord: true, permissions: [] })
    await page.goto('/tools')

    const nav = page.getByRole('navigation', { name: 'Main navigation' })

    await group(nav, 'Administration').click()
    await expect(nav.getByRole('link', { name: 'Tenants' })).toBeVisible()
    await expect(nav.getByRole('link', { name: 'Performance' })).toBeVisible()
    await expect(nav.getByRole('link', { name: 'Sanctions loader' })).toBeVisible()
  })

  test('collapses and restores the drawer on a wide screen and remembers the choice', async ({
    page,
  }) => {
    await signedIn(page)
    await page.goto('/tools')

    const nav = page.getByRole('navigation', { name: 'Main navigation' })

    await expect(nav).toBeVisible()
    await page.getByTestId('sidebar-toggle').click()
    await expect(nav).toBeHidden()

    await page.reload()
    await expect(nav).toBeHidden()

    await page.getByTestId('sidebar-toggle').click()
    await expect(nav).toBeVisible()
  })

  test('uses an overlay drawer on a narrow screen', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 })
    await signedIn(page)
    await page.goto('/tools')

    const nav = page.getByRole('navigation', { name: 'Main navigation' })

    await expect(nav).toBeHidden()
    await page.getByTestId('sidebar-toggle').click()
    await expect(nav.getByRole('link', { name: 'Cases' })).toBeVisible()

    await page.keyboard.press('Escape')
    await expect(nav).toBeHidden()

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )

    expect(overflow).toBeLessThanOrEqual(0)
  })

  test('an unauthenticated visit goes to sign-in and returns afterwards', async ({ page }) => {
    await page.route('**/api/Me', (route) => route.fulfill({ status: 401 }))
    await page.goto('/tools')

    await expect(page).toHaveURL(/\/\?redirect=\/tools$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  })

  test('signs out and lands on sign-in', async ({ page }) => {
    await signedIn(page)
    await page.route('**/api/Authentication/Logout', (route) => route.fulfill({ status: 200 }))
    await page.goto('/tools')

    await page.getByTestId('account-button').click()
    await page.getByRole('button', { name: 'Sign out' }).click()

    await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  })

  test('hides Change password when the server does not allow it', async ({ page }) => {
    await signedIn(page, { canChangePassword: false })
    await page.goto('/tools')
    await page.getByTestId('account-button').click()

    await expect(page.getByRole('link', { name: 'Change password' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`no axe violations with the account menu open in ${theme}`, async ({ page }) => {
      await signedIn(page)
      await page.goto('/tools')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
      await settleAnimations(page)

      expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
    })
  }

  test('shows an empty notification centre until the service connects', async ({ page }) => {
    await signedIn(page)
    await page.goto('/tools')
    await page.getByTestId('notifications-button').click()

    await expect(page.getByTestId('notifications-empty')).toContainText('Nothing new')
    await expect(page.getByTestId('unread')).toHaveCount(0)
  })

  test('shows a notification pushed over the event stream and marks it read', async ({ page }) => {
    await signedIn(page)
    await page.route('**/api/ServiceChange/Stream', (route) =>
      route.fulfill({
        contentType: 'text/event-stream',
        body: `retry: 600000\nevent: notification\ndata: ${JSON.stringify({ id: 'n1', title: 'Case assigned', body: 'Case 42 is yours', severity: 'info' })}\n\n`,
      }),
    )
    await page.goto('/tools')

    await expect(page.getByTestId('unread')).toHaveText('1')
    await page.getByTestId('notifications-button').click()
    await page.getByRole('button', { name: /Case assigned/ }).click()
    await expect(page.getByTestId('unread')).toHaveCount(0)
  })

  test('shows the current tenant and searches a long list on the server', async ({ page }) => {
    await signedIn(page, { landlord: true })
    const filters: string[] = []
    await page.route('**/api/TenantRegistry/1', (route) =>
      route.fulfill({ json: { id: 1, name: 'Acme Holdings' } }),
    )
    await page.route('**/api/TenantRegistry/ByFilter*', (route) => {
      const filter =
        new URL(route.request().url()).searchParams.get('filter[filters][0][value]') ?? ''

      filters.push(filter)

      const all = Array.from({ length: 3000 }, (_, i) => ({ id: i + 1, name: `Tenant ${i + 1}` }))

      return route.fulfill({
        json: all.filter((tenant) => tenant.name.toLowerCase().includes(filter.toLowerCase())),
      })
    })
    await page.goto('/tools')

    const trigger = page.getByTestId('tenant-switcher')

    await expect(trigger).toContainText('Acme Holdings')
    await trigger.click()
    await expect(page.getByTestId('tenant-more')).toContainText('first 50')
    await expect(page.getByRole('listbox').getByRole('option')).toHaveCount(50)

    await page.getByTestId('tenant-search').fill('Tenant 2999')
    await expect(page.getByRole('listbox').getByRole('option')).toHaveCount(1)
    expect(filters).toContain('Tenant 2999')

    await page.getByTestId('tenant-search').fill('zzz')
    await expect(page.getByTestId('tenant-none')).toBeVisible()
  })

  test('switches tenant from the search list with the keyboard', async ({ page }) => {
    await signedIn(page, { landlord: true })
    await page.route('**/api/TenantRegistry/1', (route) =>
      route.fulfill({ json: { id: 1, name: 'Acme' } }),
    )
    await page.route('**/api/TenantRegistry/ByFilter*', (route) =>
      route.fulfill({
        json: [
          { id: 1, name: 'Acme' },
          { id: 2, name: 'Globex' },
        ],
      }),
    )
    await page.route('**/api/AntiforgeryToken', (route) =>
      route.fulfill({ json: { token: 'a'.repeat(40) } }),
    )
    let body: unknown
    await page.route('**/api/UserInTenant', (route) => {
      body = route.request().postDataJSON()

      return route.fulfill({ status: 204 })
    })
    await page.goto('/tools')
    await page.getByTestId('tenant-switcher').click()
    await expect(page.getByRole('listbox').getByRole('option')).toHaveCount(2)

    await page.getByTestId('tenant-search').press('ArrowDown')
    await page.getByTestId('tenant-search').press('Enter')
    await expect.poll(() => body).toEqual({ tenantRegistryId: 2 })
  })

  test('does not show the tenant switcher to anyone but a landlord', async ({ page }) => {
    await signedIn(page)
    await page.goto('/tools')

    await expect(page.getByTestId('tenant-switcher')).toHaveCount(0)
  })

  test('warns before the session expires and extends it on request', async ({ page }) => {
    await signedIn(page, { sessionExpiry: new Date(Date.now() + 4 * 60_000).toISOString() })
    let extended = 0
    await page.route('**/api/Session/Extend', (route) => {
      extended += 1

      return route.fulfill({
        status: 204,
        headers: { 'X-Session-Expiry': new Date(Date.now() + 30 * 60_000).toISOString() },
      })
    })
    await page.goto('/tools')

    await expect(page.getByTestId('session-warning')).toContainText('about to expire')
    await page.getByRole('button', { name: 'Extend session' }).click()
    await expect(page.getByTestId('session-warning')).toBeHidden()
    expect(extended).toBeGreaterThanOrEqual(1)
  })

  test('an event stream revocation returns the user to sign-in', async ({ page }) => {
    await signedIn(page)
    await page.route('**/api/ServiceChange/Stream', (route) =>
      route.fulfill({ contentType: 'text/event-stream', body: 'event: revoked\ndata: {}\n\n' }),
    )
    await page.goto('/tools')

    await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
    await expect(page.getByTestId('toasts')).toContainText('session has expired')
  })

  test('keeps the old /home address working by sending it to Ask Jooby', async ({ page }) => {
    await signedIn(page, { permissions: [] })
    await page.route(/\/api\/AskJoobyConversation(\?.*)?$/, (route) => route.fulfill({ json: [] }))
    await page.route('**/api/AskJoobyConversation/StartupStats', (route) =>
      route.fulfill({ json: { bites: 1, trails: 2, saltlicks: 3, joobriks: 4, yokes: 5 } }),
    )
    await page.goto('/home')

    await expect(page).toHaveURL(/\/ask-jooby$/)
    await expect(page.getByTestId('empty-welcome')).toContainText('What brings you in?')
  })

  test('offers Ask Jooby to a user who holds no permissions at all', async ({ page }) => {
    await signedIn(page, { permissions: [] })
    await page.goto('/tools')

    const nav = page.getByRole('navigation', { name: 'Main navigation' })

    await expect(nav.getByRole('link')).toHaveText(['Ask Jooby', 'TEMP: Tools'])
    await expect(page.getByTestId('widget-launcher')).toBeVisible()
  })
})
