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

const me = (permissions: number[]) => ({
  userName: 'alex',
  landlord: false,
  permissions,
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
})

const snapshot = (id: number, name: string | null, overrides: Record<string, unknown> = {}) => ({
  id,
  guid: `g${id}`,
  snapshotSourceId: 1,
  name,
  exportVersion: 2,
  exportGuid: null,
  entityAnalysisModelCount: 3,
  bytes: 4096,
  inError: false,
  createdUser: 'alex',
  createdDate: '2026-10-21T09:30:00Z',
  completedDate: '2026-10-21T09:30:02Z',
  ...overrides,
})

const diff = (overrides: Record<string, unknown> = {}) => ({
  id: 12,
  diffKindId: 2,
  left: { snapshotId: 2, label: 'Nightly', createdDate: '2026-10-21T09:30:00Z' },
  right: { snapshotId: null, label: 'Live', createdDate: null },
  identical: false,
  added: 1,
  removed: 0,
  changed: 1,
  unchanged: 9,
  referencesIgnored: 0,
  truncated: false,
  sections: [{ name: 'EntityAnalysisModel', added: 1, removed: 0, changed: 1, unchanged: 9 }],
  entries: [
    {
      section: 'EntityAnalysisModel',
      path: ['EntityAnalysisModel[Fraud]'],
      kindId: 3,
      changes: [{ property: 'Colour', before: 'red', after: 'blue' }],
      nestedCount: 0,
    },
  ],
  ...overrides,
})

const restorePreview = (blockers: unknown[] = []) => ({
  diff: diff({ id: 13, diffKindId: 4 }),
  packageVersion: 2,
  packageGuid: 'g1',
  warnings: [],
  blockers,
  orphanedUsers: { count: 0, names: [], importerStaysActive: true },
})

interface Options {
  permissions?: number[]
  snapshots?: unknown[]
  listStatus?: number
  diffStatus?: number
}

async function stub(page: Page, calls: string[] = [], options: Options = {}) {
  const list = options.snapshots ?? [
    snapshot(2, 'Nightly'),
    snapshot(1, null, { snapshotSourceId: 4 }),
  ]

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: me(options.permissions ?? [38, 63]) }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/PreservationSnapshot?**', (route) =>
    options.listStatus
      ? route.fulfill({ status: options.listStatus, body: '' })
      : route.fulfill({ json: list }),
  )
  await page.route('**/api/PreservationSnapshot', (route) => {
    calls.push(`Take ${route.request().postData() ?? ''}`)

    return route.fulfill({ json: snapshot(3, 'Fresh') })
  })
  await page.route('**/api/PreservationSnapshot/Diff', (route) => {
    calls.push(`Diff ${route.request().postData() ?? ''}`)

    return options.diffStatus
      ? route.fulfill({ status: options.diffStatus, body: '' })
      : route.fulfill({ json: diff() })
  })
  await page.route('**/api/PreservationSnapshot/*/ImportPreview', (route) => {
    calls.push('RestorePreview')

    return route.fulfill({ json: restorePreview() })
  })
  await page.route('**/api/PreservationSnapshot/*/Import', (route) => {
    calls.push('Restore')

    return route.fulfill({ json: snapshot(4, 'Before import of x') })
  })
  await page.route('**/api/PreservationSnapshot/*/Body', (route) =>
    route.fulfill({
      contentType: 'application/json',
      headers: { 'Content-Disposition': 'attachment; filename="snapshot-2.json"' },
      body: '{}',
    }),
  )
  await page.route('**/api/PreservationSnapshot/2', (route) => {
    calls.push(`${route.request().method()} 2`)

    return route.fulfill({ status: 200, body: '' })
  })
}

async function open(page: Page) {
  await page.goto('/administration/preservation?tab=snapshots')
  await expect(page.getByTestId('preservation-safeguard')).toBeVisible()
}

