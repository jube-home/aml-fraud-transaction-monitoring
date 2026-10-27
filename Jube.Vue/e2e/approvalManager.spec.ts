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

interface Scenario {
  status: Record<string, unknown>
  history: unknown[]
  approveResponse?: { status: number; body: unknown }
}

const pendingStatus = {
  kind: 1,
  entityId: 5,
  name: 'Model',
  currentVersion: 2,
  effectiveVersion: 1,
  deleted: false,
  pending: true,
  rejected: false,
  makerUser: 'maker',
  approvalsRecorded: 0,
  approvalsRequired: 1,
  canApprove: true,
}

const rejectedStatus = { ...pendingStatus, pending: false, rejected: true, canApprove: false }

const history = [
  {
    entityVersion: 1,
    state: 'Approved',
    note: 'Looks fine',
    createdUser: 'checker',
    createdDate: '2026-10-20T10:30:00Z',
    returnedNote: null,
    returnKindId: null,
  },
]

const rejectedHistory = [
  ...history,
  {
    entityVersion: 2,
    state: 'Rejected',
    note: 'Needs work',
    createdUser: 'checker',
    createdDate: '2026-10-21T09:00:00Z',
    returnedNote: null,
    returnKindId: null,
  },
]

const versions = [
  { Id: 10, Version: 1, CreatedUser: 'maker', CreatedDate: '2026-10-19T08:00:00Z' },
  { Id: 20, Version: 2, CreatedUser: 'maker', CreatedDate: '2026-10-20T08:00:00Z' },
]

async function stub(page: Page, scenario: Scenario, posts: string[] = []) {
  await stubFormModels(page, [
    modelOf({
      area: 'EntityAnalysisModel',
      approval: { kind: 1, historyRoute: '/api/EntityAnalysisModelVersionHistory' },
    }),
  ])
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityApproval/Status**', (route) =>
    route.fulfill({ json: scenario.status }),
  )
  await page.route('**/api/EntityApproval/History**', (route) =>
    route.fulfill({ json: scenario.history }),
  )
  await page.route('**/api/EntityAnalysisModelVersionHistory/ByParent/**', (route) =>
    route.fulfill({ json: versions }),
  )
  await page.route('**/api/EntityAnalysisModelVersionHistory/Compare**', (route) =>
    route.fulfill({ json: [{ propertyName: 'Name', fromValue: 'Old', toValue: 'New' }] }),
  )
  await page.route('**/api/EntityApproval/Approve', (route) => {
    posts.push(`${route.request().method()} Approve ${route.request().postData() ?? ''}`)

    const response = scenario.approveResponse ?? { status: 200, body: {} }

    return route.fulfill({ status: response.status, json: response.body })
  })
  await page.route('**/api/EntityApproval/Reject', (route) => {
    posts.push(`${route.request().method()} Reject ${route.request().postData() ?? ''}`)

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityAnalysisModel/5/*', (route) => {
    posts.push(
      `${route.request().method()} ${new URL(route.request().url()).pathname} ${route.request().postData() ?? ''}`,
    )

    return route.fulfill({ json: {} })
  })
}

async function open(page: Page) {
  await page.goto('/tools?tool=approval-manager')
  await page.getByTestId('bench-entity').fill('5')
  await page.getByTestId('bench-load').click()
  await expect(page.getByTestId('approval-panel')).toBeVisible()
}

test.describe('ApprovalManager', () => {
  test.use({ viewport: { width: 1440, height: 1100 } })

  test('shows the status, the awaiting row and the history in grids', async ({ page }) => {
    await stub(page, { status: pendingStatus, history })
    await open(page)

    await expect(page.getByTestId('approval-state')).toHaveText('Awaiting approval')
    await expect(page.getByTestId('approval-facts')).toContainText(
      '1 of 1'.replace('1 of 1', '0 of 1'),
    )
    await expect(page.locator('.tabulator-row')).toHaveCount(4)
    await expect(page.getByTestId('grid-action-approve')).toBeVisible()
    await expect(page.getByTestId('grid-action-reject')).toBeVisible()
    await expect(page.getByText('Looks fine')).toBeVisible()
  })

  test('approves with the typed note against the current version', async ({ page }) => {
    const posts: string[] = []

    await stub(page, { status: pendingStatus, history }, posts)
    await open(page)

    await page.getByTestId('grid-input-note').fill('Ship it')
    await page.getByTestId('grid-action-approve').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toContain('"version":2')
    expect(posts[0]).toContain('"note":"Ship it"')
    expect(posts[0]).toContain('"kind":1')
  })

  test('lists the server reasons when a decision is refused', async ({ page }) => {
    await stub(page, {
      status: pendingStatus,
      history,
      approveResponse: {
        status: 400,
        body: { reasons: ['You made this change.', 'Second reason.'] },
      },
    })
    await open(page)

    await page.getByTestId('grid-action-approve').click()

    await expect(page.getByTestId('approval-reasons')).toContainText('You made this change.')
    await expect(page.getByTestId('approval-reasons')).toContainText('Second reason.')
  })

  test('offers resubmit and retract on the rejected version', async ({ page }) => {
    const posts: string[] = []

    await stub(page, { status: rejectedStatus, history: rejectedHistory }, posts)
    await open(page)

    await expect(page.getByTestId('approval-state')).toHaveText('Rejected')
    await expect(page.getByTestId('grid-action-approve')).toHaveCount(0)

    await page.getByTestId('grid-input-returnNote').fill('Fixed')
    await page.getByTestId('grid-action-resubmit').click()

    await expect
      .poll(() => posts.some((post) => post.startsWith('POST /api/EntityAnalysisModel/5/Resubmit')))
      .toBe(true)
    expect(posts.join()).toContain('"note":"Fixed"')
    await expect(page.getByTestId('grid-action-retract')).toBeVisible()
  })

  test('compares a version with the one before it, and says so for the first', async ({ page }) => {
    await stub(page, { status: pendingStatus, history })
    await open(page)

    const versionsGrid = page.getByLabel('Superseded versions')

    await versionsGrid.getByTestId('grid-action-compare').nth(1).click()

    await expect(page.getByTestId('approval-changes')).toContainText('Changes in version 2')
    await expect(page.getByTestId('approval-changes')).toContainText('Old')
    await expect(page.getByTestId('approval-changes')).toContainText('New')

    await versionsGrid.getByTestId('grid-action-compare').first().click()

    await expect(page.getByTestId('approval-first')).toBeVisible()
  })

  test('hides itself when the status is not available', async ({ page }) => {
    await stub(page, { status: pendingStatus, history })
    await page.route('**/api/EntityApproval/Status**', (route) =>
      route.fulfill({ status: 403, body: '' }),
    )
    await page.goto('/tools?tool=approval-manager')
    await page.getByTestId('bench-entity').fill('5')
    await page.getByTestId('bench-load').click()

    await expect(page.getByTestId('approval-panel')).toHaveCount(0)
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page, { status: pendingStatus, history })
    await open(page)
    await expect(page.locator('.tabulator-row')).toHaveCount(4)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
