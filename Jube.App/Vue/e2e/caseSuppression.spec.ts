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
import {setTheme} from './support'

const noMarks = (text: string) => text.replace(/[\u2068\u2069]/g, '')

const workflow = '11111111-1111-1111-1111-111111111111'
const model = '66666666-6666-6666-6666-666666666666'

const counts = {
  countInCase: 14,
  firstSeenDate: '2026-10-02T00:00:00Z',
  lastSeenDate: '2026-10-29T00:00:00Z',
  widerCountStateId: 1,
  widerTransactions: 230,
  widerKeys: 213,
  widerCapped: false,
}

const caseDetail = {
  id: 9041,
  entityAnalysisModelInstanceEntryGuid: '33333333-3333-3333-3333-333333333333',
  diaryDate: '2026-10-25T09:00:00Z',
  caseWorkflowGuid: workflow,
  caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222222',
  createdDate: '2026-10-20T09:00:00Z',
  locked: true,
  lockedUser: 'alex',
  lockedDate: '2026-10-24T09:00:00Z',
  closedStatusId: 0,
  closedDate: '2026-10-24T09:00:00Z',
  closedUser: null,
  caseKey: 'AccountId',
  diary: false,
  diaryUser: null,
  rating: 3,
  caseKeyValue: 'ACC-441',
  foreColor: null,
  backColor: null,
  formattedPayload: [],
  activation: [],
  enableVisualisation: false,
  entityAnalysisModelId: 1,
}

function suppression(patch: Record<string, unknown> = {}) {
  return {
    id: 5,
    caseId: 9041,
    caseKey: 'AccountId',
    caseKeyValue: 'ACC-441',
    overrideKey: 'IpAddress',
    overrideKeyValue: '10.0.0.1',
    pathId: 2,
    reason: 'A shared office address',
    expiryDate: '2099-12-02T00:00:00Z',
    stateId: 1,
    standingId: 1,
    sourceId: 1,
    version: 1,
    approvalsRequired: 1,
    approvalsRecorded: 0,
    canDecide: true,
    canRetract: false,
    canResubmit: false,
    then: counts,
    now: {...counts, countInCase: 15},
    targets: [
      {
        id: 1,
        entityAnalysisModelGuid: model,
        entityAnalysisModelName: 'Retail',
        ruleName: 'Velocity 9d',
        overrideKindId: 0,
        appliedOverrideId: null,
        appliedDate: null,
        standingId: 1,
      },
    ],
    transactions: [{id: 1, archiveId: 71, referenceDate: '2026-10-21T09:00:00Z', laneKey: null}],
    approvals: [],
    createdUser: 'sam',
    createdDate: '2026-10-21T09:00:00Z',
    decidedUser: null,
    decidedDate: null,
    ...patch,
  }
}

