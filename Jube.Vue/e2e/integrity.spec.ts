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
import { me } from './hierarchyStub'
import { settleAnimations } from './support'

const report = {
  entityAnalysisModelId: 1,
  modelName: 'Payments',
  checkedDate: '2026-10-21T10:00:00Z',
  engineSource: 'in-process',
  errors: 1,
  warnings: 1,
  infos: 1,
  checks: [
    {
      code: 'R1',
      title: 'Rule references a missing counter',
      severity: 'Error',
      category: 'Dependencies',
      entityKind: 'ActivationRule',
      entityId: 5,
      entityName: 'High value',
      message: 'Counter Spend does not exist.',
    },
    {
      code: 'R2',
      title: 'Rule has no description',
      severity: 'Warning',
      category: 'Hygiene',
      entityKind: 'ActivationRule',
      entityId: 5,
      entityName: 'High value',
      message: 'Add a description.',
    },
    {
      code: 'M1',
      title: 'Model is active',
      severity: 'Info',
      category: 'Model',
      entityKind: null,
      entityId: null,
      entityName: null,
      message: 'Fine.',
    },
  ],
}

const engine = {
  source: 'in-process',
  nodes: [
    {
      instance: 'node-a',
      heartbeatDate: '2026-10-21T10:00:00Z',
      synchronisedDate: '2026-10-21T09:00:00Z',
    },
  ],
  instances: [
    {
      instance: 'node-a',
      started: true,
      capturedDate: '2026-10-21T10:00:00Z',
      loaded: [
        {
          kind: 'ActivationRule',
          count: 1,
          entities: [{ name: 'High value', id: 5, guid: null }],
        },
      ],
    },
  ],
}

const graph = {
  nodes: [
    {
      id: 'ActivationRule:5',
      label: 'High value',
      kind: 'ActivationRule',
      active: true,
      detail: null,
    },
    { id: 'TtlCounter:2', label: 'Spend', kind: 'TtlCounter', active: true, detail: null },
    { id: 'List:9', label: 'Loose list', kind: 'List', active: true, detail: null },
  ],
  edges: [{ from: 'ActivationRule:5', to: 'TtlCounter:2', label: 'Spend', dashed: false }],
  truncated: false,
  maxNodes: 200,
  focus: null,
}

const svg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 100"><g data-node="ActivationRule:5" tabindex="0"><rect class="nb" x="10" y="10" width="100" height="30"/></g><g data-node="TtlCounter:2" tabindex="0"><rect class="nb" x="150" y="10" width="100" height="30"/></g><script>window.hacked=1</script></svg>'

const pending = [
  {
    kind: 16,
    entityId: 5,
    name: 'High value',
    version: 3,
    deleted: false,
    makerUser: 'maker',
    parentId: null,
  },
  {
    kind: 16,
    entityId: 6,
    name: 'Old rule',
    version: 2,
    deleted: true,
    makerUser: 'maker',
    parentId: null,
  },
  {
    kind: 2,
    entityId: 10,
    name: 'Watch list',
    version: 2,
    deleted: false,
    makerUser: 'maker',
    parentId: null,
  },
  {
    kind: 3,
    entityId: 100,
    name: 'v1',
    version: 2,
    deleted: false,
    makerUser: 'maker',
    parentId: 10,
  },
]

interface Seen {
  posts: { path: string; body: Record<string, unknown> }[]
  currentQueries: string[]
}

