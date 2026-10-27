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

type Reply = { status: number; body: string; json?: object }

async function stub(page: Page, options: { permissions?: number[]; reply?: Reply } = {}) {
  const posts: Record<string, unknown>[] = []
  const reply = options.reply ?? { status: 200, body: 'a,b\n1,2\n' }

  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [40] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityAnalysisModelIntegrity/Models', (route) =>
    route.fulfill({
      json: [
        { id: 1, name: 'Payments', active: true },
        { id: 2, name: 'Cards', active: true },
      ],
    }),
  )
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({
      json: [
        { id: 1, guid: 'guid-payments', name: 'Payments' },
        { id: 2, guid: 'guid-cards', name: 'Cards' },
      ],
    }),
  )
  await page.route('**/api/GetEntityAnalysisModelSample', (route) => {
    posts.push(route.request().postDataJSON())

    return reply.json
      ? route.fulfill({ status: reply.status, json: reply.json })
      : route.fulfill({
          status: reply.status,
          contentType: 'text/csv',
          headers: { 'Content-Disposition': 'attachment; filename="payments-sample.csv"' },
          body: reply.body,
        })
  })

  return posts
}

async function open(page: Page) {
  await page.goto('/tools?tool=model-sample')
  await expect(page.getByTestId('model-sample')).toBeVisible()
  await expect(page.getByTestId('model-picker')).toHaveValue('1')
}

test.describe('Model sample', () => {
  test.use({ viewport: { width: 1300, height: 1000 } })

  test('downloads the sample under the name the server gives it', async ({ page }) => {
    const posts = await stub(page)
    await open(page)

    await page.getByTestId('sample-rate').fill('0.25')
    await expect(page.getByTestId('sample-percent')).toHaveText('25%')

    const download = page.waitForEvent('download')

    await page.getByTestId('download-button').click()

    expect((await download).suggestedFilename()).toBe('payments-sample.csv')
    await expect(page.getByTestId('sample-saved')).toContainText('payments-sample.csv')
    expect(posts).toHaveLength(1)
    expect(posts[0]).toMatchObject({ entityAnalysisModelGuid: 'guid-payments', sample: 0.25 })
    expect(new Date(posts[0]!.dateTo as string).getTime()).toBeGreaterThan(
      new Date(posts[0]!.dateFrom as string).getTime(),
    )
  })

  test('names what is missing without calling the server', async ({ page }) => {
    const posts = await stub(page)
    await open(page)

    await page.getByTestId('sample-to').fill('')
    await page.getByTestId('download-button').click()
    await expect(page.getByTestId('sample-problems')).toContainText('Enter the end of the window.')

    await page.getByTestId('sample-to').fill('2020-01-01T00:00')
    await page.getByTestId('download-button').click()

    await expect(page.getByTestId('sample-problems')).toContainText('must be after its start')
    expect(posts).toHaveLength(0)
  })

  test('shows the reasons the server refuses a sample with', async ({ page }) => {
    await stub(page, {
      reply: { status: 400, body: '', json: { reasons: ['The window holds too many rows.'] } },
    })
    await open(page)

    await page.getByTestId('download-button').click()

    await expect(page.getByTestId('sample-problems')).toContainText(
      'The window holds too many rows.',
    )
    await expect(page.getByTestId('download-button')).toBeEnabled()
  })

  test('says when the user may not download', async ({ page }) => {
    await stub(page, { reply: { status: 403, body: '' } })
    await open(page)

    await page.getByTestId('download-button').click()

    await expect(page.getByTestId('sample-problems')).toContainText('do not have permission')
  })

  test('reports an empty file instead of saving it', async ({ page }) => {
    await stub(page, { reply: { status: 200, body: '' } })
    await open(page)

    await page.getByTestId('download-button').click()

    await expect(page.getByTestId('sample-problems')).toContainText('empty file')
  })

  test('disables the download without the permission', async ({ page }) => {
    await stub(page, { permissions: [] })
    await open(page)

    await expect(page.getByTestId('download-button')).toBeDisabled()
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await open(page)
    await page.getByTestId('sample-to').fill('2020-01-01T00:00')
    await page.getByTestId('download-button').click()
    await expect(page.getByTestId('sample-problems')).toBeVisible()
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })
})
