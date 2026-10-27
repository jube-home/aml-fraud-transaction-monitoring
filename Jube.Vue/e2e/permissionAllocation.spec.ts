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
import { settleAnimations } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const roles = [
  { id: 1, name: 'Analyst', active: true },
  { id: 2, name: 'Auditor', active: true },
]

const specifications = [
  { id: 1, name: 'Read Write Case' },
  { id: 2, name: 'Read Write Suppression' },
  { id: 34, name: 'Read Write Security Role' },
  { id: 999, name: 'Brand New Permission' },
]

const grant = (id: number, role: number, permission: number, active = true, locked = false) => ({
  id,
  roleRegistryId: role,
  permissionSpecificationId: permission,
  active,
  locked,
})

async function stub(page: Page, calls: string[] = []) {
  const grants = [
    grant(10, 1, 1),
    grant(11, 1, 2, false),
    grant(12, 1, 34, true, true),
    grant(20, 2, 1),
  ]

  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/RoleRegistry', (route) => route.fulfill({ json: roles }))
  await page.route('**/api/PermissionSpecification', (route) =>
    route.fulfill({ json: specifications }),
  )
  await page.route('**/api/RoleRegistryPermission**', (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname

    if (request.method() === 'GET') return route.fulfill({ json: grants })

    calls.push(`${request.method()} ${path} ${request.postData() ?? ''}`)

    return route.fulfill({ json: {} })
  })
}

test.describe('PermissionAllocation', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('asks for a role first, then groups the role permissions by area', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=permission-allocation')

    await expect(page.getByTestId('permission-empty')).toBeVisible()

    await page.getByTestId('permission-role').selectOption({ label: 'Analyst' })

    await expect(page.getByTestId('permission-groups')).toBeVisible()
    await expect(page.getByTestId('permission-1')).toBeChecked()
    await expect(page.getByTestId('permission-2')).not.toBeChecked()
    await expect(page.getByTestId('permission-34')).toBeDisabled()
    await expect(page.getByTestId('permission-999')).toBeVisible()
    await expect(page.getByTestId('permission-count')).toContainText('2')
    await expect(page.getByTestId('permission-save')).toBeDisabled()
  })

  test('sends only the differences when saved and clears the unsaved state', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await page.goto('/tools?tool=permission-allocation')
    await page.getByTestId('permission-role').selectOption({ label: 'Analyst' })

    await page.getByTestId('permission-1').uncheck()
    await page.getByTestId('permission-2').check()
    await page.getByTestId('permission-999').check()

    await expect(page.getByTestId('permission-dirty')).toContainText('3')

    await page.getByTestId('permission-save').click()

    await expect.poll(() => calls.length).toBe(3)
    expect(calls.some((c) => c.startsWith('DELETE /api/RoleRegistryPermission/10'))).toBe(true)
    expect(
      calls.some(
        (c) => c.startsWith('PUT') && c.includes('"id":11') && c.includes('"active":true'),
      ),
    ).toBe(true)
    expect(
      calls.some((c) => c.startsWith('POST') && c.includes('"permissionSpecificationId":999')),
    ).toBe(true)
  })

  test('discards back to the stored grants', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=permission-allocation')
    await page.getByTestId('permission-role').selectOption({ label: 'Analyst' })

    await page.getByTestId('permission-1').uncheck()
    await page.getByTestId('permission-discard').click()

    await expect(page.getByTestId('permission-1')).toBeChecked()
    await expect(page.getByTestId('permission-save')).toBeDisabled()
  })

  test('filters by name', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=permission-allocation')
    await page.getByTestId('permission-role').selectOption({ label: 'Analyst' })

    await page.getByTestId('permission-search').fill('suppression')

    await expect(page.getByTestId('permission-2')).toBeVisible()
    await expect(page.getByTestId('permission-1')).toHaveCount(0)
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=permission-allocation')
    await page.getByTestId('permission-role').selectOption({ label: 'Analyst' })
    await expect(page.getByTestId('permission-groups')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