async function stub(page: Page, options: { permissions?: number[]; items?: unknown[] } = {}) {
  const decisions: { path: string; body: Record<string, unknown> }[] = []
  const requests: Record<string, unknown>[] = []
  const items = options.items ?? [suppression()]

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await page.route('**/api/Me', (route) =>
    route.fulfill({
      json: {...me, userName: 'alex', permissions: options.permissions ?? [1, 2, 70]},
    }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/CaseSearch/Workflows', (route) =>
    route.fulfill({json: [{guid: workflow, name: 'AML structuring', modelName: 'Retail'}]}),
  )
  await page.route('**/api/GetCaseByIdQuery/*', (route) => route.fulfill({json: caseDetail}))
  await page.route('**/api/CaseSuppression/Awaiting**', (route) => route.fulfill({json: items}))
  await page.route(/\/api\/CaseSuppression\/ByCase\/9041/, (route) =>
    route.fulfill({json: items}),
  )
  await page.route(/\/api\/CaseSuppression\/\d+$/, (route) => route.fulfill({json: items[0]}))
  await page.route('**/api/CaseSuppression/Approve', (route) => {
    decisions.push({path: 'Approve', body: route.request().postDataJSON()})

    return route.fulfill({json: suppression({stateId: 2, standingId: 2})})
  })
  await page.route('**/api/CaseSuppression/Reject', (route) => {
    decisions.push({path: 'Reject', body: route.request().postDataJSON()})

    return route.fulfill({json: suppression({stateId: 3, standingId: 3})})
  })
  await page.route('**/api/CaseSuppression', (route) => {
    if (route.request().method() !== 'POST') return route.fallback()

    requests.push(route.request().postDataJSON())

    return route.fulfill({json: suppression()})
  })
  await page.route(/\/api\/CaseSuppressionCandidates\/Keys/, (route) =>
    route.fulfill({
      json: [
        {name: 'AccountId', isCaseKey: true, enabled: false, reason: 'KeyNotOverrideEnabled'},
        {name: 'IpAddress', isCaseKey: false, enabled: true, reason: null},
      ],
    }),
  )
  await page.route(/\/api\/CaseSuppressionCandidates\/Values/, (route) =>
    route.fulfill({
      json: [
        {
          value: '10.0.0.1',
          count: 14,
          firstSeenDate: '2026-10-02T00:00:00Z',
          lastSeenDate: '2026-10-29T00:00:00Z',
          lanes: [{laneKey: '', laneValue: 'ACC-441', count: 14}],
          widerCountStateId: 1,
          widerTransactions: 230,
          widerKeys: 213,
          widerCapped: false,
        },
        {
          value: '192.0.2.9',
          count: 2,
          firstSeenDate: null,
          lastSeenDate: null,
          lanes: [],
          widerCountStateId: 2,
          widerTransactions: null,
          widerKeys: null,
          widerCapped: false,
        },
      ],
    }),
  )
  await page.route(/\/api\/CaseSuppressionCandidates\/Transactions/, (route) =>
    route.fulfill({
      json: [
        {
          archiveId: 71,
          referenceDate: '2026-10-21T09:00:00Z',
          responseElevation: 12,
          activationCount: 1,
          activations: 'Velocity 9d',
          laneKey: null,
        },
        {
          archiveId: 72,
          referenceDate: '2026-10-22T09:00:00Z',
          responseElevation: 0,
          activationCount: 0,
          activations: '',
          laneKey: null,
        },
      ],
    }),
  )
  await page.route(/\/api\/CaseSuppressionCandidates\/InForce/, (route) =>
    route.fulfill({
      json: [
        {
          id: 3,
          isRule: false,
          overrideKey: 'IpAddress',
          overrideKeyValue: '192.0.2.9',
          entityAnalysisModelName: 'Retail',
          ruleName: null,
          overrideKindId: 0,
          expiryDate: null,
          createdUser: 'Administrator',
          createdDate: '2026-09-01T00:00:00Z',
          originCaseId: null,
        },
      ],
    }),
  )
  await page.route(/\/api\/CaseSuppressionCandidates\/ReasonDraft/, (route) =>
    route.fulfill({
      json: {
        reason: 'Suppress Retail for IpAddress 10.0.0.1: seen in 14 transactions on C-9041.',
      },
    }),
  )

  return {decisions, requests}
}

test.describe('case suppression', () => {
  test('shows the Suppress tile only with the Override permission', async ({page}) => {
    await stub(page, {permissions: [1]})
    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-decide-tab-research')).toBeVisible()
    await expect(page.getByTestId('case-decide-tab-suppress')).toHaveCount(0)
  })

  test('walks the transaction path to a request with a drafted reason and a required expiry', async ({
                                                                                                       page,
                                                                                                     }) => {
    const {requests} = await stub(page, {items: []})

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-suppress').click()

    const dialog = page.getByTestId('case-suppress-dialog')

    await expect(dialog).toBeVisible()
    await expect(page.getByTestId('suppress-key-disabled-AccountId')).toBeVisible()
    await expect(page.getByTestId('suppress-key-AccountId')).toBeDisabled()
    await page.getByTestId('suppress-key-IpAddress').check()
    await page.getByTestId('suppress-next').click()

    await expect(page.getByTestId('suppress-values')).toBeVisible()
    await expect(page.getByTestId('suppress-values')).toContainText('10.0.0.1')
    await expect(page.getByTestId('suppress-values')).toContainText('192.0.2.9')
    await page
      .getByTestId('suppress-values')
      .getByRole('button', {name: 'Review'})
      .first()
      .click()

    await expect(page.getByTestId('suppress-evidence')).toBeVisible()
    await expect(page.getByTestId('suppress-context')).toContainText('10.0.0.1')
    await expect(page.getByTestId('suppress-evidence-count')).toContainText('2 of 2')
    await page.getByTestId('suppress-show-evidence').click()
    await page.getByTestId('suppress-next').click()

    await expect(page.getByTestId('override-manager')).toBeVisible()
  })

  test('the case key path skips the value picker', async ({page}) => {
    await stub(page, {items: []})
    await page.route(/\/api\/CaseSuppressionCandidates\/Keys/, (route) =>
      route.fulfill({
        json: [{name: 'AccountId', isCaseKey: true, enabled: true, reason: null}],
      }),
    )
    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-suppress').click()
    await page.getByTestId('suppress-key-AccountId').check()
    await page.getByTestId('suppress-next').click()
    await expect(page.getByTestId('suppress-values')).toHaveCount(0)
    await expect(page.getByTestId('override-manager')).toBeVisible()
  })

  test('an approver reviews counts then and now, what will change, and approves', async ({
                                                                                           page,
                                                                                         }) => {
    const {decisions} = await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-decide-tab-suppression')).toBeVisible()
    await page.getByTestId('case-decide-tab-suppression').click()

    await expect(page.getByTestId('case-suppression-approval-dialog')).toBeVisible()
    await expect(page.getByTestId('suppression-approval-then')).toContainText('14 transactions')
    await expect(page.getByTestId('suppression-approval-now')).toContainText('15 transactions')
    await expect
      .poll(async () => noMarks(await page.getByTestId('suppression-approval-changes').innerText()))
      .toContain('Suppress Retail / Velocity 9d for IpAddress 10.0.0.1')
    await expect(page.getByTestId('suppression-approval-reason')).toContainText(
      'A shared office address',
    )

    await page.getByTestId('suppression-note').fill('Looks right')
    await page.getByTestId('suppression-approve').click()

    await expect.poll(() => decisions.length).toBe(1)
    expect(decisions[0]).toEqual({
      path: 'Approve',
      body: {id: 5, note: 'Looks right', expiryDate: expect.any(String)},
    })
  })

  test('the maker cannot approve their own request but can retract it', async ({page}) => {
    await stub(page, {items: [suppression({canDecide: false, canRetract: true})]})

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-suppression').click()
    await expect(page.getByTestId('suppression-approve')).toHaveCount(0)
    await expect(page.getByTestId('suppression-reject')).toHaveCount(0)
    await expect(page.getByTestId('suppression-retract')).toBeVisible()
  })

  test('opens the approval straight from the Cases page panel', async ({page}) => {
    await stub(page)

    await page.goto('/cases')
    await expect(page.getByTestId('cases-approvals')).toBeVisible()
    await expect(page.getByTestId('cases-approvals')).toContainText('IpAddress = 10.0.0.1')
    await page.getByTestId('cases-approvals').getByRole('button', {name: 'Review'}).click()
    await expect(page).toHaveURL(/\/cases\/9041\?suppression=5/)
    await expect(page.getByTestId('case-suppression-approval-dialog')).toBeVisible()
  })

  test('hides the Approvals panel without the approval permission', async ({page}) => {
    await stub(page, {permissions: [1, 2]})
    await page.goto('/cases')
    await expect(page.getByTestId('cases-page')).toBeVisible()
    await expect(page.getByTestId('cases-approvals')).toHaveCount(0)
  })

  test('the canvas band lists what was set from this case and what is already in force', async ({
                                                                                                  page,
                                                                                                }) => {
    await stub(page, {items: [suppression({stateId: 2, standingId: 2, canDecide: false})]})

    await page.goto('/cases/9041?section=journal')
    await expect(page.getByTestId('case-suppressions')).toBeVisible()
    await expect
      .poll(async () => noMarks(await page.getByTestId('suppression-card').innerText()))
      .toContain('IpAddress = 10.0.0.1')
    await expect(page.getByTestId('suppression-standing-approved')).toBeVisible()
    await expect(page.getByTestId('suppression-in-force')).toContainText('192.0.2.9')
    await expect(page.getByTestId('case-suppressions').getByRole('button')).toHaveCount(0)
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no axe violations in the dialog and the band in the ${theme} theme`, async ({
                                                                                            page,
                                                                                          }) => {
      await stub(page)
      await page.goto('/cases/9041?section=journal')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await expect(page.getByTestId('case-suppressions')).toBeVisible()

      const band = await new AxeBuilder({page})
        .include('[data-testid="case-suppressions"]')
        .analyze()

      expect(band.violations).toEqual([])

      await page.getByTestId('case-decide-tab-suppression').click()
      await expect(page.getByTestId('suppression-approval-key')).toBeVisible()
      await page.waitForTimeout(800)

      const dialog = await new AxeBuilder({page})
        .include('[data-testid="case-suppression-approval-dialog"]')
        .analyze()

      expect(dialog.violations).toEqual([])
    })
  }
})
