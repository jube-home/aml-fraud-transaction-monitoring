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
import { expect, test, type Page } from '@playwright/test'
import { stubHierarchy as stub } from './hierarchyStub'

async function start(page: Page) {
  await page.goto('/tools?tool=crud')
  await page.getByTestId('bench-area').selectOption('EntityAnalysisModel')
  await page.getByTestId('bench-parent').fill('')
  await page.getByTestId('bench-load').click()
  await expect(page.getByTestId('level-0')).toBeVisible()
}

test.describe('Hierarchy', () => {
  test.use({ viewport: { width: 1500, height: 1200 } })

  test('opening a child hides its parent and leaves the way back in the trail', async ({
    page,
  }) => {
    await stub(page)
    await start(page)

    await page
      .getByTestId('level-body-0')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()

    await expect(page.getByTestId('level-1')).toBeVisible()
    await expect(page.getByTestId('level-0')).toBeHidden()
    await expect(page.getByTestId('crumb-1')).toHaveAttribute('aria-current', 'page')
    await expect(page.getByTestId('crumb-0')).toContainText('Fraud model')
    await expect(page.getByTestId('hierarchy-announce')).toContainText('Showing Cases Workflows')
  })

  test('climbing back through the trail shows that level and clears everything below it', async ({
    page,
  }) => {
    await stub(page)
    await start(page)

    await page
      .getByTestId('level-body-0')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
    await page
      .getByTestId('level-body-1')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflowXPath').click()

    await expect(page.getByTestId('level-2')).toBeVisible()
    await expect(page.getByTestId('hierarchy-trail').locator('li')).toHaveCount(3)

    await page.getByTestId('crumb-1').click()

    await expect(page.getByTestId('level-1')).toBeVisible()
    await expect(page.getByTestId('level-2')).toHaveCount(0)
    await expect(page.getByTestId('hierarchy-trail').locator('li')).toHaveCount(2)
    await expect(page.getByTestId('crumb-1')).toHaveAttribute('aria-current', 'page')
    await expect(page.getByTestId('hubs-1')).toBeVisible()
    await expect(page.getByTestId('level-body-1').locator('.tabulator-row')).toHaveCount(3)

    await page.getByTestId('crumb-0').click()

    await expect(page.getByTestId('level-0')).toBeVisible()
    await expect(page.getByTestId('level-body-0').locator('.tabulator-row')).toHaveCount(2)
    await expect(page.getByTestId('level-1')).toHaveCount(0)
    await expect(page.getByTestId('hierarchy-announce')).toContainText(
      'Showing Entity analysis model: Fraud model',
    )
  })

  test('each kind shows how many children there are and how many are active or locked', async ({
    page,
  }) => {
    await stub(page)
    await start(page)

    await page
      .getByTestId('level-body-0')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('hub-total-CaseWorkflow')).toHaveText('3')
    await expect(page.getByTestId('hub-CaseWorkflow')).toContainText('2 active')
    await expect(page.getByTestId('hub-CaseWorkflow')).toContainText('1 locked')
    await expect(page.getByTestId('hub-total-EntityAnalysisModelTag')).toHaveText('0')
    await expect(page.getByTestId('hub-EntityAnalysisModelTag')).toContainText('None yet')
  })

  test('the actions menu on a kind adds a new one straight away', async ({ page }) => {
    await stub(page)
    await start(page)

    await page
      .getByTestId('level-body-0')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-more-CaseWorkflow').click()
    await page.getByTestId('hub-add-CaseWorkflow').click()

    await expect(page.getByTestId('level-1')).toBeVisible()
    await expect(page.getByTestId('level-body-1').getByTestId('crud-form')).toBeVisible()
  })

  test('opening another record in a level drops the levels that belonged to the first', async ({
    page,
  }) => {
    await stub(page)
    await start(page)

    await page
      .getByTestId('level-body-0')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
    await expect(page.getByTestId('level-1')).toBeVisible()

    await page.getByTestId('crumb-0').click()
    await page
      .getByTestId('level-body-0')
      .locator('.tabulator-row')
      .nth(1)
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('level-1')).toHaveCount(0)
    await expect(page.getByTestId('hierarchy-trail')).toContainText('AML model')
  })

  test('keeps the open path in the address and rebuilds every level from a deep link', async ({
    page,
  }) => {
    await stub(page)
    await page.goto(
      '/tools?tool=crud&area=EntityAnalysisModel&path=EntityAnalysisModel:12,CaseWorkflow:5,CaseWorkflowXPath:9',
    )

    await expect(page.getByTestId('hierarchy-trail')).toContainText('XPath: Merchant')
    await expect(page.getByTestId('level-2')).toBeVisible()
    await expect(page.getByTestId('level-0')).toBeHidden()
    await expect(page.getByTestId('hierarchy-trail').locator('li')).toHaveCount(3)
    expect(decodeURIComponent(page.url())).toContain(
      'path=EntityAnalysisModel:12,CaseWorkflow:5,CaseWorkflowXPath:9',
    )
  })
})
