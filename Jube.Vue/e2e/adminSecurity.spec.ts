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
  landlord: false,
  permissions: [34, 35, 36],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const roles = [
  { id: 1, guid: 'r1', name: 'Analyst', active: true, locked: false },
  { id: 2, guid: 'r2', name: 'Auditor', active: true, locked: false },
]

const users = [
  {
    id: 7,
    guid: 'u7',
    name: 'sam',
    email: 'sam@example.com',
    roleRegistryGuid: 'r1',
    active: true,
    wirePasswordHash: true,
    passwordLocked: true,
  },
  {
    id: 8,
    guid: 'u8',
    name: 'kim',
    email: 'kim@example.com',
    roleRegistryGuid: 'r2',
    active: true,
    wirePasswordHash: false,
    passwordLocked: false,
  },
]

const specifications = [
  { id: 1, name: 'Read Write Case' },
  { id: 2, name: 'Read Write Suppression' },
  { id: 34, name: 'Read Write Security Role' },
]

const grants = [
  { id: 10, roleRegistryId: 1, permissionSpecificationId: 1, active: true, locked: false },
  { id: 11, roleRegistryId: 1, permissionSpecificationId: 2, active: true, locked: false },
  { id: 12, roleRegistryId: 2, permissionSpecificationId: 1, active: false, locked: false },
]

const gridModel = (area: string, title: string) => ({
  area,
  endpoint: `/api/${area}`,
  title,
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
      field: 'userName',
      title: 'User',
      description: '',
      type: 'string',
      format: 'text',
      unit: null,
      order: 20,
      sortable: true,
      hidden: false,
      numerator: null,
      denominator: null,
    },
  ],
})

const freshKey = 'k9freshkeyvalue0123456789abcdefghijklmnopqrstuvwxyz'
const freshPassword = 'Tmp9xQ2vLk4pR7sW'

function apiKeysOf() {
  return [
    {
      id: 101,
      userRegistryId: 7,
      name: 'Reporting',
      description: 'Nightly export',
      createdDate: '2026-10-20T09:00:00Z',
      apiKeyDisplay: 'abcdefgh',
    },
    {
      id: 102,
      userRegistryId: 7,
      name: 'Monitoring',
      description: null,
      createdDate: '2026-10-21T09:00:00Z',
      apiKeyDisplay: 'zyxwvuts',
    },
  ]
}

