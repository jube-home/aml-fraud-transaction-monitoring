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

import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { settleAnimations, setTheme, setLanguage } from './support'

const readyMarkers: Record<string, string> = {
  '/scaffold': 'card-pinia',
  '/scaffold/grid': 'row-count',
  '/scaffold/api-demo': 'api-success',
  '/scaffold/theme': 'theme-ready',
}

test.describe('scaffolding', () => {
  test('home page shows every stack card', async ({ page }) => {
    await page.goto('/scaffold')

    await expect(
      page.getByRole('heading', { level: 1, name: 'Jube Vue scaffolding' }),
    ).toBeVisible()
    for (const check of ['pinia', 'intl', 'query']) {
      await expect(page.getByTestId(`status-${check}`)).toHaveText('Works')
    }
    await expect(page.getByTestId('card-intl')).toContainText(/GBP\s12,500\.50/)
    await expect(page.getByTestId('card-intl')).toContainText('3 cases')
  })

  test('navigation reaches each example page and the not-found state', async ({ page }) => {
    await page.goto('/scaffold')

    await page.getByRole('link', { name: 'Data grid' }).click()
    await expect(page).toHaveURL(/\/scaffold\/grid$/)

    await page.getByRole('link', { name: 'API and cache' }).click()
    await expect(page).toHaveURL(/\/scaffold\/api-demo$/)

    await page.goto('/no/such/page')
    await expect(page.getByRole('heading', { name: 'Page not found' })).toBeVisible()
  })

  test('theme preference sets the daisyUI theme and survives a reload', async ({ page }) => {
    await page.goto('/scaffold')
    await setTheme(page, 'dark')
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'jube-dark')

    await page.reload()
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'jube-dark')

    await setTheme(page, 'light')
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'jube')
  })

  test('language switch lazy-loads Polish and updates the document language', async ({ page }) => {
    await page.goto('/scaffold')
    await setLanguage(page, 'pl')

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Szkielet Jube Vue')
    await expect(page.locator('html')).toHaveAttribute('lang', 'pl')
    await expect(page.getByTestId('card-intl')).toContainText('3 sprawy')
  })

  for (const theme of ['light', 'dark'] as const) {
    for (const path of ['/scaffold', '/scaffold/grid', '/scaffold/api-demo', '/scaffold/theme']) {
      test(`no axe violations on ${path} in ${theme}`, async ({ page }) => {
        await page.route('**/api/AntiforgeryToken', (route) =>
          route.fulfill({ json: { token: 'a'.repeat(40) } }),
        )
        await page.goto(path)
        await setTheme(page, theme)
        await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
        await expect(page.getByTestId(readyMarkers[path]!).first()).toBeVisible()

        await settleAnimations(page)

        const results = await new AxeBuilder({ page }).analyze()

        expect(results.violations).toEqual([])
      })
    }
  }
})
