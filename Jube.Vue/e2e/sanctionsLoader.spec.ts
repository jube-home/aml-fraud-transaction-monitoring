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

interface Seen {
  imports: { body: string; token: string | undefined }[]
}

async function stub(
  page: Page,
  options: { landlord?: boolean; status?: number; json?: object } = {},
) {
  const seen: Seen = { imports: [] }

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, landlord: options.landlord ?? true } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({ json: { id: 1, name: 'Tenant one' } }),
  )
  await page.route('**/api/TenantRegistry/ByFilter*', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Tenant one' }] }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 'tok' } }))
  await page.route('**/api/SanctionEntrySource', (route) =>
    route.fulfill({
      json: [
        { id: 3, name: 'OFAC SDN' },
        { id: 4, name: 'UK HMT' },
      ],
    }),
  )
  await page.route('**/api/SanctionEntrySource/Import', (route) => {
    seen.imports.push({
      body: route.request().postData() ?? '',
      token: route.request().headers()['x-xsrf-token'],
    })

    return options.status && options.status !== 200
      ? route.fulfill({ status: options.status, json: options.json ?? {} })
      : route.fulfill({ status: 200, body: '' })
  })

  return seen
}

async function open(page: Page) {
  await page.goto('/tools?tool=sanctions-loader')
  await expect(page.getByTestId('sanctions-loader')).toBeVisible()
}

const csv = (name = 'sdn.csv', text = 'a,b\n1,2\n') => ({
  name,
  mimeType: 'text/csv',
  buffer: Buffer.from(text),
})

test.describe('Sanctions loader', () => {
  test.use({ viewport: { width: 1300, height: 1000 } })

  test('uploads the file for the chosen source with the antiforgery header', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await expect(page.getByTestId('sanctions-source')).toHaveValue('3')
    await page.getByTestId('sanctions-source').selectOption('4')
    await page.getByTestId('file-upload-input').setInputFiles(csv())
    await page.getByTestId('file-upload-send').click()

    await expect(page.getByTestId('sanctions-loaded')).toContainText('sdn.csv')
    expect(seen.imports).toHaveLength(1)
    expect(seen.imports[0]!.token).toBe('tok')
    expect(seen.imports[0]!.body).toMatch(/name="id"\s+4/)
    expect(seen.imports[0]!.body).toContain('name="files"; filename="sdn.csv"')
  })

  test('refuses a missing, mistyped or empty file without calling the server', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toContainText('Choose a file first.')

    await page.getByTestId('file-upload-input').setInputFiles(csv('list.xlsx'))
    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toContainText('Only .csv files')

    await page.getByTestId('file-upload-input').setInputFiles(csv('empty.csv', ''))
    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toContainText('is empty')

    expect(seen.imports).toHaveLength(0)
  })

  test('shows the reasons the server refuses a file with', async ({ page }) => {
    await stub(page, {
      status: 400,
      json: { errors: [{ propertyName: 'File', errorMessage: 'The file has no header row.' }] },
    })
    await open(page)

    await page.getByTestId('file-upload-input').setInputFiles(csv())
    await page.getByTestId('file-upload-send').click()

    await expect(page.getByTestId('sanctions-problems')).toContainText(
      'The file has no header row.',
    )
    await expect(page.getByTestId('file-upload-send')).toBeEnabled()
  })

  test('says so when the server refuses the user', async ({ page }) => {
    await stub(page, { status: 403 })
    await open(page)

    await page.getByTestId('file-upload-input').setInputFiles(csv())
    await page.getByTestId('file-upload-send').click()

    await expect(page.getByTestId('sanctions-problems')).toContainText('Only the landlord')
  })

  test('offers no upload to anyone but the landlord', async ({ page }) => {
    await stub(page, { landlord: false })
    await open(page)

    await expect(page.getByTestId('sanctions-refused')).toBeVisible()
    await expect(page.getByTestId('file-upload')).toHaveCount(0)
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('file-upload-send').click()
    await expect(page.getByTestId('file-upload-problem')).toBeVisible()
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })
})
