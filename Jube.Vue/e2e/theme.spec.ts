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

import { expect, test, type Locator, type Page } from '@playwright/test'
import { settleAnimations, setTheme, setLanguage } from './support'

function gridReady(page: Page, path: string): Locator {
  return path === '/scaffold' || path === '/'
    ? page.locator('body')
    : page.locator('.tabulator.tabulator-rtl .tabulator-header')
}

test.describe('theme palette', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`every colour pair on the theme page reaches 4.5:1 in ${theme}`, async ({ page }) => {
      await page.goto('/scaffold/theme')
      await setTheme(page, theme)
      await settleAnimations(page)

      const swatches = page.locator('[data-swatch]')
      await expect(swatches).toHaveCount(11)
      await expect(page.locator('[data-swatch]', { hasText: 'Below AA' })).toHaveCount(0)
      await expect(page.locator('[data-swatch]', { hasText: ':1 AA' })).toHaveCount(11)
    })
  }
})

test.describe('right-to-left layout', () => {
  test('Arabic sets direction and language on the document and translates the page', async ({
    page,
  }) => {
    await page.goto('/scaffold')
    await setLanguage(page, 'ar')

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('هيكل Jube Vue')
    await expect(page.getByTestId('card-intl')).toContainText('3 قضايا')
  })

  test('the navigation runs from the right edge', async ({ page }) => {
    await page.goto('/scaffold')
    await setLanguage(page, 'ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

    const first = await page.getByRole('navigation').getByRole('link').first().boundingBox()
    const last = await page.getByRole('navigation').getByRole('link').last().boundingBox()

    expect(first!.x).toBeGreaterThan(last!.x)
  })

  test('the grid mirrors: first column on the right, amounts aligned to the end', async ({
    page,
  }) => {
    await page.goto('/scaffold/grid')
    await setLanguage(page, 'ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('.tabulator-rtl')).toHaveCount(1)

    const headers = page.getByRole('columnheader')
    const first = await headers.first().boundingBox()
    const last = await headers.last().boundingBox()

    expect(first!.x).toBeGreaterThan(last!.x)
    await expect(page.getByTestId('row-count')).toHaveText('10 صفوف')
  })

  test('switching back to English restores left-to-right and the grid', async ({ page }) => {
    await page.goto('/scaffold/grid')
    await setLanguage(page, 'ar')
    await expect(page.locator('.tabulator-rtl')).toHaveCount(1)

    await setLanguage(page, 'en')

    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr')
    await expect(page.locator('.tabulator-rtl')).toHaveCount(0)
    await expect(page.locator('.tabulator-row')).toHaveCount(10)
  })

  for (const theme of ['light', 'dark'] as const) {
    for (const path of ['/', '/scaffold', '/scaffold/grid', '/scaffold/theme']) {
      test(`no axe violations on ${path} in Arabic ${theme}`, async ({ page }) => {
        const { default: AxeBuilder } = await import('@axe-core/playwright')

        await page.goto(path)
        await setTheme(page, theme)
        await setLanguage(page, 'ar')
        await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
        await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

        await expect(gridReady(page, path)).toBeVisible()

        await settleAnimations(page)

        const results = await new AxeBuilder({ page }).analyze()

        expect(results.violations).toEqual([])
      })
    }
  }

  test('long Polish and Arabic strings do not overflow the page horizontally', async ({ page }) => {
    for (const language of ['pl', 'ar']) {
      await page.goto('/scaffold/theme')
      await setLanguage(page, language as 'pl' | 'ar')
      await expect(page.locator('html')).toHaveAttribute('lang', language)

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      )

      expect(overflow).toBeLessThanOrEqual(0)
    }
  })
})

test.describe('nested form', () => {
  test('each switch reveals its own group, three levels deep, and collapses again', async ({
    page,
  }) => {
    await page.goto('/scaffold/theme')

    const cases = page.getByRole('checkbox', { name: 'Create cases' })
    const bypass = page.getByRole('checkbox', { name: 'Bypass', exact: true })
    const interval = page.getByRole('radio', { name: 'Hours' })

    await expect(cases).toHaveAttribute('aria-expanded', 'false')
    await expect(page.getByLabel('Case workflow')).toHaveCount(0)

    await cases.check()
    await expect(cases).toHaveAttribute('aria-expanded', 'true')
    await expect(page.getByLabel('Case workflow')).toBeVisible()
    await expect(interval).toHaveCount(0)

    await bypass.check()
    await expect(interval).toBeChecked()
    await expect(page.getByLabel('Bypass value')).toBeVisible()

    await cases.uncheck()
    await expect(page.getByLabel('Case workflow')).toHaveCount(0)
    await expect(interval).toHaveCount(0)
  })

  test('the nesting rule sits on the inline-start edge in both directions', async ({ page }) => {
    await page.goto('/scaffold/theme')
    await page.getByRole('checkbox', { name: 'Create cases' }).check()
    await page.getByRole('checkbox', { name: 'Bypass', exact: true }).check()

    const panelId = await page
      .getByRole('checkbox', { name: 'Bypass', exact: true })
      .getAttribute('aria-controls')
    const panel = page.locator(`[id="${panelId}"]`)

    await expect(panel).toHaveCSS('border-inline-start-width', '4px')

    await setLanguage(page, 'ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(panel).toHaveCSS('border-inline-start-width', '4px')
    await expect(panel).toHaveCSS('border-left-width', '1px')
    await expect(panel).toHaveCSS('border-right-width', '4px')
  })
})

test.describe('wordmark', () => {
  const expected = { light: 'rgb(0, 95, 127)', dark: 'rgb(255, 255, 255)' } as const

  for (const theme of ['light', 'dark'] as const) {
    test(`the wordmark is ${theme === 'light' ? 'Navy' : 'white'} in the ${theme} theme`, async ({
      page,
    }) => {
      await page.goto('/scaffold')
      await setTheme(page, theme)
      await settleAnimations(page)

      const logo = page.getByRole('img', { name: 'Jube' })

      await expect(logo).toBeVisible()
      await expect(logo).toHaveCSS('background-color', expected[theme])
    })
  }
})