async function stub(
  page: Page,
  options: { pendingStatus?: number; syncStatus?: number; permissions?: number[] } = {},
) {
  const seen: Seen = { posts: [], currentQueries: [] }

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [5, 60] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityAnalysisModelIntegrity/Models', (route) =>
    route.fulfill({
      json: [
        { id: 1, name: 'Payments', active: true },
        { id: 2, name: 'Cards', active: false },
      ],
    }),
  )
  await page.route('**/api/EntityAnalysisModelIntegrity/1', (route) =>
    route.fulfill({ json: report }),
  )
  await page.route('**/api/EntityAnalysisModelEngineState/1', (route) =>
    route.fulfill({ json: engine }),
  )
  await page.route('**/api/EntityAnalysisModelDependencyGraph/1', (route) =>
    route.fulfill({ json: graph }),
  )
  await page.route('**/api/EntityAnalysisModelDependencyGraph/1/Svg**', (route) =>
    route.fulfill({ contentType: 'image/svg+xml', body: svg }),
  )
  await page.route('**/api/EntityApproval/Pending**', (route) =>
    route.fulfill({
      status: options.pendingStatus ?? 200,
      json: options.pendingStatus ? {} : pending,
    }),
  )
  await page.route('**/api/EntityApproval/Approve', (route) => {
    seen.posts.push({ path: '/Approve', body: route.request().postDataJSON() })

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/EntityApproval/Reject', (route) => {
    seen.posts.push({ path: '/Reject', body: route.request().postDataJSON() })

    return route.fulfill({
      status: 400,
      json: { reasons: ['The maker cannot reject their own change.'] },
    })
  })
  await page.route('**/api/EntityApproval/ApproveValues', (route) => {
    seen.posts.push({ path: '/ApproveValues', body: route.request().postDataJSON() })

    return route.fulfill({ json: { approved: 1, refusals: ['One value was already approved.'] } })
  })
  await page.route('**/api/EntityAnalysisModelSynchronisationSchedule/ByCurrent**', (route) => {
    seen.currentQueries.push(new URL(route.request().url()).search)

    return route.fulfill({ json: { scheduleDate: '2026-10-21T12:00:00Z' } })
  })
  await page.route('**/api/EntityAnalysisModelSynchronisationSchedule', (route) => {
    if (route.request().method() !== 'POST') return route.fallback()

    seen.posts.push({ path: '/Schedule', body: route.request().postDataJSON() })

    return options.syncStatus
      ? route.fulfill({ status: options.syncStatus, body: 'ScheduleDateTooFarInFuture' })
      : route.fulfill({ json: { scheduleDate: '2026-10-21T12:00:00Z' } })
  })
  await page.route('**/api/GetEntityAnalysisModelSynchronisationNodeStatusEntries', (route) =>
    route.fulfill({
      json: [
        {
          id: 1,
          instance: 'node-a',
          heartbeatDate: '2026-10-21T10:00:00Z',
          synchronisedDate: '2026-10-21T09:00:00Z',
          instanceAvailable: true,
          synchronisationPending: false,
        },
        {
          id: 2,
          instance: 'node-b',
          heartbeatDate: null,
          synchronisedDate: null,
          instanceAvailable: false,
          synchronisationPending: true,
        },
      ],
    }),
  )

  return seen
}

async function open(page: Page) {
  await page.goto('/tools?tool=integrity')
  await expect(page.getByTestId('integrity-summary')).toBeVisible()
}

