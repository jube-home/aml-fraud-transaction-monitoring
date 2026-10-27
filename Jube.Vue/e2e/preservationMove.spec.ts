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

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [38],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const summary = {
  totalBytes: 9000,
  minimumPasswordLength: 12,
  sections: [
    { key: 'models', count: 2, rows: 14, bytes: 4000 },
    { key: 'approvals', count: 3, rows: 0, bytes: 500 },
    { key: 'visualisations', count: 1, rows: 2, bytes: 700 },
    { key: 'roles', count: 2, rows: 6, bytes: 900 },
    { key: 'lists', count: 1, rows: 20, bytes: 1500 },
    { key: 'dictionaries', count: 1, rows: 5, bytes: 400 },
    { key: 'exhaustive', count: 1, rows: 3000, bytes: 250000 },
    { key: 'askJoobyAnalytics', count: 0, rows: 0, bytes: 0 },
  ],
}

const preview = (overrides: Record<string, unknown> = {}) => ({
  diff: {
    id: 7,
    diffKindId: 3,
    left: { snapshotId: null, label: 'Live', createdDate: null },
    right: { snapshotId: null, label: 'pkg', createdDate: null },
    identical: false,
    added: 3,
    removed: 1,
    changed: 2,
    unchanged: 40,
    referencesIgnored: 4,
    truncated: false,
    sections: [{ name: 'EntityAnalysisModel', added: 3, removed: 1, changed: 2, unchanged: 40 }],
    entries: [
      {
        section: 'EntityAnalysisModel',
        path: ['EntityAnalysisModel[Fraud]', 'EntityAnalysisModelTag[high]'],
        kindId: 3,
        changes: [{ property: 'Colour', before: 'red', after: 'blue' }],
        nestedCount: 0,
      },
      {
        section: 'RoleRegistry',
        path: ['RoleRegistry[Analyst]'],
        kindId: 1,
        changes: [],
        nestedCount: 2,
      },
    ],
  },
  packageVersion: 2,
  packageGuid: '11111111-1111-1111-1111-111111111111',
  warnings: [{ code: 'NoApprovalHistory', message: 'The package carries no approval history.' }],
  blockers: [],
  orphanedUsers: { count: 2, names: ['alex', 'sam'], importerStaysActive: true },
  ...overrides,
})

interface Options {
  previewStatus?: number
  previewBody?: unknown
  importStatus?: number
}

async function stub(page: Page, calls: string[] = [], options: Options = {}) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/Preservation/Summary', (route) => route.fulfill({ json: summary }))
  await page.route('**/api/Preservation/Export', (route) => {
    calls.push(`Export ${route.request().postData() ?? ''}`)

    return route.fulfill({
      status: 200,
      contentType: 'application/octet-stream',
      headers: { 'Content-Disposition': 'attachment; filename="abc.jemp"' },
      body: 'package-bytes',
    })
  })
  await page.route('**/api/Preservation/ImportPreview', (route) => {
    calls.push('ImportPreview')

    return options.previewStatus
      ? route.fulfill({ status: options.previewStatus, body: '' })
      : route.fulfill({ json: options.previewBody ?? preview() })
  })
  await page.route('**/api/Preservation/Import', (route) => {
    calls.push(
      `Import ${route.request().postData()?.includes('hunter2-hunter2') ? 'with-password' : 'no-password'}`,
    )

    return route.fulfill({ status: options.importStatus ?? 200, body: '' })
  })
}

async function open(page: Page, password = 'correct-horse-battery') {
  await page.goto('/administration/preservation?tab=move')
  await expect(page.getByTestId('preservation-move')).toBeVisible()

  if (password) await page.getByTestId('preservation-password').fill(password)
}

async function choose(page: Page) {
  await page.getByTestId('import-file').setInputFiles({
    name: 'export.jemp',
    mimeType: 'application/octet-stream',
    buffer: Buffer.from('bytes'),
  })
}

