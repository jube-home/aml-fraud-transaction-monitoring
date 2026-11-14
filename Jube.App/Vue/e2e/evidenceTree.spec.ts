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
import {stubExhaustive} from './exhaustiveStubs'
import {modelOf, stubFormModels} from './formModels'
import {settleAnimations} from './support'

const path = '/models?path=EntityAnalysisModel:1,ExhaustiveSearchInstance:1&section=trials'

const sample = {
  model: {name: 'Fraud score', version: 4, active: true},
  metrics: {auc: 0.9123456, precision: 0.8, recall: 0.75},
  variables: [
    {name: 'Amount', weight: 1.25},
    {name: 'Country', weight: 0},
  ],
  notes: '',
  reviewer: null,
  flags: [],
  extra: {},
}

async function stub(page: Page, evidence: unknown = sample) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await stubExhaustive(page)
  await stubFormModels(page, [
    modelOf({
      area: 'EntityAnalysisModel',
      keys: {id: 'id', parent: null, naturalKey: 'name'},
      children: [
        {
          area: 'ExhaustiveSearchInstance',
          label: 'Adaptations',
          hub: '',
          order: 10,
          parentKey: 'entityAnalysisModelId',
          parentField: 'id',
          listRoute: '/api/ExhaustiveSearchInstance/ByEntityAnalysisModelId',
        },
      ],
    }),
    modelOf({
      area: 'ExhaustiveSearchInstance',
      keys: {id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name'},
      fields: [{name: 'name', type: 'string'}],
    }),
  ])
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({json: [{id: 1, guid: 'g1', name: 'Payments'}]}),
  )
  await page.route('**/api/EntityAnalysisModel/1', (route) =>
    route.fulfill({json: {id: 1, guid: 'g1', name: 'Payments'}}),
  )
  await page.route('**/api/GetExhaustiveSearchInstanceTrialInstanceDetail/*', (route) =>
    route.fulfill({
      json: {
        id: 5,
        kind: 'ExhaustiveTopologyNetwork',
        score: 0.9123,
        hasDiagram: true,
        evidence,
      },
    }),
  )
}

async function open(page: Page) {
  await page.goto(path)
  await page
    .getByTestId('exhaustive-trials')
    .getByRole('button', {name: 'Show evidence'})
    .first()
    .click()
  await expect(page.getByTestId('evidence-tree')).toBeVisible()
}

test.describe('EvidenceTree', () => {
  test.use({viewport: {width: 1600, height: 1000}})

  test('shows every level expanded, with readable values', async ({page}) => {
    await stub(page)
    await open(page)

    const tree = page.getByTestId('evidence-tree')

    await expect(tree.getByText('Fraud score')).toBeVisible()
    await expect(tree.getByText('Yes', {exact: true})).toBeVisible()
    await expect(tree.getByText('0.9123')).toBeVisible()
    await expect(tree.getByText('[0]').first()).toBeVisible()
    await expect(tree.getByText('Amount')).toBeVisible()
    await expect(page.getByTestId('evidence-count')).toHaveText('14 values')
  })

  test('tells null, empty text, empty array and empty object apart', async ({page}) => {
    await stub(page)
    await open(page)

    const tree = page.getByTestId('evidence-tree')

    await expect(tree.locator('.tabulator-row', {hasText: 'reviewer'})).toContainText('No value')
    await expect(tree.locator('.tabulator-row', {hasText: 'notes'})).toContainText('(empty)')
    await expect(tree.locator('.tabulator-row', {hasText: 'flags'})).toContainText('[]')
    await expect(tree.locator('.tabulator-row', {hasText: 'extra'})).toContainText('{}')
  })

  test('collapses and expands every branch', async ({page}) => {
    await stub(page)
    await open(page)

    const tree = page.getByTestId('evidence-tree')

    await page.getByTestId('evidence-collapse').click()

    await expect(tree.getByText('Fraud score')).toHaveCount(0)
    await expect(tree.getByText('metrics')).toBeVisible()

    await page.getByTestId('evidence-expand').click()

    await expect(tree.getByText('Fraud score')).toBeVisible()
    await expect(tree.getByText('Amount')).toBeVisible()
  })

  test('says so when the evidence holds nothing to show', async ({page}) => {
    await stub(page, {})
    await open(page)

    await expect(page.getByTestId('evidence-empty')).toBeVisible()
    await expect(page.getByTestId('evidence-expand')).toHaveCount(0)
  })

  test('has no accessibility violations', async ({page}) => {
    await stub(page)
    await open(page)
    await expect(page.getByText('Fraud score')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({page})
      .include('[data-testid="trial-evidence"]')
      .analyze()

    expect(results.violations).toEqual([])
  })
})
