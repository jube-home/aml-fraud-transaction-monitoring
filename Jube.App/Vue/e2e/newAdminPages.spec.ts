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
import {expect, type Page, type Request} from '@playwright/test'
import {test} from './fixtures'
import {settleAnimations} from './support'

const base = {
  userName: 'alex',
  landlord: false,
  permissions: [] as number[],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const iso = '2026-11-10T09:00:00Z'

async function stub(page: Page, permissions: number[]) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await page.route('**/api/Me', (route) => route.fulfill({json: {...base, permissions}}))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
}

function body(request: Request): Record<string, unknown> {
  return request.postDataJSON() as Record<string, unknown>
}

const erasure = (patch: Record<string, unknown> = {}) => ({
  guid: 'e0000000-0000-0000-0000-000000000001',
  keyName: 'AccountId',
  keyValue: 'ACC-1',
  reason: 'DSR-77',
  stateId: 1,
  refusalReasonId: null,
  archiveRowsFound: 4,
  archiveRowsRemoved: null,
  graphNodesRemoved: null,
  graphEdgesRemoved: null,
  casePayloadsNulled: null,
  requestedUser: 'sam',
  requestedDate: iso,
  decidedUser: null,
  decidedDate: null,
  decisionNote: null,
  executedDate: null,
  ...patch,
})

test.describe('Erasure and subject access page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('lists requests and lets a different approver approve one', async ({page}) => {
    await stub(page, [78, 79])
    await page.route('**/api/Erasure', (route) => route.fulfill({json: [erasure()]}))
    const approve = page.waitForRequest('**/api/Erasure/Approve')
    await page.route('**/api/Erasure/Approve', (route) =>
      route.fulfill({json: erasure({stateId: 2})}),
    )
    await page.goto('/administration/erasure')

    await expect(page.getByTestId('erasure-state-e0000000-0000-0000-0000-000000000001')).toHaveText(
      'Pending approval',
    )
    await page.getByTestId('erasure-approve-e0000000-0000-0000-0000-000000000001').click()
    expect(body(await approve)).toMatchObject({guid: 'e0000000-0000-0000-0000-000000000001'})
  })

  test('never offers the requester approve or reject, only retract', async ({page}) => {
    await stub(page, [78, 79])
    await page.route('**/api/Erasure', (route) =>
      route.fulfill({json: [erasure({requestedUser: 'alex'})]}),
    )
    await page.goto('/administration/erasure')

    await expect(
      page.getByTestId('erasure-retract-e0000000-0000-0000-0000-000000000001'),
    ).toBeVisible()
    await expect(
      page.getByTestId('erasure-approve-e0000000-0000-0000-0000-000000000001'),
    ).toHaveCount(0)
    await expect(
      page.getByTestId('erasure-reject-e0000000-0000-0000-0000-000000000001'),
    ).toHaveCount(0)
  })

  test('rejecting needs a reason', async ({page}) => {
    await stub(page, [79])
    await page.route('**/api/Erasure', (route) => route.fulfill({json: [erasure()]}))
    await page.goto('/administration/erasure')

    const reject = page.getByTestId('erasure-reject-e0000000-0000-0000-0000-000000000001')
    await expect(reject).toBeDisabled()
    await page.getByTestId('erasure-note-e0000000-0000-0000-0000-000000000001').fill('No grounds')
    await expect(reject).toBeEnabled()
  })

  test('previews a subject, shows the refusal reason and sends the request body', async ({
                                                                                           page,
                                                                                         }) => {
    await stub(page, [78])
    await page.route('**/api/Erasure', (route) => route.fulfill({json: []}))
    await page.route('**/api/Erasure/Preview', (route) =>
      route.fulfill({json: {archiveRows: 3, refusalReasonId: 2}}),
    )
    const request = page.waitForRequest('**/api/Erasure/Request')
    await page.route('**/api/Erasure/Request', (route) => route.fulfill({json: erasure()}))
    await page.goto('/administration/erasure')

    await page.getByTestId('erasure-key-name').fill('AccountId')
    await page.getByTestId('erasure-key-value').fill('ACC-1')
    await expect(page.getByTestId('erasure-request')).toBeDisabled()
    await page.getByTestId('erasure-preview').click()
    await expect(page.getByTestId('erasure-preview-result')).toContainText(
      '3 archived transactions',
    )
    await expect(page.getByTestId('erasure-preview-result')).toContainText('In an open case')

    await page.getByTestId('erasure-reason').fill('DSR-77')
    await page.getByTestId('erasure-request').click()
    expect(body(await request)).toEqual({
      keyName: 'AccountId',
      keyValue: 'ACC-1',
      reason: 'DSR-77',
    })
  })

  test('hides the request form from an approver who cannot request', async ({page}) => {
    await stub(page, [79])
    await page.route('**/api/Erasure', (route) => route.fulfill({json: []}))
    await page.goto('/administration/erasure')

    await expect(page.getByTestId('erasure-empty')).toBeVisible()
    await expect(page.getByTestId('erasure-subject')).toHaveCount(0)
  })
})

