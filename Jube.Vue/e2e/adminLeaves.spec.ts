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
import { modelOf, stubFormModels } from './formModels'
import { me } from './hierarchyStub'
import { setLanguage, setTheme, settleAnimations } from './support'

async function stub(page: Page, overrides: Record<string, unknown> = {}) {
  await page.route('**/api/Me', (route) =>
    route.fulfill({
      json: { ...me, landlord: true, permissions: [31, 34, 35, 36, 38], ...overrides },
    }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/TenantRegistry/ByFilter*', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Tenant one' }] }),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({ json: { id: 1, name: 'Tenant one' } }),
  )
  await page.route('**/api/SanctionEntrySource', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/Preservation/Summary', (route) =>
    route.fulfill({ json: { totalBytes: 0, minimumPasswordLength: 12, sections: [] } }),
  )
  await page.route('**/api/PreservationSnapshot**', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/OpenTelemetry*', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Orders' }] }),
  )
  await stubFormModels(page, [
    modelOf({
      area: 'OpenTelemetryLogCounter',
      fields: [{ name: 'name', type: 'string' }],
    }),
    modelOf({ area: 'OpenTelemetryExclude', fields: [{ name: 'name', type: 'string' }] }),
  ])
}

test.describe('Administration menu', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('opens as one level of routed pages', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/preservation?tab=move')

    for (const name of [
      'Tenants',
      'Security',
      'Performance',
      'Visualisation setup',
      'Telemetry settings',
      'Sanctions loader',
      'Preservation',
    ]) {
      await expect(page.getByRole('link', { name, exact: true })).toBeVisible()
    }
  })

  test('hides the landlord-only pages from a tenant user', async ({ page }) => {
    await stub(page, { landlord: false })
    await page.goto('/administration/preservation?tab=move')

    await expect(page.getByRole('link', { name: 'Security', exact: true })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Tenants', exact: true })).toHaveCount(0)
    await expect(page.getByRole('link', { name: 'Performance', exact: true })).toHaveCount(0)
    await expect(page.getByRole('link', { name: 'Telemetry settings', exact: true })).toHaveCount(0)
    await expect(page.getByRole('link', { name: 'Sanctions loader', exact: true })).toHaveCount(0)
  })
})

test.describe('Preservation tabs', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('switches between snapshots and import and export and keeps the choice in the address', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/administration/preservation')
    await expect(page.getByTestId('tab-snapshots')).toHaveAttribute('aria-selected', 'true')
    await page.getByTestId('tab-move').click()
    await expect(page).toHaveURL(/tab=move/)
    await expect(page.getByTestId('preservation-move')).toBeVisible()
    await page.reload()
    await expect(page.getByTestId('tab-move')).toHaveAttribute('aria-selected', 'true')
  })

  test('sends the old preservation addresses to their tab', async ({ page }) => {
    await stub(page)
    await page.goto('/preservation/move')
    await expect(page).toHaveURL(/\/administration\/preservation\?tab=move/)
    await page.goto('/preservation/snapshots')
    await expect(page).toHaveURL(/\/administration\/preservation\?tab=snapshots/)
  })
})

test.describe('Telemetry settings', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('switches between log counters and exclusions and keeps the choice in the address', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/administration/telemetry')
    await expect(page.getByTestId('tab-OpenTelemetryLogCounter')).toHaveAttribute(
      'aria-selected',
      'true',
    )
    await page.getByTestId('tab-OpenTelemetryExclude').click()
    await expect(page).toHaveURL(/area=OpenTelemetryExclude/)
    await page.reload()
    await expect(page.getByTestId('tab-OpenTelemetryExclude')).toHaveAttribute(
      'aria-selected',
      'true',
    )
  })
})

test.describe('Sanctions loader page', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('shows the loader to a landlord', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/sanctions-loader')
    await expect(page.getByTestId('sanctions-loader')).toBeVisible()
  })
})

test.describe('Administration leaf pages accessibility', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  for (const path of [
    '/administration/preservation?tab=move',
    '/administration/telemetry',
    '/administration/sanctions-loader',
  ]) {
    test(`has no violations on ${path} in light, dark and Arabic`, async ({ page }) => {
      await stub(page)
      await page.goto(path)
      await settleAnimations(page)

      for (const mode of ['light', 'dark'] as const) {
        await page.getByTestId('account-button').click()
        await setTheme(page, mode)
        await page.keyboard.press('Escape')
        await settleAnimations(page)
        expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
      }

      await page.getByTestId('account-button').click()
      await setLanguage(page, 'ar')
      await page.keyboard.press('Escape')
      await settleAnimations(page)
      expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
    })
  }
})
