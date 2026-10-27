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

const guid = '11111111-1111-1111-1111-111111111111'

const baseMe = {
  userName: 'alex',
  landlord: false,
  permissions: [2],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

interface Put {
  path: string
  body: Record<string, unknown>
}

interface Options {
  permissions?: number[]
  putStatus?: number
  puts?: Put[]
  keysStatus?: number
  emptyKeys?: boolean
}

async function stub(page: Page, options: Options = {}) {
  const puts = options.puts ?? []
  const putStatus = options.putStatus ?? 200
  const models = [
    {
      name: 'Payments',
      entityAnalysisModelGuid: guid,
      hasOverride: false,
      overrideKind: 0,
      deleteExpiryDate: null,
    },
  ]
  const rules = [
    {
      name: 'High value',
      hasOverride: true,
      entityAnalysisModelActivationRuleOverrideId: 7,
      entityAnalysisModelGuid: guid,
      enableForce: true,
      overrideKind: 0,
      deleteExpiryDate: null,
    },
  ]

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...baseMe, permissions: options.permissions ?? [2] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityAnalysisModelRequestXPath/ByOverrideKey', (route) =>
    route.fulfill({ json: [{ name: 'AccountId' }] }),
  )
  await page.route('**/api/GetEntityAnalysisModelOverrideQuery/Keys', (route) =>
    options.keysStatus
      ? route.fulfill({ status: options.keysStatus, json: {} })
      : route.fulfill({
          json: options.emptyKeys
            ? []
            : [
                {
                  overrideKey: 'AccountId',
                  enabledOnModels: 1,
                  values: 2,
                  overrides: 3,
                  forced: 1,
                  activationRules: 2,
                  allActivationRules: 4,
                  nextExpiryDate: null,
                  lastCreatedDate: '2026-10-20T10:00:00Z',
                },
              ],
        }),
  )
  await page.route('**/api/GetEntityAnalysisModelOverrideQuery/Values**', (route) =>
    route.fulfill({
      json: [
        {
          overrideKeyValue: 'ACC-1',
          name: 'Payments',
          entityAnalysisModelGuid: guid,
          overrideKind: 1,
          createdUser: 'Administrator',
          createdDate: '2026-10-20T10:00:00Z',
          deleteExpiryDate: null,
        },
      ],
    }),
  )
  await page.route('**/api/GetEntityAnalysisModelOverrideQuery?**', (route) =>
    route.fulfill({ json: models }),
  )
  await page.route('**/api/GetEntityAnalysisModelActivationRuleOverrideQuery**', (route) =>
    route.fulfill({ json: rules }),
  )
  await page.route('**/api/EntityAnalysisModelOverride**', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback()

    const body = route.request().postDataJSON() as Record<string, unknown>

    puts.push({ path: new URL(route.request().url()).pathname, body })

    return route.fulfill({
      status: putStatus,
      json:
        putStatus === 200
          ? {}
          : { errors: [{ propertyName: 'x', errorMessage: 'Not allowed by the server.' }] },
    })
  })
  await page.route('**/api/EntityAnalysisModelActivationRuleOverride**', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback()

    puts.push({
      path: new URL(route.request().url()).pathname,
      body: route.request().postDataJSON() as Record<string, unknown>,
    })

    return route.fulfill({ status: putStatus, json: {} })
  })
}

const modes = {
  light: (page: Page) => setTheme(page, 'light'),
  dark: (page: Page) => setTheme(page, 'dark'),
  arabic: (page: Page) => setLanguage(page, 'ar'),
}

