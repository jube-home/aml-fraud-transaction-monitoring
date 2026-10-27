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
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

async function stub(page: Page) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
}

test.describe('download and upload benches', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test.beforeEach(async ({ page }) => {
    await stub(page)
  })

  test('saves a file under the fallback name', async ({ page }) => {
    await page.goto('/tools?tool=download-button')

    const download = page.waitForEvent('download')

    await page.getByTestId('download-button').click()

    expect((await download).suggestedFilename()).toBe('bench.txt')
    await expect(page.getByTestId('bench-saved')).toContainText('bench.txt')
  })

  test('prefers the name the server gives', async ({ page }) => {
    await page.goto('/tools?tool=download-button')
    await page.getByTestId('bench-outcome').selectOption('named')

    const download = page.waitForEvent('download')

    await page.getByTestId('download-button').click()

    expect((await download).suggestedFilename()).toBe('named-by-server.txt')
  })

  test('treats an empty file and a failure as a failed download', async ({ page }) => {
    await page.goto('/tools?tool=download-button')

    for (const outcome of ['empty', 'fails']) {
      await page.getByTestId('bench-outcome').selectOption(outcome)
      await page.getByTestId('download-button').click()

      await expect(page.getByTestId('bench-failed')).toBeVisible()
      await expect(page.getByTestId('bench-saved')).toHaveCount(0)
    }
  })

  test('uploads an allowed file and clears the input', async ({ page }) => {
    await page.goto('/tools?tool=antiforgery-upload')
    await page.getByTestId('file-upload-input').setInputFiles({
      name: 'rows.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from('a,b'),
    })
    await page.getByTestId('file-upload-send').click()

    await expect(page.getByTestId('bench-uploaded')).toContainText('rows.csv 3')
    await expect(page.getByTestId('file-upload-input')).toHaveValue('')
  })

  test('refuses nothing chosen, the wrong type and an empty file before calling', async ({
    page,
  }) => {
    await page.goto('/tools?tool=antiforgery-upload')
    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toContainText('Choose a file first.')

    await page.getByTestId('file-upload-input').setInputFiles({
      name: 'rows.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('a'),
    })
    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toContainText('.csv, .json')

    await page.getByTestId('file-upload-input').setInputFiles({
      name: 'rows.csv',
      mimeType: 'text/csv',
      buffer: Buffer.alloc(0),
    })
    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toContainText('The chosen file is empty.')
    await expect(page.getByTestId('bench-uploaded')).toHaveCount(0)
  })

  test('reports a failed upload and keeps the chosen file', async ({ page }) => {
    await page.goto('/tools?tool=antiforgery-upload')
    await page.getByTestId('bench-fails').check()
    await page.getByTestId('file-upload-input').setInputFiles({
      name: 'rows.json',
      mimeType: 'application/json',
      buffer: Buffer.from('{}'),
    })
    await page.getByTestId('file-upload-send').click()

    await expect(page.getByTestId('bench-failed')).toBeVisible()
  })

  for (const tool of ['download-button', 'antiforgery-upload']) {
    for (const theme of ['light', 'dark'] as const) {
      test(`${tool} has no accessibility violations in the ${theme} theme`, async ({ page }) => {
        await page.goto(`/tools?tool=${tool}`)
        await page.getByTestId('account-button').click()
        await setTheme(page, theme)
        await page.keyboard.press('Escape')
        await settleAnimations(page)

        const results = await new AxeBuilder({ page }).include('main').analyze()

        expect(
          results.violations.map(
            (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
          ),
        ).toEqual([])
      })
    }

    test(`${tool} has no accessibility violations in Arabic`, async ({ page }) => {
      await page.goto(`/tools?tool=${tool}`)
      await page.getByTestId('account-button').click()
      await setLanguage(page, 'ar')
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      const results = await new AxeBuilder({ page }).include('main').analyze()

      expect(
        results.violations.map(
          (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
        ),
      ).toEqual([])
    })
  }
})