const inventoryRow = (patch: Record<string, unknown> = {}) => ({
  kindId: 1,
  modelGuid: 'a0000000-0000-0000-0000-000000000001',
  name: 'Payments model',
  recorded: true,
  ownerUser: 'Head of Fraud',
  riskTierId: 3,
  nextReviewDate: '2026-12-01T00:00:00Z',
  reviewStatusId: 3,
  ...patch,
})

test.describe('Model governance page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('lists the inventory with review status and a card link', async ({page}) => {
    await stub(page, [6])
    await page.route('**/api/ModelGovernance', (route) =>
      route.fulfill({
        json: [
          inventoryRow(),
          inventoryRow({
            modelGuid: 'a0000000-0000-0000-0000-000000000002',
            name: 'Cards model',
            recorded: false,
            ownerUser: null,
            riskTierId: null,
            nextReviewDate: null,
            reviewStatusId: 1,
          }),
        ],
      }),
    )
    await page.goto('/administration/governance')

    await expect(
      page.getByTestId('governance-status-a0000000-0000-0000-0000-000000000001'),
    ).toHaveText('Due soon')
    await expect(
      page.getByTestId('governance-status-a0000000-0000-0000-0000-000000000002'),
    ).toHaveText('Not recorded')
    await expect(
      page.getByTestId('governance-card-a0000000-0000-0000-0000-000000000001'),
    ).toHaveAttribute(
      'href',
      '/api/ModelGovernance/Card?kindId=1&modelGuid=a0000000-0000-0000-0000-000000000001',
    )
  })

  test('edits a statement and sends it', async ({page}) => {
    await stub(page, [6])
    await page.route('**/api/ModelGovernance', (route) => route.fulfill({json: [inventoryRow()]}))
    await page.route('**/api/ModelGovernance/ByModel*', (route) =>
      route.fulfill({json: {current: null, history: []}}),
    )
    const save = page.waitForRequest('**/api/ModelGovernance/Set')
    await page.route('**/api/ModelGovernance/Set', (route) => route.fulfill({json: {}}))
    await page.goto('/administration/governance')

    await page.getByTestId('governance-edit-a0000000-0000-0000-0000-000000000001').click()
    await expect(page.getByTestId('governance-save')).toBeDisabled()
    await page.getByTestId('governance-owner').fill('Head of Fraud')
    await page.getByTestId('governance-purpose').fill('Scores card payments')
    await page.getByTestId('governance-oversight').fill('Analysts review every alert')
    await page.getByTestId('governance-tier').selectOption('3')
    await page.getByTestId('governance-review').fill('2027-01-31')
    await page.getByTestId('governance-save').click()

    expect(body(await save)).toMatchObject({
      kindId: 1,
      modelGuid: 'a0000000-0000-0000-0000-000000000001',
      ownerUser: 'Head of Fraud',
      riskTierId: 3,
      nextReviewDate: '2027-01-31T00:00:00Z',
    })
  })
})

