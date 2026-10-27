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

test.describe('StorySpine', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test.beforeEach(async ({ page }) => {
    await stub(page)
  })

  test('starts with one working beat and no trail', async ({ page }) => {
    await page.goto('/tools?tool=story')

    await expect(page.getByTestId('beat-level-0')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('story-trail')).toHaveCount(0)
  })

  test('choosing turns the beat into a receipt and opens the next as the only working beat', async ({
    page,
  }) => {
    await page.goto('/tools?tool=story')
    await page.getByTestId('pick-card').click()

    await expect(page.getByTestId('beat-level-0')).toHaveAttribute('data-state', 'receipt')
    await expect(page.getByTestId('receipt-level-0')).toContainText('Card fraud model')
    await expect(page.getByTestId('receipt-level-0')).toContainText('Approved')
    await expect(page.getByTestId('beat-level-1')).toHaveAttribute('data-state', 'working')
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)
    await expect(page.getByTestId('story-announce')).toContainText('Showing Areas')
    await expect(page.getByRole('heading', { level: 2, name: 'Areas' })).toBeFocused()
    await expect(page).toHaveURL(/story=card/)
  })

  test('reopening a receipt drops everything below it', async ({ page }) => {
    await page.goto('/tools?tool=story')
    await page.getByTestId('pick-card').click()
    await page.getByTestId('pick-rules').click()
    await page.getByTestId('pick-velocity').click()

    await expect(page.getByTestId('beat-level-3')).toHaveAttribute('data-state', 'working')

    await page.getByTestId('receipt-level-1').click()

    await expect(page.getByTestId('beat-level-1')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-level-2')).toHaveCount(0)
    await expect(page.getByTestId('beat-level-3')).toHaveCount(0)
    await expect(page.getByTestId('receipt-level-0')).toBeVisible()
    await expect(page).toHaveURL(/story=card(&|$)/)
  })

  test('the trail climbs the same way as a receipt', async ({ page }) => {
    await page.goto('/tools?tool=story')
    await page.getByTestId('pick-aml').click()
    await page.getByTestId('pick-tags').click()

    await page.getByTestId('crumb-level-0').click()

    await expect(page.getByTestId('beat-level-0')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-level-1')).toHaveCount(0)
  })

  test('a deep link rebuilds the whole story', async ({ page }) => {
    await page.goto('/tools?tool=story&story=aml,lists,geo')

    await expect(page.getByTestId('receipt-level-0')).toContainText('AML transaction model')
    await expect(page.getByTestId('receipt-level-1')).toContainText('Lists')
    await expect(page.getByTestId('receipt-level-2')).toContainText('Impossible travel')
    await expect(page.getByTestId('beat-level-3')).toHaveAttribute('data-state', 'working')
  })

  test('shows a changed marker on a receipt and unsaved edits on the working beat', async ({
    page,
  }) => {
    await page.goto('/tools?tool=story&story=card,rules,velocity')

    await page.getByTestId('bench-changed').check()
    await page.getByTestId('bench-unsaved').check()

    await expect(page.getByTestId('receipt-level-0')).toContainText('Changed')
    await expect(page.getByTestId('unsaved-level-3')).toHaveText('Unsaved')
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme`, async ({ page }) => {
      await page.goto('/tools?tool=story&story=card,rules,velocity')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      const results = await new AxeBuilder({ page }).include('[data-testid="story"]').analyze()

      expect(
        results.violations.map(
          (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
        ),
      ).toEqual([])
    })
  }

  test('has no accessibility violations in Arabic and mirrors', async ({ page }) => {
    await page.goto('/tools?tool=story&story=card,rules,velocity')
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).include('[data-testid="story"]').analyze()

    expect(results.violations.map((v) => v.id)).toEqual([])
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  })
})