test.describe('Override page', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('is the Override entry in the menu and opens the keys', async ({ page }) => {
    await stub(page)
    await page.goto('/ask-jooby')

    await page
      .getByRole('navigation', { name: 'Main navigation' })
      .getByRole('link', { name: 'Override' })
      .click()

    await expect(page).toHaveURL(/\/override$/)
    await expect(page).toHaveTitle(/Override/)
    await expect(page.getByTestId('beat-keys')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('override-page')).toContainText('AccountId')
  })

  test('drills by clicking and the address follows each step', async ({ page }) => {
    await stub(page)
    await page.goto('/override')

    await page.getByTestId('grid-action-open').click()

    await expect(page).toHaveURL(/key=AccountId$/)
    await expect(page.getByTestId('receipt-keys')).toBeVisible()
    await expect(page.getByTestId('receipt-keys')).toBeVisible()
    await expect(page.getByTestId('beat-key:AccountId')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-key:AccountId')).toContainText('ACC-1')

    await page.getByTestId('grid-action-open').click()

    await expect(page).toHaveURL(/key=AccountId&value=ACC-1$/)
    await expect(page.getByTestId('receipt-key:AccountId')).toContainText('2 values')
    await expect(page.getByTestId('receipt-key:AccountId')).toContainText('1 forced')
    await expect(page.locator('[aria-label="Models"] .tabulator-row')).toHaveCount(1)
  })

  test('a deep link rebuilds the drill and expands the named model', async ({ page }) => {
    await stub(page)
    await page.goto(`/override?key=AccountId&value=ACC-1&model=${guid}`)

    await expect(page.getByTestId('beat-value:ACC-1')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('receipt-keys')).toBeVisible()
    await expect(page.getByTestId('receipt-key:AccountId')).toBeVisible()
    await expect(page.getByTestId(`override-rules-${guid}`).locator('.tabulator-row')).toHaveCount(
      1,
    )
  })

  test('expanding a model is written to the address without a history entry', async ({ page }) => {
    await stub(page)
    await page.goto('/override')
    await page.getByTestId('grid-action-open').click()
    await page.getByTestId('grid-action-open').click()
    await page.getByTestId('grid-expand').click()

    await expect(page).toHaveURL(new RegExp(`model=${guid}`))

    await page.goBack()

    await expect(page).toHaveURL(/key=AccountId$/)
  })

  test('the browser Back and Forward buttons climb and descend the trail', async ({ page }) => {
    await stub(page)
    await page.goto('/override')
    await page.getByTestId('grid-action-open').click()
    await page.getByTestId('grid-action-open').click()

    await expect(page.getByTestId('beat-value:ACC-1')).toHaveAttribute('data-state', 'working')

    await page.goBack()

    await expect(page).toHaveURL(/key=AccountId$/)
    await expect(page.getByTestId('beat-key:AccountId')).toHaveAttribute('data-state', 'working')

    await page.goBack()

    await expect(page.getByTestId('beat-keys')).toHaveAttribute('data-state', 'working')

    await page.goForward()
    await page.goForward()

    await expect(page).toHaveURL(/key=AccountId&value=ACC-1$/)
    await expect(page.getByTestId('beat-value:ACC-1')).toHaveAttribute('data-state', 'working')
  })

  test('a receipt and the trail reopen an earlier beat and drop what is below', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/override?key=AccountId&value=ACC-1')

    await page.getByTestId('receipt-key:AccountId').click()

    await expect(page).toHaveURL(/key=AccountId$/)
    await expect(page.getByTestId('beat-value:ACC-1')).toHaveCount(0)

    await page.getByTestId('crumb-keys').click()

    await expect(page).toHaveURL(/\/override$/)
    await expect(page.getByTestId('beat-keys')).toHaveAttribute('data-state', 'working')
  })

  test('the selector looks a value up exactly', async ({ page }) => {
    await stub(page)
    await page.goto('/override')
    await page.getByTestId('override-key').selectOption('AccountId')
    await page.getByTestId('override-value').fill('NEW-9')
    await page.getByTestId('override-fetch').click()

    await expect(page).toHaveURL(/key=AccountId&value=NEW-9$/)
    await expect(page.getByTestId('beat-value:NEW-9')).toHaveAttribute('data-state', 'working')
  })

  test('a change is sent and a refusal reverts the switch with the server reason', async ({
    page,
  }) => {
    const puts: Put[] = []

    await stub(page, { puts, putStatus: 400 })
    await page.goto('/override?key=AccountId&value=ACC-1')

    const toggle = page.locator('[aria-label="Models"] .tabulator-row input[type=checkbox]').first()

    await toggle.check()

    await expect.poll(() => puts.length).toBe(1)
    expect(puts[0]!.body).toMatchObject({ overrideKey: 'AccountId', overrideKeyValue: 'ACC-1' })
    await expect(page.getByTestId('override-problems')).toContainText('Not allowed by the server.')
    await expect(toggle).not.toBeChecked()
  })

  test('without the Force permission Force is not offered and the page says why', async ({
    page,
  }) => {
    await stub(page, { permissions: [2] })
    await page.goto(`/override?key=AccountId&value=ACC-1&model=${guid}`)

    await expect(page.getByTestId('override-no-force')).toBeVisible()
    await expect(
      page.getByTestId(`override-rules-${guid}`).locator('select').first().locator('option'),
    ).toHaveText(['Suppress'])
  })

  test('with the Force permission Force is offered where the rule allows it', async ({ page }) => {
    await stub(page, { permissions: [2, 62] })
    await page.goto(`/override?key=AccountId&value=ACC-1&model=${guid}`)

    await expect(page.getByTestId('override-no-force')).toHaveCount(0)
    await expect(
      page.getByTestId(`override-rules-${guid}`).locator('select').first().locator('option'),
    ).toHaveText(['Suppress', 'Force'])
  })

  test('says so when the person may not manage overrides', async ({ page }) => {
    await stub(page, { permissions: [] })
    await page.goto('/override')

    await expect(page.getByTestId('override-denied')).toContainText('do not have permission')
    await expect(page.getByTestId('override-manager')).toHaveCount(0)
  })

  test('shows the empty state when no key offers overrides', async ({ page }) => {
    await stub(page, { emptyKeys: true })
    await page.goto('/override')

    await expect(page.getByTestId('override-page')).toContainText('No Request XPath')
  })

  test('shows the error state when the keys cannot be loaded', async ({ page }) => {
    await stub(page, { keysStatus: 500 })
    await page.goto('/override')

    await expect(page.getByRole('alert').first()).toContainText('could not be loaded')
  })

  test('has no page-level horizontal scroll on a phone', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 })
    await stub(page)
    await page.goto(`/override?key=AccountId&value=ACC-1&model=${guid}`)
    await expect(page.getByTestId('beat-value:ACC-1')).toBeVisible()
    await settleAnimations(page)

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    )

    expect(overflow).toBe(false)
  })

  for (const mode of ['light', 'dark', 'arabic'] as const) {
    test(`has no accessibility violations in ${mode}`, async ({ page }) => {
      await stub(page, { permissions: [2, 62] })
      await page.goto(`/override?key=AccountId&value=ACC-1&model=${guid}`)
      await expect(page.getByTestId(`override-rules-${guid}`)).toBeVisible()
      await page.getByTestId('account-button').click()

      await modes[mode](page)

      await page.keyboard.press('Escape')
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="override-page"]')
        .analyze()

      expect(
        results.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90))}`),
      ).toEqual([])
    })
  }
})