test.describe('preservation move', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('shows what each switch carries and keeps the largest off', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('flag-lists-count')).toContainText('Items 1, nested 20')
    await expect(page.getByTestId('flag-lists-size')).toContainText('1.5 kB')
    await expect(page.getByTestId('flag-exhaustive-toggle')).not.toBeChecked()
    await expect(page.getByTestId('flag-roles-toggle')).toBeChecked()
    await expect(page.getByTestId('export-total')).toContainText('8 kB')

    await page.getByTestId('flag-exhaustive-toggle').check()

    await expect(page.getByTestId('export-total')).toContainText('258 kB')
  })

  test('holds export and import until the password is long enough', async ({ page }) => {
    await stub(page)
    await open(page, '')
    await choose(page)

    await expect(page.getByTestId('password-short')).toContainText('12')
    await expect(page.getByTestId('download-button')).toBeDisabled()
    await expect(page.getByTestId('import-preview')).toBeDisabled()

    await page.getByTestId('preservation-password').fill('elevenchars')

    await expect(page.getByTestId('password-short')).toBeVisible()
    await expect(page.getByTestId('download-button')).toBeDisabled()

    await page.getByTestId('preservation-password').fill('twelve-chars')

    await expect(page.getByTestId('password-short')).toHaveCount(0)
    await expect(page.getByTestId('download-button')).toBeEnabled()
    await expect(page.getByTestId('import-preview')).toBeEnabled()
  })

  test('downloads a package with the chosen options and password', async ({ page }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page.getByTestId('preservation-password').fill('hunter2-hunter2')
    await page.getByTestId('flag-lists-toggle').uncheck()

    const download = page.waitForEvent('download')

    await page.getByTestId('download-button').click()

    expect((await download).suggestedFilename()).toBe('abc.jemp')

    const body = JSON.parse(calls.find((call) => call.startsWith('Export'))!.slice(7))

    expect(body).toMatchObject({
      password: 'hunter2-hunter2',
      lists: false,
      roles: true,
      exhaustive: false,
    })
  })

  test('previews an import before changing anything and imports only after confirmation', async ({
    page,
  }) => {
    const calls: string[] = []

    await stub(page, calls)
    await open(page)
    await page.getByTestId('preservation-password').fill('hunter2-hunter2')
    await choose(page)
    await page.getByTestId('import-preview').click()

    await expect(page.getByTestId('preview-added')).toHaveText('3')
    await expect(page.getByTestId('preview-removed')).toHaveText('1')
    await expect(page.getByTestId('preview-warning-NoApprovalHistory')).toBeVisible()
    await expect(page.getByTestId('preview-orphans')).toContainText(
      'Orphaned Preservation Import role: 2',
    )
    await expect(page.getByTestId('preview-orphans')).toContainText('You stay active')
    await expect(
      page.getByText('EntityAnalysisModel[Fraud] › EntityAnalysisModelTag[high]'),
    ).toBeVisible()
    await expect(page.getByTestId('import-run')).toBeDisabled()
    expect(calls).toEqual(['ImportPreview'])

    await page.getByTestId('import-confirm').check()
    await page.getByTestId('import-run').click()

    await expect(page.getByTestId('import-done')).toBeVisible()
    expect(calls).toEqual(['ImportPreview', 'Import with-password'])
    await expect(page.getByTestId('preservation-preview')).toHaveCount(0)
  })

  test('refuses to import when the preview has a blocker', async ({ page }) => {
    await stub(page, [], {
      previewBody: preview({
        blockers: [{ code: 'RoleRekey', message: 'Roles already exist in another tenant.' }],
      }),
    })
    await open(page)
    await choose(page)
    await page.getByTestId('import-preview').click()

    await expect(page.getByTestId('preview-blocker')).toContainText('another tenant')
    await expect(page.getByTestId('import-confirm')).toBeDisabled()
    await expect(page.getByTestId('import-run')).toBeDisabled()
  })

  test('says so when the file is not a valid package', async ({ page }) => {
    await stub(page, [], { previewStatus: 400 })
    await open(page)
    await choose(page)
    await page.getByTestId('import-preview').click()

    await expect(page.getByTestId('import-problem')).toContainText('could not be read as a package')
    await expect(page.getByTestId('preservation-preview')).toHaveCount(0)
  })

  test('asks for a file before previewing', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('import-preview')).toBeDisabled()
  })

  test('drops a preview when the password changes so it cannot be imported stale', async ({
    page,
  }) => {
    await stub(page)
    await open(page)
    await choose(page)
    await page.getByTestId('import-preview').click()
    await expect(page.getByTestId('preservation-preview')).toBeVisible()

    await page.getByTestId('preservation-password').fill('changed')

    await expect(page.getByTestId('preservation-preview')).toHaveCount(0)
  })

  test('says so when the import itself fails', async ({ page }) => {
    await stub(page, [], { importStatus: 500 })
    await open(page)
    await choose(page)
    await page.getByTestId('import-preview').click()
    await page.getByTestId('import-confirm').check()
    await page.getByTestId('import-run').click()

    await expect(page.getByTestId('import-problem')).toContainText(
      'could not be previewed or imported',
    )
    await expect(page.getByTestId('import-done')).toHaveCount(0)
  })
})

test.describe('preservation move accessibility', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no violations with a preview open in the ${theme} theme`, async ({ page }) => {
      await stub(page)
      await open(page)
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await choose(page)
      await page.getByTestId('import-preview').click()
      await expect(page.getByTestId('preservation-preview')).toBeVisible()
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="preservation-move"]')
        .analyze()

      expect(
        results.violations.map(
          (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
        ),
      ).toEqual([])
    })
  }

  test('has no violations in Arabic', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await choose(page)
    await page.getByTestId('import-preview').click()
    await expect(page.getByTestId('preservation-preview')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="preservation-move"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