test.describe('Inbound queue page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  const dead = [
    {
      id: 7,
      entityAnalysisModelGuid: 'b0000000-0000-0000-0000-000000000001',
      attempts: 5,
      lastError: 'The model failed',
      enqueuedBy: 'integration',
      createdDate: iso,
    },
  ]

  test('shows the depth and the dead rows, and requeues and deletes them', async ({page}) => {
    await stub(page, [6])
    await page.route('**/api/InboundQueue/Depth', (route) =>
      route.fulfill({json: {pending: 12, processing: 3, dead: 1}}),
    )
    await page.route('**/api/InboundQueue/Dead', (route) => route.fulfill({json: dead}))
    const requeue = page.waitForRequest('**/api/InboundQueue/Dead/7/Requeue')
    await page.route('**/api/InboundQueue/Dead/7/Requeue', (route) => route.fulfill({json: {}}))
    await page.goto('/administration/queue')

    await expect(page.getByTestId('queue-pending')).toHaveText('12')
    await expect(page.getByTestId('queue-dead-count')).toHaveText('1')
    await expect(page.getByTestId('queue-table')).toContainText('The model failed')
    await page.getByTestId('queue-requeue-7').click()
    expect((await requeue).method()).toBe('POST')
  })

  test('deletes a dead row with the DELETE verb', async ({page}) => {
    await stub(page, [6])
    await page.route('**/api/InboundQueue/Depth', (route) =>
      route.fulfill({json: {pending: 0, processing: 0, dead: 1}}),
    )
    await page.route('**/api/InboundQueue/Dead', (route) => route.fulfill({json: dead}))
    const removal = page.waitForRequest('**/api/InboundQueue/Dead/7')
    await page.route('**/api/InboundQueue/Dead/7', (route) => route.fulfill({json: {}}))
    await page.goto('/administration/queue')

    await page.getByTestId('queue-delete-7').click()
    expect((await removal).method()).toBe('DELETE')
  })

  test('says so when nothing is dead', async ({page}) => {
    await stub(page, [6])
    await page.route('**/api/InboundQueue/Depth', (route) =>
      route.fulfill({json: {pending: 0, processing: 0, dead: 0}}),
    )
    await page.route('**/api/InboundQueue/Dead', (route) => route.fulfill({json: []}))
    await page.goto('/administration/queue')

    await expect(page.getByTestId('queue-empty')).toBeVisible()
  })
})

const replay = (guid: string, patch: Record<string, unknown> = {}) => ({
  guid,
  statusId: 3,
  agentDefinitionHash: 'abcdef0123456789',
  modelName: 'model-x',
  fromDate: iso,
  toDate: iso,
  asOfMinutes: 60,
  casesSelected: 50,
  scored: 48,
  unusable: 2,
  agreed: 40,
  missedRisk: 3,
  overEscalation: 5,
  inconclusive: 0,
  agreementRate: 0.8333,
  error: null,
  createdDate: iso,
  createdUser: 'sam',
  ...patch,
})

test.describe('Agent replay page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  const first = 'c0000000-0000-0000-0000-000000000001'
  const second = 'c0000000-0000-0000-0000-000000000002'
  const running = 'c0000000-0000-0000-0000-000000000003'

  test('starts a replay for a window', async ({page}) => {
    await stub(page, [18])
    await page.route('**/api/CaseAgentReplay', (route) => route.fulfill({json: []}))
    const start = page.waitForRequest('**/api/CaseAgentReplay/Start')
    await page.route('**/api/CaseAgentReplay/Start', (route) =>
      route.fulfill({json: replay(first)}),
    )
    await page.goto('/administration/replay')

    await expect(page.getByTestId('replay-start')).toBeDisabled()
    await page.getByTestId('replay-from').fill('2026-10-01')
    await page.getByTestId('replay-to').fill('2026-10-31')
    await page.getByTestId('replay-start').click()

    expect(body(await start)).toMatchObject({
      fromDate: '2026-10-01T00:00:00Z',
      toDate: '2026-10-31T23:59:59Z',
      maximumCases: 50,
      asOfMinutes: 60,
    })
  })

  test('compares exactly two completed replays', async ({page}) => {
    await stub(page, [18])
    await page.route('**/api/CaseAgentReplay', (route) =>
      route.fulfill({
        json: [
          replay(first),
          replay(second),
          replay(running, {statusId: 2, agreementRate: null}),
        ],
      }),
    )
    await page.route(`**/api/CaseAgentReplay/Compare/${first}/${second}`, (route) =>
      route.fulfill({
        json: {
          first: null,
          second: null,
          commonCases: 45,
          sameRecommendation: 39,
          firstAgreed: 30,
          secondAgreed: 33,
        },
      }),
    )
    await page.goto('/administration/replay')

    await expect(page.getByTestId(`replay-select-${running}`)).toBeDisabled()
    await expect(page.getByTestId('replay-compare')).toBeDisabled()
    await page.getByTestId(`replay-select-${first}`).check()
    await expect(page.getByTestId('replay-compare')).toBeDisabled()
    await page.getByTestId(`replay-select-${second}`).check()
    await page.getByTestId('replay-compare').click()
    await expect(page.getByTestId('replay-comparison')).toContainText('45 cases in common')
  })
})

