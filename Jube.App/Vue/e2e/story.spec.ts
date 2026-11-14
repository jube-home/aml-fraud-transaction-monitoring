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
import {expect, type Page} from '@playwright/test'
import {test} from './fixtures'
import {stubHierarchy} from './hierarchyStub'
import {setLanguage, settleAnimations} from './support'

async function stub(page: Page) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await stubHierarchy(page)
}

async function openCaseWorkflows(page: Page) {
  await page.goto('/models')
  await page
    .locator('.tabulator-row', {hasText: 'Fraud model'})
    .getByTestId('grid-action-open')
    .click()
  await page.getByTestId('hub-open-CaseWorkflow').click()
}

test.describe('StorySpine', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test.beforeEach(async ({page}) => {
    await stub(page)
  })

  test('starts with one working beat', async ({page}) => {
    await page.goto('/models')

    await expect(page.getByTestId('beat-models')).toHaveAttribute('data-state', 'working')
  })

  test('choosing turns the beat into a receipt and opens the next as the only working beat', async ({
                                                                                                      page,
                                                                                                    }) => {
    await openCaseWorkflows(page)

    await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'receipt')
    await expect(page.getByTestId('receipt-model')).toContainText('Fraud model')
    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveAttribute('data-state', 'working')
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)
    await expect(page.getByTestId('story-announce')).toContainText('Showing')
    await expect(page).toHaveURL(/path=EntityAnalysisModel:12/)
  })

  test('a receipt climbs back to its beat', async ({page}) => {
    await openCaseWorkflows(page)

    await page.getByTestId('receipt-model').click()

    await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveCount(0)
  })

  test('has no accessibility violations in Arabic and mirrors', async ({page}) => {
    await page.goto('/models')
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await settleAnimations(page)

    const results = await new AxeBuilder({page}).include('[data-testid="story"]').analyze()

    expect(results.violations.map((v) => v.id)).toEqual([])
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  })
})
