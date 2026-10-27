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
import { stubHierarchy } from './hierarchyStub'

async function settled(kinds: Locator): Promise<void> {
  let last = -1

  await expect
    .poll(async () => {
      const now = await kinds.locator('[role=tablist]').evaluate((e) => e.scrollLeft)
      const steady = now === last

      last = now

      return steady
    })
    .toBe(true)
}

async function start(page: Page) {
  await page.goto('/tools?tool=grid')
  await page.getByTestId('bench-area').selectOption('EntityAnalysisModel')
  await page.getByTestId('bench-load').click()
  await expect(page.getByTestId('grid-level-EntityAnalysisModel')).toBeVisible()
  await expect(page.locator('.tabulator-row').first()).toBeVisible()
}

test.describe('Hierarchy grid', () => {
  test.use({ viewport: { width: 1500, height: 1300 } })

  test('a row with several kinds under it offers them, and a row with one just expands', async ({
    page,
  }) => {
    await stubHierarchy(page)
    await start(page)

    const model = page
      .getByTestId('grid-level-EntityAnalysisModel')
      .locator('.tabulator-row')
      .first()

    await model.getByTestId('grid-expand').click()

    await expect(page.getByTestId('grid-kinds-12')).toBeVisible()

    const rowBox = await model.boundingBox()
    const detailBox = await page.getByTestId('grid-detail-12').boundingBox()

    expect(detailBox!.height).toBeGreaterThan(60)
    expect(rowBox!.height).toBeGreaterThanOrEqual(detailBox!.height)
    await expect(page.getByTestId('grid-kind-EntityAnalysisModelTag')).toHaveAttribute(
      'aria-selected',
      'true',
    )

    await page.getByTestId('grid-kind-CaseWorkflow').click()

    const workflows = page.getByTestId('grid-level-CaseWorkflow')

    await expect(workflows.locator('.tabulator-row')).toHaveCount(3)

    await workflows.locator('.tabulator-row').first().getByTestId('grid-expand').click()

    await expect(page.getByTestId('grid-kinds-5')).toHaveCount(0)
    await expect(page.getByTestId('grid-detail-5')).toContainText('XPath')
    await expect(
      page.getByTestId('grid-level-CaseWorkflowXPath').locator('.tabulator-row'),
    ).toHaveCount(1)

    const workflow = workflows.locator('.tabulator-row').first()

    await expect
      .poll(async () => {
        const inner = await page.getByTestId('grid-detail-5').boundingBox()
        const innerRow = await workflow.boundingBox()
        const outer = await page.getByTestId('grid-detail-12').boundingBox()
        const outerRow = await model.boundingBox()

        return (
          innerRow!.height >= inner!.height - 1 &&
          outer!.height >= innerRow!.height &&
          outerRow!.height >= outer!.height - 1
        )
      })
      .toBe(true)

    const clipped = await page.evaluate(() =>
      [...document.querySelectorAll<HTMLElement>('.tabulator-tableholder')]
        .filter((holder) => holder.offsetParent !== null)
        .filter((holder) => holder.scrollHeight > holder.clientHeight + 1)
        .map((holder) =>
          holder.closest('[data-testid^="grid-level-"]')?.getAttribute('data-testid'),
        ),
    )

    expect(clipped).toEqual([])
  })

  test('the kind tabs scroll on their own, with a subtle edge control only when they overflow', async ({
    page,
  }) => {
    await stubHierarchy(page)
    await start(page)

    await page
      .getByTestId('grid-level-EntityAnalysisModel')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-expand')
      .click()

    const kinds = page.getByTestId('grid-kinds-12')

    await expect(page.getByTestId('tabs-forward')).toHaveCount(0)
    await expect(page.getByTestId('tabs-back')).toHaveCount(0)

    await kinds.evaluate((element) => (element.style.width = '90px'))

    await expect(page.getByTestId('tabs-forward')).toBeVisible()
    await expect(page.getByTestId('tabs-back')).toHaveCount(0)

    await page.getByTestId('tabs-forward').click()

    await expect(page.getByTestId('tabs-back')).toBeVisible()

    await page.getByTestId('tabs-back').click()

    await expect(page.getByTestId('tabs-back')).toHaveCount(0)
    await expect(page.getByTestId('tabs-forward')).toBeVisible()

    const list = kinds.locator('[role=tablist]')

    await list.evaluate((e) => (e.scrollLeft = e.scrollWidth))

    await expect(page.getByTestId('tabs-forward')).toHaveCount(0)
    await expect(page.getByTestId('tabs-back')).toBeVisible()

    const atEnd = await list.evaluate((e) => e.scrollLeft)

    await page.getByTestId('tabs-back').click()
    await settled(kinds)

    expect(await list.evaluate((e) => e.scrollLeft)).toBeLessThan(atEnd)
  })

  test('the expander says what it will do and flips when opened and closed', async ({ page }) => {
    await stubHierarchy(page)
    await start(page)

    const expander = page
      .getByTestId('grid-level-EntityAnalysisModel')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-expand')

    await expect(expander).toHaveAttribute('aria-expanded', 'false')
    await expect(expander).toHaveAttribute('aria-label', 'Show what is under this row')

    await expander.click()

    await expect(expander).toHaveAttribute('aria-expanded', 'true')
    await expect(expander).toHaveAttribute('aria-label', 'Hide what is under this row')
    await expect(page.getByTestId('grid-detail-12')).toBeVisible()

    await expander.click()

    await expect(page.getByTestId('grid-detail-12')).toHaveCount(0)
  })

  test('a leaf kind has no expander, and Open on a nested row goes to that record in the CRUD tool', async ({
    page,
  }) => {
    await stubHierarchy(page)
    await start(page)

    await page
      .getByTestId('grid-level-EntityAnalysisModel')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-expand')
      .click()
    await page.getByTestId('grid-kind-CaseWorkflow').click()
    await page
      .getByTestId('grid-level-CaseWorkflow')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-expand')
      .click()

    const xpaths = page.getByTestId('grid-level-CaseWorkflowXPath')

    await expect(xpaths.getByTestId('grid-expand')).toHaveCount(0)

    await xpaths.locator('.tabulator-row').first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('hierarchy-trail')).toContainText('XPath: Merchant')
    expect(decodeURIComponent(page.url())).toContain(
      'path=EntityAnalysisModel:12,CaseWorkflow:5,CaseWorkflowXPath:9',
    )
  })
})