async function stub(page: Page, overrides: Partial<typeof me> = {}) {
  const apiKeys = apiKeysOf()

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await stubFormModels(page, [
    modelOf({
      area: 'RoleRegistry',
      keys: { id: 'id', parent: null, naturalKey: 'name' },
      fields: [{ name: 'active', type: 'boolean' }],
      children: [
        {
          area: 'UserRegistry',
          label: 'Users',
          hub: '',
          order: 10,
          parentKey: 'roleRegistryGuid',
          parentField: 'guid',
          listRoute: '/api/UserRegistry/ByRoleRegistryGuid',
        },
      ],
    }),
    modelOf({
      area: 'UserRegistry',
      keys: { id: 'id', parent: 'roleRegistryGuid', naturalKey: 'name' },
      fields: [{ name: 'active', type: 'boolean' }],
    }),
    modelOf({
      area: 'TenantRegistry',
      keys: { id: 'id', parent: null, naturalKey: 'name' },
    }),
  ])
  await page.route('**/api/Me', (route) => route.fulfill({ json: { ...me, ...overrides } }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/RoleRegistry', (route) =>
    route.request().method() === 'POST'
      ? route.fulfill({ json: { id: 9, guid: 'r9', name: 'Fresh', active: true } })
      : route.fulfill({ json: roles }),
  )
  await page.route('**/api/RoleRegistry/*', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').pop())

    return route.fulfill({
      json: roles.find((role) => role.id === id) ?? { id, guid: `r${id}`, name: 'Fresh' },
    })
  })
  await page.route('**/api/UserRegistry', (route) => route.fulfill({ json: users }))
  await page.route('**/api/UserRegistry/ByRoleRegistryGuid/*', (route) => {
    const guid = new URL(route.request().url()).pathname.split('/').pop()

    return route.fulfill({ json: users.filter((user) => user.roleRegistryGuid === guid) })
  })
  await page.route('**/api/UserRegistry/*', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').pop())

    return route.fulfill({ json: users.find((user) => user.id === id) ?? users[0]! })
  })
  await page.route('**/api/UserRegistry/SetPassword', (route) =>
    route.fulfill({
      json: { password: freshPassword, passwordExpiryDate: '2026-10-24T12:00:00Z' },
    }),
  )
  await page.route('**/api/UserRegistry/RevokeTokens/*', (route) =>
    route.fulfill({ status: 204, body: '' }),
  )
  await page.route('**/api/UserRegistryApiKey', (route) => {
    const body = route.request().postDataJSON() as { name: string; description: string | null }
    const created = {
      id: 109,
      userRegistryId: 7,
      name: body.name,
      description: body.description,
      createdDate: '2026-10-24T12:00:00Z',
      apiKeyDisplay: freshKey,
    }

    apiKeys.unshift({ ...created, apiKeyDisplay: freshKey.slice(0, 8) })

    return route.fulfill({ json: created })
  })
  await page.route('**/api/UserRegistryApiKey/*', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').pop())
    const index = apiKeys.findIndex((key) => key.id === id)

    if (index >= 0) apiKeys.splice(index, 1)

    return route.fulfill({ status: 200, body: '' })
  })
  await page.route('**/api/UserRegistryApiKey/ByUserRegistryId/*', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').pop())

    return route.fulfill({ json: apiKeys.filter((key) => key.userRegistryId === id) })
  })
  await page.route('**/api/TenantRegistry', (route) =>
    route.fulfill({ json: [{ id: 1, guid: 't1', name: 'Acme Bank' }] }),
  )
  await page.route('**/api/PermissionSpecification', (route) =>
    route.fulfill({ json: specifications }),
  )
  await page.route('**/api/RoleRegistryPermission**', (route) => route.fulfill({ json: grants }))
  await page.route('**/api/GridModel', (route) =>
    route.fulfill({ json: ['UserLogin', 'UserLogout'] }),
  )
  await page.route('**/api/GridModel/UserLogin', (route) =>
    route.fulfill({ json: gridModel('UserLogin', 'User sign-ins') }),
  )
  await page.route('**/api/GridModel/UserLogout', (route) =>
    route.fulfill({ json: gridModel('UserLogout', 'User sign-outs') }),
  )
  await page.route('**/api/UserLogin**', (route) =>
    route.fulfill({
      json: {
        rows: [{ createdDate: '2026-10-21T09:00:00Z', userName: 'sam' }],
        total: 1,
        statistics: { columns: {} },
      },
    }),
  )
  await page.route('**/api/UserLogout**', (route) =>
    route.fulfill({
      json: {
        rows: [{ createdDate: '2026-10-21T10:00:00Z', userName: 'kim' }],
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

test.describe('Security story', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('offers a card for each part the person may use, with counts', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security')

    for (const id of ['users', 'roles', 'permissions', 'logins', 'logouts']) {
      await expect(page.getByTestId(`root-${id}`)).toBeVisible()
    }

    await expect(page.getByTestId('root-tenants')).toHaveCount(0)
    await expect(page.getByTestId('root-total-users')).toHaveText('2')
    await expect(page.getByTestId('root-total-roles')).toHaveText('2')
  })

  test('leaves tenants to their own entry, even for the landlord', async ({ page }) => {
    await stub(page, { landlord: true })
    await page.goto('/administration/security')

    await expect(page.getByTestId('root-roles')).toBeVisible()
    await expect(page.getByTestId('root-tenants')).toHaveCount(0)
  })

  test('follows the permission of each card', async ({ page }) => {
    await stub(page, { permissions: [34] })
    await page.goto('/administration/security')

    await expect(page.getByTestId('root-roles')).toBeVisible()
    await expect(page.getByTestId('root-users')).toHaveCount(0)
    await expect(page.getByTestId('root-permissions')).toHaveCount(0)
    await expect(page.getByTestId('root-logins')).toHaveCount(0)
  })

  test('says so when nothing is permitted', async ({ page }) => {
    await stub(page, { permissions: [] })
    await page.goto('/administration/security')

    await expect(page.getByTestId('security-denied')).toBeVisible()
  })

  test('a user leads to a Role card that opens the role record', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security')
    await page.getByTestId('root-open-users').click()

    await expect(page.getByTestId('receipt-entry')).toBeVisible()
    await expect(page).toHaveURL(/path=users$/)

    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('beat-record')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('hub-role-name')).toHaveText('Analyst')
    await expect(page).toHaveURL(/path=users:7$/)

    await page.getByTestId('hub-open-role').click()

    await expect(page.getByTestId('beat-record')).toContainText('Analyst')
    await expect(page).toHaveURL(/path=roles:1$/)
  })

  test('a user shows an API keys card that opens the key list', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=users:7')

    await expect(page.getByTestId('hub-total-apiKeys')).toHaveText('2')

    await page.getByTestId('hub-open-apiKeys').click()

    await expect(page.getByTestId('api-key-panel')).toBeVisible()
    await expect(page.getByTestId('receipt-record')).toContainText('sam')
    await expect(page).toHaveURL(/path=users:7,~apiKeys$/)

    const rows = page.getByTestId('api-key-panel').locator('.tabulator-row')

    await expect(rows).toHaveCount(2)
    await expect(rows.first()).toContainText('Monitoring')
    await expect(rows.first()).toContainText('zyxwvuts…')
    await expect(rows.nth(1)).toContainText('Nightly export')
    await expect(page.getByTestId('api-key-reveal')).toHaveCount(0)
  })

  test('generating a key needs a name, reveals the key once and copies it', async ({
    page,
    context,
  }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write'])
    await stub(page)
    await page.goto('/administration/security?path=users:7,~apiKeys')
    await page.getByTestId('api-key-generate').click()

    await expect(page.getByTestId('api-key-name')).toBeFocused()

    await page.getByTestId('api-key-submit').click()

    await expect(page.getByTestId('api-key-reasons')).toContainText('A name is needed')

    await page.getByTestId('api-key-name').fill('Batch loader')
    await page.getByTestId('api-key-description').fill('  Overnight files  ')

    const posted = page.waitForRequest(
      (request) => request.url().endsWith('/api/UserRegistryApiKey') && request.method() === 'POST',
    )

    await page.getByTestId('api-key-submit').click()

    const body = (await posted).postDataJSON() as Record<string, unknown>

    expect(body).toEqual({
      userRegistryId: 7,
      name: 'Batch loader',
      description: 'Overnight files',
    })

    await expect(page.getByTestId('api-key-form')).toHaveCount(0)
    await expect(page.getByTestId('api-key-reveal')).toContainText('Batch loader')
    await expect(page.getByTestId('api-key-value')).toHaveValue(freshKey)
    await expect(page.getByTestId('api-key-value')).toBeFocused()

    const rows = page.getByTestId('api-key-panel').locator('.tabulator-row')

    await expect(rows).toHaveCount(3)
    await expect(rows.first()).toContainText(`${freshKey.slice(0, 8)}…`)
    await expect(rows.first()).not.toContainText(freshKey)

    await page.getByTestId('api-key-copy').click()

    await expect(page.getByTestId('api-key-copy')).toContainText('Copied')
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(freshKey)

    await page.getByTestId('api-key-done').click()

    await expect(page.getByTestId('api-key-reveal')).toHaveCount(0)
    await expect(page.getByTestId('hub-total-apiKeys')).toHaveCount(0)
  })

  test('revoking a key asks first and then removes it', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=users:7,~apiKeys')

    const rows = page.getByTestId('api-key-panel').locator('.tabulator-row')

    await expect(rows).toHaveCount(2)

    await rows.first().getByTestId('grid-action-revoke').click()

    await expect(page.getByTestId('api-key-revoke-dialog')).toBeVisible()
    await expect(page.getByTestId('api-key-revoke-dialog')).toContainText('Monitoring')

    await page.getByTestId('api-key-revoke-cancel').click()

    await expect(rows).toHaveCount(2)

    await rows.first().getByTestId('grid-action-revoke').click()

    const deleted = page.waitForRequest(
      (request) =>
        request.url().endsWith('/api/UserRegistryApiKey/102') && request.method() === 'DELETE',
    )

    await page.getByTestId('api-key-revoke-confirm').click()
    await deleted

    await expect(rows).toHaveCount(1)
    await expect(rows.first()).toContainText('Reporting')
  })

  test('the API key list has no accessibility violations, with a key revealed', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/administration/security?path=users:7,~apiKeys')
    await page.getByTestId('api-key-generate').click()
    await page.getByTestId('api-key-name').fill('Audit')
    await page.getByTestId('api-key-submit').click()
    await expect(page.getByTestId('api-key-reveal')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })

  test('a user shows a Sign-in card that issues a temporary password once', async ({
    page,
    context,
  }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write'])
    await stub(page)
    await page.goto('/administration/security?path=users:7')

    await expect(page.getByTestId('hub-access-locked')).toHaveText('Locked')

    await page.getByTestId('hub-access-password').click()

    const dialog = page.getByTestId('temporary-password-dialog')

    await expect(dialog).toBeVisible()
    await expect(dialog).toContainText('Issue a temporary password for sam?')

    const posted = page.waitForRequest(
      (request) =>
        request.url().endsWith('/api/UserRegistry/SetPassword') && request.method() === 'POST',
    )

    await page.getByTestId('temporary-password-issue').click()

    expect((await posted).postDataJSON()).toEqual({ id: 7, wirePasswordHash: true })

    await expect(dialog).toContainText('Copy the password for sam now')
    await expect(page.getByTestId('temporary-password-value')).toHaveValue(freshPassword)
    await expect(page.getByTestId('temporary-password-value')).toBeFocused()
    await expect(page.getByTestId('hub-access-locked')).toHaveCount(0)

    await page.getByTestId('temporary-password-copy').click()

    await expect(page.getByTestId('temporary-password-copy')).toContainText('Copied')
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(freshPassword)

    await page.getByTestId('temporary-password-done').click()

    await expect(dialog).toBeHidden()
    await expect(page.getByTestId('record-beat')).not.toContainText(freshPassword)
  })

  test('revoking the sessions of a user asks first', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=users:8')

    await expect(page.getByTestId('hub-access-locked')).toHaveCount(0)

    const revokes: string[] = []

    page.on('request', (request) => {
      if (request.url().includes('/api/UserRegistry/RevokeTokens/')) revokes.push(request.url())
    })

    await page.getByTestId('hub-access-revoke').click()

    await expect(page.getByTestId('revoke-sessions-dialog')).toContainText(
      'Revoke every session of kim?',
    )

    await page.getByTestId('revoke-sessions-cancel').click()

    await expect(page.getByTestId('revoke-sessions-dialog')).toBeHidden()
    expect(revokes).toEqual([])

    await page.getByTestId('hub-access-revoke').click()
    await page.getByTestId('revoke-sessions-confirm').click()

    await expect(page.getByTestId('toasts')).toContainText('Sessions revoked')
    expect(revokes.map((url) => new URL(url).pathname)).toEqual([
      '/api/UserRegistry/RevokeTokens/8',
    ])
  })

  test('the temporary password dialog has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=users:7')
    await page.getByTestId('hub-access-password').click()
    await page.getByTestId('temporary-password-issue').click()
    await expect(page.getByTestId('temporary-password-value')).toBeVisible()
    await page
      .getByTestId('temporary-password-dialog')
      .evaluate((dialog) =>
        Promise.all(dialog.getAnimations({ subtree: true }).map((animation) => animation.finished)),
      )

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })

  test('a role leads to its users and to its permissions', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=roles:1')

    await expect(page.getByTestId('hub-total-UserRegistry')).toHaveText('1')
    await expect(page.getByTestId('hub-total-permissions')).toHaveText('2')

    await page.getByTestId('hub-open-permissions').click()

    await expect(page.getByTestId('permission-allocation')).toBeVisible()
    await expect(page.getByTestId('permission-role')).toBeDisabled()
    await expect(page.getByTestId('permission-count')).toHaveText('2 granted')
    await expect(page.getByTestId('receipt-record')).toContainText('Analyst')
    await expect(page).toHaveURL(/path=roles:1,~permissions$/)
  })

  test('the users of a role open as a record under it', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=roles:1')
    await page.getByTestId('hub-open-UserRegistry').click()
    await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('receipt-record')).toContainText('Analyst')
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)
    await expect(page).toHaveURL(/path=roles:1,UserRegistry:7$/)
  })

  test('the permissions card asks for a role first', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security')
    await page.getByTestId('root-open-permissions').click()

    await expect(page.getByTestId('permission-empty')).toBeVisible()
    await expect(page.getByTestId('permission-role')).toBeEnabled()

    await page.getByTestId('permission-role').selectOption({ label: 'Auditor' })

    await expect(page.getByTestId('permission-count')).toHaveText('0 granted')
  })

  test('sign-ins and sign-outs are single grid beats', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security')
    await page.getByTestId('root-open-logins').click()

    await expect(page.getByTestId('payload-grid-UserLogin')).toBeVisible()
    await expect(page.locator('.tabulator-row').first()).toContainText('sam')

    await page.getByTestId('receipt-entry').click()
    await page.getByTestId('root-open-logouts').click()

    await expect(page.getByTestId('payload-grid-UserLogout')).toBeVisible()
    await expect(page.locator('.tabulator-row').first()).toContainText('kim')
  })

  test('New folds the list into a receipt and saving opens the record', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=roles')
    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('receipt-root')).toBeVisible()
    await expect(page.getByTestId('beat-create')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-create')).toContainText('New')

    await page.getByTestId('new-back').click()

    await expect(page.getByTestId('beat-root')).toHaveAttribute('data-state', 'working')

    await page.getByTestId('crud-new').click()
    await page
      .getByRole('checkbox', { name: /active/i })
      .first()
      .check()
    await page.getByTestId('crud-save').click()

    await expect(page.getByTestId('beat-record')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-record')).toContainText('Fresh')
    await expect(page.getByTestId('beat-create')).toHaveCount(0)
  })

  test('asks before a receipt discards unsaved edits', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=roles:1')
    await page.getByTestId('record-properties').locator('summary').click()
    await page
      .getByRole('checkbox', { name: /active/i })
      .first()
      .uncheck()

    await expect(page.getByTestId('crud-dirty')).toBeVisible()

    await page.getByTestId('receipt-entry').click()

    await expect(page.getByTestId('story-discard-dialog')).toBeVisible()

    await page.getByTestId('story-discard-cancel').click()

    await expect(page.getByTestId('beat-record')).toHaveAttribute('data-state', 'working')

    await page.getByTestId('receipt-entry').click()
    await page.getByTestId('story-discard-confirm').click()

    await expect(page.getByTestId('beat-entry')).toHaveAttribute('data-state', 'working')
  })

  test('a deep link rebuilds the role, the view and the receipts', async ({ page }) => {
    await stub(page)
    await page.goto('/administration/security?path=roles:2,~permissions')

    await expect(page.getByTestId('receipt-record')).toContainText('Auditor')
    await expect(page.getByTestId('beat-view:permissions')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('permission-count')).toHaveText('0 granted')
  })

  for (const name of modes) {
    test(`has no accessibility violations in ${name}`, async ({ page }) => {
      await stub(page)
      await page.goto('/administration/security')
      await inMode(page, name)
      await expect(page.getByTestId('root-roles')).toBeVisible()
      await settleAnimations(page)

      const entry = await new AxeBuilder({ page }).analyze()

      expect(entry.violations).toEqual([])

      await page.getByTestId('root-open-roles').click()
      await page.locator('.tabulator-row').first().getByTestId('grid-action-open').click()
      await expect(page.getByTestId('hub-permissions')).toBeVisible()
      await settleAnimations(page)

      const record = await new AxeBuilder({ page }).analyze()

      expect(record.violations).toEqual([])
    })
  }

  test('does not scroll sideways at phone width', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 })
    await stub(page)
    await page.goto('/administration/security?path=roles:1')
    await expect(page.getByTestId('hub-permissions')).toBeVisible()

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )

    expect(overflow).toBeLessThanOrEqual(0)
  })
})
