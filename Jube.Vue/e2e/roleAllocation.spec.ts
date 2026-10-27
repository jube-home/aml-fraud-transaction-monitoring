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

const guid = '11111111-1111-1111-1111-111111111111'

const roles = [
  { id: 1, guid: 'aaaaaaaa-0000-0000-0000-000000000001', name: 'Analyst', active: true },
  { id: 2, guid: 'aaaaaaaa-0000-0000-0000-000000000002', name: 'Auditor', active: true },
  { id: 3, guid: 'aaaaaaaa-0000-0000-0000-000000000003', name: 'Manager', active: true },
]

async function stub(page: Page, calls: string[] = []) {
  await stubFormModels(page, [
    modelOf({
      area: 'CaseWorkflowStatus',
      roles: { endpoint: '/api/CaseWorkflowStatusRole', parentKey: 'caseWorkflowStatusGuid' },
    }),
  ])
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/RoleRegistry', (route) => route.fulfill({ json: roles }))
  await page.route('**/api/CaseWorkflowStatusRole**', (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname

    if (request.method() === 'GET') {
      return route.fulfill({
        json: [{ id: 40, caseWorkflowStatusGuid: guid, roleRegistryGuid: roles[0]!.guid }],
      })
    }

    calls.push(`${request.method()} ${path} ${request.postData() ?? ''}`)

    return route.fulfill({ json: {} })
  })
}

async function open(page: Page) {
  await page.goto('/tools?tool=role-allocation')
  await page.getByTestId('bench-parent').selectOption('CaseWorkflowStatus')
  await page.getByTestId('bench-guid').fill(guid)
  await page.getByTestId('bench-load').click()
  await expect(page.getByTestId('transfer-list')).toBeVisible()
}

test.describe('RoleAllocation', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('splits the roles into available and assigned', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('transfer-assigned').locator('li')).toHaveText(['Analyst'])
    await expect(page.getByTestId('transfer-available').locator('li')).toHaveText([
      'Auditor',
      'Manager',
    ])
    await expect(page.getByTestId('role-save')).toBeDisabled()
  })

  test('saves only the differences', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await page.getByTestId('transfer-item-aaaaaaaa-0000-0000-0000-000000000002').check()
    await page.getByTestId('transfer-assign').click()
    await page.getByTestId(`transfer-item-${roles[0]!.guid}`).check()
    await page.getByTestId('transfer-unassign').click()

    await expect(page.getByTestId('role-dirty')).toContainText('2')

    await page.getByTestId('role-save').click()

    await expect.poll(() => calls.length).toBe(2)
    expect(calls).toContainEqual(expect.stringContaining('DELETE /api/CaseWorkflowStatusRole/40'))
    expect(calls).toContainEqual(
      expect.stringMatching(
        /POST \/api\/CaseWorkflowStatusRole .*"caseWorkflowStatusGuid":"1111.*"roleRegistryGuid":"aaaaaaaa-0000-0000-0000-000000000002"/,
      ),
    )
  })

  test('moves everything and discards back', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('transfer-assign-all').click()
    await expect(page.getByTestId('transfer-assigned-count')).toHaveText('3')

    await page.getByTestId('role-discard').click()

    await expect(page.getByTestId('transfer-assigned-count')).toHaveText('1')
    await expect(page.getByTestId('role-save')).toBeDisabled()
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await open(page)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