test.describe('preservation safeguard', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('lists snapshots with their source in words', async ({ page }) => {
    await stub(page)
    await open(page)

    const grid = page.getByTestId('preservation-snapshots')

    await expect(grid).toContainText('Nightly')
    await expect(grid).toContainText('Before import')
    await expect(grid).toContainText('Snapshot 1')
  })

  test('shows an empty state', async ({ page }) => {
    await stub(page, [], { snapshots: [] })
    await open(page)

    await expect(page.getByTestId('preservation-snapshots')).toContainText(
      'No snapshots have been taken yet.',
    )
  })

  test('says so when the list cannot be loaded', async ({ page }) => {
    await stub(page, [], { listStatus: 403 })
    await open(page)

    await expect(page.getByTestId('safeguard-error')).toBeVisible()
  })

  test('takes a snapshot with the name given', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page.getByTestId('take-name').fill('Before the audit')
    await page.getByTestId('take-run').click()

    await expect
      .poll(() => calls.find((call) => call.startsWith('Take')))
      .toContain('"name":"Before the audit"')
  })

  test('defaults the comparison to the newest snapshot against live', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)

    await expect(page.getByTestId('compare-left')).toHaveValue('2')
    await expect(page.getByTestId('compare-right')).toHaveValue('live')
    await page.getByTestId('compare-run').click()

    await expect(page.getByTestId('compare-heading')).toContainText('Nightly')
    await expect(page.getByTestId('compare-heading')).toContainText('comparison 12')
    await expect(page.getByTestId('preview-changed')).toHaveText('1')
    expect(calls.find((call) => call.startsWith('Diff'))).toContain('"leftSnapshotId":2')
  })

  test('refuses to compare a side with itself', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('compare-left').selectOption('live')

    await expect(page.getByTestId('compare-same')).toBeVisible()
    await expect(page.getByTestId('compare-run')).toBeDisabled()
  })

  test('reports a failed comparison', async ({ page }) => {
    await stub(page, [], { diffStatus: 500 })
    await open(page)
    await page.getByTestId('compare-run').click()

    await expect(page.getByTestId('compare-failed')).toBeVisible()
  })

  test('compares a row against live from its action', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page.getByTestId('compare-left').selectOption('live')
    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Compare' })
      .first()
      .click()

    await expect(page.getByTestId('compare-heading')).toBeVisible()
    await expect(page.getByTestId('compare-left')).toHaveValue('2')
  })

  test('previews a restore and needs confirmation before restoring', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Restore' })
      .first()
      .click()

    await expect(page.getByTestId('restore-target')).toContainText('Nightly')
    await expect(page.getByTestId('preservation-preview')).toBeVisible()
    await expect(page.getByTestId('restore-run')).toBeDisabled()
    await page.getByTestId('restore-confirm').check()
    await page.getByTestId('restore-run').click()

    await expect(page.getByTestId('preservation-restore')).toBeHidden()
    expect(calls).toContain('Restore')
  })

  test('cannot restore without the import permission', async ({ page }) => {
    await stub(page, [], { permissions: [38] })
    await open(page)
    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Restore' })
      .first()
      .click()

    await expect(page.getByTestId('restore-not-permitted')).toBeVisible()
    await expect(page.getByTestId('restore-confirm')).toBeDisabled()
    await expect(page.getByTestId('restore-run')).toBeDisabled()
  })

  test('closing the restore panel leaves everything untouched', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Restore' })
      .first()
      .click()
    await page.getByTestId('restore-close').click()

    await expect(page.getByTestId('preservation-restore')).toBeHidden()
    expect(calls).not.toContain('Restore')
  })

  test('downloads the body of a snapshot', async ({ page }) => {
    await stub(page)
    await open(page)

    const download = page.waitForEvent('download')

    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Download' })
      .first()
      .click()

    expect((await download).suggestedFilename()).toBe('snapshot-2.json')
  })

  test('deletes only after confirmation', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Delete' })
      .first()
      .click()
    await page.getByTestId('snapshot-confirm-no').click()
    expect(calls).not.toContain('DELETE 2')

    await page
      .getByTestId('preservation-snapshots')
      .getByRole('button', { name: 'Delete' })
      .first()
      .click()
    await page.getByTestId('snapshot-confirm-yes').click()

    await expect.poll(() => calls).toContain('DELETE 2')
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme`, async ({ page }) => {
      await stub(page)
      await open(page)
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await page.getByTestId('compare-run').click()
      await expect(page.getByTestId('compare-heading')).toBeVisible()
      await page
        .getByTestId('preservation-snapshots')
        .getByRole('button', { name: 'Restore' })
        .first()
        .click()
      await expect(page.getByTestId('preservation-preview')).toBeVisible()
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="preservation-safeguard"]')
        .analyze()

      expect(
        results.violations.map(
          (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
        ),
      ).toEqual([])
    })
  }

  test('has no accessibility violations in Arabic', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await page.getByTestId('compare-run').click()
    await expect(page.getByTestId('compare-heading')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="preservation-safeguard"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
