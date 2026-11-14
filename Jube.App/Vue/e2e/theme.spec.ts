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

import {expect} from '@playwright/test'
import {test} from './fixtures'
import {settleAnimations, setTheme, setLanguage} from './support'

test.describe('theme preference', () => {
  test('sets the daisyUI theme and survives a reload', async ({page}) => {
    await page.goto('/')
    await setTheme(page, 'dark')
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'jube-dark')

    await page.reload()
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'jube-dark')
  })
})

test.describe('right-to-left layout', () => {
  test('Arabic sets direction and language on the document', async ({page}) => {
    await page.goto('/')
    await setLanguage(page, 'ar')

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
  })

  test('switching back to English restores left-to-right', async ({page}) => {
    await page.goto('/')
    await setLanguage(page, 'ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

    await setLanguage(page, 'en')

    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr')
    await expect(page.locator('html')).toHaveAttribute('lang', 'en-GB')
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`no axe violations on the sign-in page in Arabic ${theme}`, async ({page}) => {
      const {default: AxeBuilder} = await import('@axe-core/playwright')

      await page.goto('/')
      await setTheme(page, theme)
      await setLanguage(page, 'ar')
      await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
      await expect(page.getByRole('heading', {level: 1})).toBeVisible()

      await settleAnimations(page)

      const results = await new AxeBuilder({page}).analyze()

      expect(results.violations).toEqual([])
    })
  }

  test('long Polish and Arabic strings do not overflow the page horizontally', async ({page}) => {
    for (const language of ['pl', 'ar']) {
      await page.goto('/')
      await setLanguage(page, language as 'pl' | 'ar')
      await expect(page.locator('html')).toHaveAttribute('lang', language)

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      )

      expect(overflow).toBeLessThanOrEqual(0)
    }
  })
})

test.describe('wordmark', () => {
  const expected = {light: 'rgb(0, 95, 127)', dark: 'rgb(255, 255, 255)'} as const

  for (const theme of ['light', 'dark'] as const) {
    test(`the wordmark is ${theme === 'light' ? 'Navy' : 'white'} in the ${theme} theme`, async ({
                                                                                                   page,
                                                                                                 }) => {
      await page.goto('/')
      await setTheme(page, theme)
      await settleAnimations(page)

      const logo = page.getByRole('img', {name: 'Jube'})

      await expect(logo).toBeVisible()
      await expect(logo).toHaveCSS('background-color', expected[theme])
    })
  }
})
