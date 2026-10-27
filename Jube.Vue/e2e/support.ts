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

import type { Page } from '@playwright/test'

const languageNames = { en: 'English', pl: 'Polski', ar: 'العربية' } as const
const themeNames = { system: 'System', light: 'Light', dark: 'Dark' } as const

export async function setTheme(page: Page, theme: keyof typeof themeNames): Promise<void> {
  await page
    .getByRole('group', { name: 'Theme' })
    .first()
    .getByRole('button', { name: themeNames[theme] })
    .click()
}

export async function setLanguage(page: Page, language: keyof typeof languageNames): Promise<void> {
  await page.getByTestId('locale-trigger').first().click()
  await page.getByTestId(`locale-${language}`).first().click()
}

export async function settleAnimations(page: Page): Promise<void> {
  await page.evaluate(() =>
    Promise.all(
      document
        .getAnimations()
        .filter((animation) => animation.effect?.getComputedTiming().iterations !== Infinity)
        .map((animation) => animation.finished.catch(() => null)),
    ),
  )
}

export async function stubRuleBuilder(page: Page): Promise<void> {
  await page.route('**/api/RuleVocabulary', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/Completions/ByEntityAnalysisModelIdParseTypeId**', (route) =>
    route.fulfill({ json: [] }),
  )
  await page.route('**/api/Parser', (route) => route.fulfill({ json: { message: 'Compiled' } }))
}