const match = (id: number, patch: Record<string, unknown> = {}) => ({
  id,
  nodeIdA: 1,
  keyA: 'Account',
  valueA: 'ACC-1',
  nodeIdB: 2,
  keyB: 'Account',
  valueB: 'ACC-2',
  sourceId: 2,
  posterior: 0.92,
  evidence: 'Same name, similar address',
  decisionId: 1,
  decidedUser: null,
  decidedDate: null,
  decisionNote: null,
  resolvedEntityId: null,
  createdDate: iso,
  ...patch,
})

test.describe('Entity resolution page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('confirms a pending match with the confirmed decision', async ({page}) => {
    await stub(page, [51])
    await page.route('**/api/GraphEntityMatch?*', (route) => route.fulfill({json: [match(31)]}))
    const decide = page.waitForRequest('**/api/GraphEntityMatch/Decide')
    await page.route('**/api/GraphEntityMatch/Decide', (route) =>
      route.fulfill({json: match(31, {decisionId: 2})}),
    )
    await page.goto('/administration/resolution')

    await expect(page.getByTestId('resolution-31')).toContainText('92.0%')
    await page.getByTestId('resolution-confirm-31').click()
    expect(body(await decide)).toMatchObject({id: 31, decisionId: 2})
  })

  test('rejects with the rejected decision and offers nothing on a decided match', async ({
                                                                                            page,
                                                                                          }) => {
    await stub(page, [51])
    await page.route('**/api/GraphEntityMatch?*', (route) => {
      const decision = new URL(route.request().url()).searchParams.get('decisionId')

      return route.fulfill({
        json: decision === '1' ? [match(32)] : [match(33, {decisionId: 2})],
      })
    })
    const decide = page.waitForRequest('**/api/GraphEntityMatch/Decide')
    await page.route('**/api/GraphEntityMatch/Decide', (route) =>
      route.fulfill({json: match(32, {decisionId: 3})}),
    )
    await page.goto('/administration/resolution')

    await page.getByTestId('resolution-reject-32').click()
    expect(body(await decide)).toMatchObject({id: 32, decisionId: 3})

    await page.getByTestId('resolution-filter').selectOption('2')
    await expect(page.getByTestId('resolution-33')).toBeVisible()
    await expect(page.getByTestId('resolution-confirm-33')).toHaveCount(0)
  })
})

const proposal = (patch: Record<string, unknown> = {}) => ({
  guid: 'd0000000-0000-0000-0000-000000000001',
  keyName: 'Account',
  keyValue: 'ACC-9',
  tierId: 3,
  score: null,
  reviewByDate: null,
  reason: 'Adverse media',
  basedOnVersion: 1,
  stateId: 1,
  resultingVersion: null,
  requestedUser: 'sam',
  requestedDate: iso,
  decidedUser: null,
  decidedDate: null,
  decisionNote: null,
  ...patch,
})