test.describe('Integrity', () => {
  test.use({ viewport: { width: 1500, height: 1300 } })

  test('opens on the first model, summarises the findings and opens a row onto its checks', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('model-picker')).toHaveValue('1')
    await expect(page.getByTestId('integrity-summary')).toContainText(
      'Errors 1 · Warnings 1 · Information 1',
    )
    await expect(page.locator('[aria-label="Findings by entity"] .tabulator-row')).toHaveCount(2)

    await page
      .locator('[aria-label="Findings by entity"] .tabulator-row')
      .first()
      .getByTestId('grid-expand')
      .click()

    await expect(page.getByText('Rule references a missing counter')).toBeVisible()
    await expect(page.getByText('Add a description.')).toBeVisible()
  })

  test('leads with a verdict and starts with the findings and the engine open, the rest closed', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('integrity-verdict')).toHaveText('Not sound')
    await expect(page.getByTestId('integrity-findings')).toBeVisible()
    await expect(page.getByTestId('canvas-index').getByRole('button')).toHaveText([
      'What is wrong',
      'Is the engine running it',
      'What it depends on',
      'What is waiting for approval',
    ])
    await expect(page.getByTestId('integrity-tab-findings')).toHaveCount(0)
    await expect(page.getByTestId('dependency-diagram')).toHaveCount(0)
  })

  test('the verdict is sound, or sound with warnings, when there are no errors', async ({
    page,
  }) => {
    await stub(page)
    await page.route('**/api/EntityAnalysisModelIntegrity/1', (route) =>
      route.fulfill({ json: { ...report, errors: 0, warnings: 2, checks: [] } }),
    )
    await open(page)

    await expect(page.getByTestId('integrity-verdict')).toHaveText('Sound, with warnings')

    await page.route('**/api/EntityAnalysisModelIntegrity/1', (route) =>
      route.fulfill({ json: { ...report, errors: 0, warnings: 0, checks: [] } }),
    )
    await page.getByTestId('integrity-check').click()

    await expect(page.getByTestId('integrity-verdict')).toHaveText('Sound')
  })

  test('opening a closed section from the index records it in the address', async ({ page }) => {
    await stub(page)
    await open(page)

    await page.getByTestId('canvas-index-pending').click()

    await expect(page).toHaveURL(/section=pending/)
    await expect(page.getByTestId('canvas-pending')).toBeVisible()
  })

  test('the engine section shows each instance and what it has loaded, and schedules a synchronisation', async ({
    page,
  }) => {
    const seen = await stub(page)

    await open(page)
    await page.getByTestId('canvas-index-engine').click()

    await expect(page.locator('[aria-label="Engine instances"] .tabulator-row')).toHaveCount(1)
    await expect(
      page.locator('[aria-label="Engine instances"]').getByText('Started', { exact: true }),
    ).toBeVisible()
    await expect(
      page.getByTestId('model-synchronisation').getByText('Unavailable', { exact: true }),
    ).toBeVisible()
    await expect(
      page.getByTestId('model-synchronisation').getByText('Synchronised', { exact: true }).first(),
    ).toBeVisible()

    await page
      .locator('[aria-label="Engine instances"] .tabulator-row')
      .first()
      .getByTestId('grid-expand')
      .click()
    await expect(page.getByText('Loaded for this model')).toBeVisible()

    await page.getByTestId('sync-now').click()
    await expect.poll(() => seen.posts.length).toBe(1)
    expect(seen.posts[0]).toEqual({ path: '/Schedule', body: { EntityAnalysisModelId: 1 } })
    expect(seen.currentQueries).toContain('?entityAnalysisModelId=1')

    await page.getByTestId('sync-when').fill('2026-10-22T09:30')
    await page.getByTestId('sync-schedule').click()
    await expect.poll(() => seen.posts.length).toBe(2)
    expect(String(seen.posts[1]!.body.ScheduleDate)).toBe(
      new Date('2026-10-22T09:30').toISOString(),
    )
    expect(seen.posts[1]!.body.EntityAnalysisModelId).toBe(1)
  })

  test('without the synchronise permission the instances are shown but there is nothing to schedule', async ({
    page,
  }) => {
    await stub(page, { permissions: [60] })
    await open(page)
    await page.getByTestId('canvas-index-engine').click()

    await expect(page.getByTestId('model-synchronisation')).toContainText('node-a')
    await expect(page.getByTestId('sync-now')).toHaveCount(0)
    await expect(page.getByTestId('sync-schedule')).toHaveCount(0)
  })

  test('a schedule too far ahead is explained rather than failing silently', async ({ page }) => {
    await stub(page, { syncStatus: 400 })
    await open(page)
    await page.getByTestId('canvas-index-engine').click()
    await page.getByTestId('sync-now').click()

    await expect(page.getByTestId('sync-problems')).toContainText('too far in the future')
  })

  test('the dependency diagram selects an entity by click, strips anything unsafe and lists the loose entities', async ({
    page,
  }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('canvas-index-dependencies').click()

    const chart = page.getByTestId('dependency-diagram').getByTestId('jube-chart')

    await expect(chart.locator('[data-node]')).toHaveCount(2)
    await expect(chart.locator('script')).toHaveCount(0)
    await expect(page.getByTestId('dependency-unconnected')).toContainText('1 entities')

    await chart.locator('[data-node="TtlCounter:2"]').click()

    await expect(page.getByTestId('dependency-selected')).toContainText('TtlCounter:2')
    await expect(chart.locator('[data-node="TtlCounter:2"]')).toHaveAttribute(
      'data-selected',
      'true',
    )

    await page.keyboard.press('Escape')
    await expect(page.getByTestId('dependency-selected')).toHaveCount(0)

    await expect(page.locator('[aria-label="Entities in the model"] .tabulator-row')).toHaveCount(3)
    await page.getByTestId('dependency-filter').fill('Loose')
    await expect(page.locator('[aria-label="Entities in the model"] .tabulator-row')).toHaveCount(1)
  })

  test('choosing show on the entity list selects it on the diagram', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('canvas-index-dependencies').click()

    await page
      .locator('[aria-label="Entities in the model"] .tabulator-row', { hasText: 'High value' })
      .getByTestId('grid-action-show')
      .click()

    await expect(page.getByTestId('dependency-selected')).toContainText('ActivationRule:5')
  })

  test('pending approvals group by kind, approve and reject with the real reasons, and approve list values', async ({
    page,
  }) => {
    const seen = await stub(page)

    await open(page)
    await page.getByTestId('canvas-index-pending').click()

    await expect(
      page.locator('[aria-label="Pending approvals by kind"] .tabulator-row'),
    ).toHaveCount(2)

    await page
      .locator('[aria-label="Pending approvals by kind"] .tabulator-row', {
        hasText: 'Activation rule',
      })
      .getByTestId('grid-expand')
      .click()

    const row = page.locator('[aria-label="Entities awaiting approval"] .tabulator-row', {
      hasText: 'High value',
    })

    await row.getByTestId('grid-action-approve').click()
    await expect.poll(() => seen.posts.length).toBe(1)
    expect(seen.posts[0]).toEqual({
      path: '/Approve',
      body: { kind: 16, entityId: 5, version: 3, note: null },
    })

    await row.getByTestId('grid-action-reject').click()
    await expect(page.getByTestId('pending-problems')).toContainText(
      'The maker cannot reject their own change.',
    )

    await page
      .locator('[aria-label="Pending approvals by kind"] .tabulator-row', { hasText: 'List' })
      .getByTestId('grid-expand')
      .click()
    await page
      .locator('[aria-label="Entities awaiting approval"] .tabulator-row', {
        hasText: 'Watch list',
      })
      .getByTestId('grid-action-values')
      .click()

    await expect(page.getByTestId('pending-note')).toContainText('Approved 1 value(s).')
    await expect(page.getByTestId('pending-problems')).toContainText(
      'One value was already approved.',
    )
    expect(seen.posts.at(-1)).toEqual({ path: '/ApproveValues', body: { kind: 2, entityId: 10 } })
  })

  test('a user who may not see pending approvals is told so', async ({ page }) => {
    await stub(page, { pendingStatus: 403 })
    await open(page)
    await page.getByTestId('canvas-index-pending').click()

    await expect(page.getByTestId('pending-error')).toContainText(
      'do not have permission to view pending approvals',
    )
  })

  test('has no accessibility violations with every section opened from the index', async ({
    page,
  }) => {
    await stub(page)
    await open(page)

    for (const tab of ['findings', 'engine', 'dependencies', 'pending']) {
      await page.getByTestId(`canvas-index-${tab}`).click()
      await settleAnimations(page)

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations, tab).toEqual([])
    }
  })
})
