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
import { setLanguage, setTheme, settleAnimations } from './support'

const me = {
  userName: 'alex',
  landlord: true,
  permissions: [31, 32, 33],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

async function stub(page: Page, overrides: Partial<typeof me> = {}) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await stubFormModels(page, [
    modelOf({
      area: 'TenantRegistry',
      keys: { id: 'id', parent: null, naturalKey: 'name' },
      fields: [{ name: 'active', type: 'boolean' }],
    }),
    modelOf({
      area: 'VisualisationRegistry',
      keys: { id: 'id', parent: null, naturalKey: 'name' },
      fields: [{ name: 'active', type: 'boolean' }],
      children: [
        {
          area: 'VisualisationRegistryParameter',
          label: 'Parameters',
          hub: '',
          order: 10,
          parentKey: 'visualisationRegistryId',
          parentField: 'id',
          listRoute: '/api/VisualisationRegistryParameter/ByVisualisationRegistryId',
        },
      ],
    }),
    modelOf({
      area: 'VisualisationRegistryParameter',
      keys: { id: 'id', parent: 'visualisationRegistryId', naturalKey: 'name' },
      fields: [{ name: 'active', type: 'boolean' }],
    }),
  ])
  await page.route('**/api/Me', (route) => route.fulfill({ json: { ...me, ...overrides } }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/TenantRegistry', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Tenant one', active: true }] }),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({ json: { id: 1, name: 'Tenant one', active: true } }),
  )
  await page.route('**/api/VisualisationRegistry', (route) =>
    route.fulfill({ json: [{ id: 5, guid: 'v5', name: 'Sales overview', active: true }] }),
  )
  await page.route('**/api/VisualisationRegistry/5', (route) =>
    route.fulfill({ json: { id: 5, guid: 'v5', name: 'Sales overview', active: true } }),
  )
  await page.route('**/api/VisualisationRegistryParameter/ByVisualisationRegistryId*', (route) =>
    route.fulfill({ json: [] }),
  )
}

test.describe('Tenants page', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('opens straight onto the tenants list and then a tenant', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/tenants')
    await expect(page.getByTestId('grid-action-open')).toBeVisible()
    await expect(page.getByTestId('adminTenants-denied')).toHaveCount(0)
    await page.getByTestId('grid-action-open').click()
    await expect(page).toHaveURL(/path=adminTenants:1/)
  })

  test('is denied to anyone but the landlord', async ({ page }) => {
    await stub(page, { landlord: false })
    await page.goto('/administration/tenants')
    await expect(page.getByTestId('adminTenants-denied')).toBeVisible()
  })
})

test.describe('Visualisation setup page', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('lists dashboards and opens one with its child cards', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/visualisations')
    await expect(page.getByTestId('grid-action-open')).toBeVisible()
    await page.getByTestId('grid-action-open').click()
    await expect(page).toHaveURL(/path=adminVisualisations:5/)
    await expect(page.getByRole('heading', { name: 'Parameters' })).toBeVisible()
  })

  test('is denied without any visualisation permission', async ({ page }) => {
    await stub(page, { permissions: [] })
    await page.goto('/administration/visualisations')
    await expect(page.getByTestId('adminVisualisations-denied')).toBeVisible()
  })
})

test.describe('Solo story pages accessibility', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  for (const path of ['/administration/tenants', '/administration/visualisations']) {
    test(`has no violations on ${path} in light, dark and Arabic`, async ({ page }) => {
      await stub(page)
      await page.goto(path)
      await expect(page.getByTestId('grid-action-open')).toBeVisible()
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