test.describe('Entity risk ratings page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  const guid = 'd0000000-0000-0000-0000-000000000001'

  async function lists(page: Page, requests: unknown[]) {
    await page.route('**/api/EntityRiskRating/Requests', (route) =>
      route.fulfill({json: requests}),
    )
    await page.route('**/api/EntityRiskRating/Due', (route) => route.fulfill({json: []}))
  }

  test('an approver approves somebody elses proposal', async ({page}) => {
    await stub(page, [80])
    await lists(page, [proposal()])
    const approve = page.waitForRequest('**/api/EntityRiskRating/Approve')
    await page.route('**/api/EntityRiskRating/Approve', (route) =>
      route.fulfill({json: proposal({stateId: 2})}),
    )
    await page.goto('/administration/entity-risk')

    await page.getByTestId(`entity-risk-approve-${guid}`).click()
    expect(body(await approve)).toMatchObject({guid})
  })

  test('the proposer can retract but never approve their own proposal', async ({page}) => {
    await stub(page, [18, 80])
    await lists(page, [proposal({requestedUser: 'alex'})])
    await page.goto('/administration/entity-risk')

    await expect(page.getByTestId(`entity-risk-retract-${guid}`)).toBeVisible()
    await expect(page.getByTestId(`entity-risk-approve-${guid}`)).toHaveCount(0)
  })

  test('looks a key up and proposes a change with a reason', async ({page}) => {
    await stub(page, [18])
    await lists(page, [])
    await page.route('**/api/EntityRiskRating/ByKey*', (route) =>
      route.fulfill({
        json: {
          current: {
            keyName: 'Account',
            keyValue: 'ACC-9',
            version: 1,
            tierId: 1,
            score: null,
            sourceId: 1,
            reason: null,
            reviewByDate: null,
            createdUser: 'import',
            createdDate: iso,
          },
          history: [],
        },
      }),
    )
    const propose = page.waitForRequest('**/api/EntityRiskRating/Set')
    await page.route('**/api/EntityRiskRating/Set', (route) => route.fulfill({json: proposal()}))
    await page.goto('/administration/entity-risk')

    await page.getByTestId('entity-risk-key-name').fill('Account')
    await page.getByTestId('entity-risk-key-value').fill('ACC-9')
    await page.getByTestId('entity-risk-lookup').click()
    await expect(page.getByTestId('entity-risk-current')).toContainText('Low')

    await expect(page.getByTestId('entity-risk-propose')).toBeDisabled()
    await page.getByTestId('entity-risk-tier').selectOption('3')
    await page.getByTestId('entity-risk-reason').fill('Adverse media')
    await page.getByTestId('entity-risk-propose').click()
    expect(body(await propose)).toMatchObject({
      keyName: 'Account',
      keyValue: 'ACC-9',
      tierId: 3,
      reason: 'Adverse media',
    })
  })
})

test.describe('Administration menu', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('shows each new page only to the holders of its permission', async ({page}) => {
    await stub(page, [78])
    await page.route('**/api/Erasure', (route) => route.fulfill({json: []}))
    await page.goto('/administration/erasure')

    const nav = page.getByRole('navigation', {name: 'Main navigation'})
    await nav
      .locator('summary')
      .filter({hasText: /^\s*Administration\s*$/})
      .click()

    await expect(nav.getByRole('link', {name: 'Erasure and subject access'})).toBeVisible()
    await expect(nav.getByRole('link', {name: 'Model governance'})).toHaveCount(0)
    await expect(nav.getByRole('link', {name: 'Inbound queue'})).toHaveCount(0)
  })
})

test.describe('New admin pages accessibility', () => {
  test.use({viewport: {width: 1440, height: 900}})

  const cases: Array<{
    path: string
    permissions: number[]
    wait: string
    routes: Record<string, unknown>
  }> = [
    {
      path: '/administration/erasure',
      permissions: [78, 79],
      wait: 'adminErasure-page',
      routes: {'**/api/Erasure': [erasure()]},
    },
    {
      path: '/administration/governance',
      permissions: [6],
      wait: 'adminGovernance-page',
      routes: {'**/api/ModelGovernance': [inventoryRow()]},
    },
    {
      path: '/administration/queue',
      permissions: [6],
      wait: 'adminQueue-page',
      routes: {
        '**/api/InboundQueue/Depth': {pending: 1, processing: 0, dead: 0},
        '**/api/InboundQueue/Dead': [],
      },
    },
    {
      path: '/administration/replay',
      permissions: [18],
      wait: 'adminReplay-page',
      routes: {'**/api/CaseAgentReplay': [replay('c0000000-0000-0000-0000-000000000009')]},
    },
    {
      path: '/administration/resolution',
      permissions: [51],
      wait: 'adminResolution-page',
      routes: {'**/api/GraphEntityMatch?*': [match(40)]},
    },
    {
      path: '/administration/entity-risk',
      permissions: [18, 80],
      wait: 'adminEntityRisk-page',
      routes: {
        '**/api/EntityRiskRating/Requests': [proposal()],
        '**/api/EntityRiskRating/Due': [],
      },
    },
  ]

  for (const item of cases) {
    test(`has no violations on ${item.path} in light and dark`, async ({page}) => {
      await stub(page, item.permissions)

      for (const [pattern, json] of Object.entries(item.routes)) {
        await page.route(pattern, (route) => route.fulfill({json}))
      }

      await page.goto(item.path)
      await expect(page.getByTestId(item.wait)).toBeVisible()
      await settleAnimations(page)

      for (const scheme of ['light', 'dark'] as const) {
        await page.emulateMedia({colorScheme: scheme})
        await settleAnimations(page)
        expect((await new AxeBuilder({page}).analyze()).violations).toEqual([])
      }
    })
  }
})
