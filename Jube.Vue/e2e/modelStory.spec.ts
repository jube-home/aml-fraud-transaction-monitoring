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
import { modelOf, stubFormModels } from './formModels'
import { me, models as baseModels, stubHierarchy } from './hierarchyStub'
import { setTheme, settleAnimations } from './support'

const modelForm = modelOf({
  area: 'EntityAnalysisModel',
  keys: { id: 'id', parent: null, naturalKey: 'name' },
  fields: [{ name: 'active', type: 'boolean' }],
  lockFields: ['locked'],
  approval: { kind: 1, historyRoute: '/api/EntityAnalysisModelVersionHistory' },
  children: [
    {
      area: 'EntityAnalysisModelTag',
      label: 'Tags',
      hub: 'References',
      order: 10,
      parentKey: 'entityAnalysisModelId',
      parentField: 'id',
      listRoute: '/api/EntityAnalysisModelTag/ByEntityAnalysisModelId',
    },
    {
      area: 'CaseWorkflow',
      label: 'Cases Workflows',
      hub: '',
      order: 20,
      parentKey: 'entityAnalysisModelId',
      parentField: 'id',
      listRoute: '/api/CaseWorkflow/ByEntityAnalysisModelId',
    },
  ],
})

const tagForm = modelOf({
  area: 'EntityAnalysisModelTag',
  keys: { id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name' },
  approval: { kind: 3, historyRoute: '/api/EntityAnalysisModelTagVersionHistory' },
})

async function stub(page: Page) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await stubHierarchy(page)
  await stubFormModels(page, [
    modelForm,
    tagForm,
    ...baseModels.filter(
      (model) => !['EntityAnalysisModel', 'EntityAnalysisModelTag'].includes(model.area),
    ),
  ])
  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: [5, 6, 40, 60] } }),
  )
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({
      json: [
        {
          id: 12,
          guid: 'g12',
          name: 'Fraud model',
          version: 14,
          active: true,
          locked: false,
          updatedUser: 'alex',
          updatedDate: '2026-10-21T09:12:00Z',
        },
        {
          id: 13,
          guid: 'g13',
          name: 'AML model',
          version: 6,
          active: false,
          locked: true,
          updatedUser: 'sam',
          updatedDate: '2026-10-19T16:40:00Z',
        },
      ],
    }),
  )
  await page.route('**/api/EntityApproval/Status**', (route) => {
    const id = Number(new URL(route.request().url()).searchParams.get('entityId'))

    return route.fulfill({
      json:
        id === 12
          ? {
              kind: 1,
              entityId: 12,
              name: 'Fraud model',
              currentVersion: 14,
              effectiveVersion: 12,
              deleted: false,
              pending: true,
              rejected: false,
              makerUser: 'bob',
              approvalsRecorded: 1,
              approvalsRequired: 2,
              canApprove: false,
            }
          : {
              kind: 1,
              entityId: id,
              name: 'AML model',
              currentVersion: 6,
              effectiveVersion: 6,
              deleted: false,
              pending: false,
              rejected: false,
              makerUser: null,
              approvalsRecorded: 0,
              approvalsRequired: 0,
              canApprove: false,
            },
    })
  })
  await page.route('**/api/EntityApproval/Pending**', (route) => {
    const id = Number(new URL(route.request().url()).searchParams.get('modelId'))

    return route.fulfill({
      json:
        id === 12
          ? [
              {
                kind: 3,
                entityId: 1,
                name: 'high-risk',
                version: 2,
                deleted: false,
                makerUser: 'bob',
                parentId: 12,
              },
            ]
          : [],
    })
  })
  await page.route('**/api/EntityAnalysisModelIntegrity/12', (route) =>
    route.fulfill({
      json: {
        entityAnalysisModelId: 12,
        modelName: 'Fraud model',
        checkedDate: '2026-10-21T09:12:00Z',
        engineSource: 'x',
        errors: 0,
        warnings: 2,
        infos: 0,
        checks: [],
      },
    }),
  )
}

test.describe('Model story', () => {
  test.use({ viewport: { width: 1500, height: 1100 } })

  test.beforeEach(async ({ page }) => {
    await stub(page)
  })

  test('lists the models with their state and what awaits approval', async ({ page }) => {
    await page.goto('/tools?tool=model-story')

    const rows = page.getByRole('grid', { name: 'Models' }).locator('.tabulator-row')

    await expect(rows).toHaveCount(2)
    await expect(rows.first()).toContainText('Fraud model')
    await expect(rows.first()).toContainText('Active')
    await expect(rows.first()).toContainText('1 awaiting approval')
    await expect(rows.nth(1)).toContainText('AML model')
    await expect(rows.nth(1)).toContainText('Locked')
    await expect(page.getByTestId('models-count')).toHaveText('2 models')
  })

  test('searching narrows the table and the count', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page.getByTestId('models-search').fill('fraud')

    await expect(page.getByTestId('models-count')).toHaveText('1 model')
  })

  test('opening a model leads with its status and what is under it, with no separate hub', async ({
    page,
  }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-model')).toContainText('Fraud model')
    await expect(page.getByTestId('beat-model')).toContainText('v14')
    await expect(page.getByTestId('beat-models')).toHaveCount(0)
    await expect(page.getByTestId('hub-EntityAnalysisModelTag')).toBeVisible()
    await expect(page.getByTestId('hub-awaiting-EntityAnalysisModelTag')).toHaveText('1 awaiting')
    await expect(page.getByTestId('hub-CaseWorkflow')).toBeVisible()
    await expect(page.getByTestId('model-awaiting')).toHaveText('1 awaiting approval')
    await expect(page.getByTestId('hub-pill-integrity')).toHaveText('Sound, with warnings')
    await expect(page.getByTestId('hub-open-sample')).toBeVisible()
    await expect(page).toHaveURL(/path=EntityAnalysisModel:12/)
  })

  test('drills through an area to a record and back up through the receipts', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()

    await expect(page.getByTestId('receipt-model')).toContainText('Fraud model')
    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveAttribute('data-state', 'working')
    await page
      .getByTestId('beat-CaseWorkflow:12')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflowXPath').click()

    await expect(page.getByTestId('receipt-CaseWorkflow:12#record')).toContainText('Card fraud')
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)

    await page.getByTestId('receipt-model').click()

    await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toHaveCount(0)
    await expect(page).toHaveURL(/path=EntityAnalysisModel:12(&|$)/)
  })

  test('opening a record from a list replaces the list with the record, one line for the level', async ({
    page,
  }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
    await page
      .getByTestId('beat-CaseWorkflow:12')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveCount(0)
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toContainText('Cases Workflows')
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toContainText('Card fraud')
    await expect(page.getByTestId('hub-open-CaseWorkflowXPath')).toBeVisible()
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)
    await expect(page.getByTestId('crud-list')).toHaveCount(0)
  })

  test('the back button returns to the list and drops the record', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
    await page
      .getByTestId('beat-CaseWorkflow:12')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('record-back').click()

    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toHaveCount(0)
    await expect(page.getByTestId('crud-list')).toBeVisible()
    await expect(page).toHaveURL(/path=EntityAnalysisModel:12(&|$)/)
  })

  test('properties are closed on a record with something under it, and open on a leaf', async ({
    page,
  }) => {
    await page.goto('/tools?tool=model-story&path=EntityAnalysisModel:12,CaseWorkflow:5')

    const workflow = page
      .getByTestId('beat-CaseWorkflow:12#record')
      .getByTestId('record-properties')

    await expect(workflow).toBeVisible()
    await expect(workflow).not.toHaveAttribute('open', '')
    await expect(page.getByTestId('crud-form')).toHaveCount(0)

    await workflow.locator('summary').click()

    await expect(page.getByTestId('crud-form')).toBeVisible()

    await page.goto(
      '/tools?tool=model-story&path=EntityAnalysisModel:12,CaseWorkflow:5,CaseWorkflowXPath:9',
    )

    const leaf = page
      .getByTestId('beat-CaseWorkflowXPath:5#record')
      .getByTestId('record-properties')

    await expect(leaf).toHaveAttribute('open', '')
    await expect(page.getByTestId('crud-form')).toBeVisible()
  })

  test('the model keeps its properties behind a closed section, beside the cards', async ({
    page,
  }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()

    const properties = page.getByTestId('beat-model').getByTestId('record-properties')

    await expect(properties).toBeVisible()
    await expect(properties).not.toHaveAttribute('open', '')
    await expect(page.getByTestId('hub-CaseWorkflow')).toBeVisible()
    await expect(page.getByTestId('model-edit')).toHaveCount(0)
  })

  async function openWorkflowList(page: Page) {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
  }

  test('New folds the list away and opens a working beat for the new record', async ({ page }) => {
    await openWorkflowList(page)
    await page.getByTestId('crud-new').click()

    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveCount(0)
    await expect(page.getByTestId('beat-CaseWorkflow:12#new')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('beat-CaseWorkflow:12#new')).toContainText('Cases Workflows')
    await expect(page.getByTestId('beat-CaseWorkflow:12#new')).toContainText('New')
    await expect(page.getByTestId('new-back')).toBeVisible()
    await expect(page.getByTestId('crud-form')).toBeVisible()
    await expect(page.getByTestId('crud-list')).toHaveCount(0)
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)
  })

  test('Back from a new record returns to the list', async ({ page }) => {
    await openWorkflowList(page)
    await page.getByTestId('crud-new').click()
    await page.getByTestId('new-back').click()

    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-CaseWorkflow:12#new')).toHaveCount(0)
    await expect(page.getByTestId('crud-list')).toBeVisible()
  })

  test('Add on a hub card opens the same new-record beat', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-more-CaseWorkflow').click()
    await page.getByTestId('hub-add-CaseWorkflow').click()

    await expect(page.getByTestId('beat-CaseWorkflow:12#new')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('crud-form')).toBeVisible()
  })

  test('saving a new record turns it into the record beat', async ({ page }) => {
    await page.route('**/api/CaseWorkflow', (route) =>
      route.request().method() === 'POST'
        ? route.fulfill({
            json: { id: 9, guid: 'w9', name: 'Fresh', entityAnalysisModelId: 12, active: true },
          })
        : route.fallback(),
    )
    await page.route('**/api/CaseWorkflow/9', (route) =>
      route.fulfill({
        json: { id: 9, guid: 'w9', name: 'Fresh', entityAnalysisModelId: 12, active: true },
      }),
    )
    await openWorkflowList(page)
    await page.getByTestId('crud-new').click()
    await page
      .getByRole('checkbox', { name: /active/i })
      .first()
      .check()
    await page.getByTestId('crud-save').click()

    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toContainText('Fresh')
    await expect(page.getByTestId('beat-CaseWorkflow:12#new')).toHaveCount(0)
  })

  test('a new model also gets Back and cancelling returns to the table', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page.getByTestId('models-new').click()

    await expect(page.getByTestId('beat-create')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('new-back')).toBeVisible()

    await page.getByTestId('new-back').click()

    await expect(page.getByTestId('beat-models')).toHaveAttribute('data-state', 'working')
  })

  test('back to Models returns to the table', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'AML model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('record-back').click()

    await expect(page.getByTestId('beat-models')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-model')).toHaveCount(0)
    await expect(page).not.toHaveURL(/path=/)
  })

  test('a deep link rebuilds the model, the area and the record', async ({ page }) => {
    await page.goto(
      '/tools?tool=model-story&path=EntityAnalysisModel:12,CaseWorkflow:5,CaseWorkflowXPath:9',
    )

    await expect(page.getByTestId('receipt-model')).toContainText('Fraud model')
    await expect(page.getByTestId('receipt-CaseWorkflow:12#record')).toContainText('Card fraud')
    await expect(page.locator('[data-state="working"]')).toHaveCount(1)
    await expect(page.getByTestId('beat-CaseWorkflowXPath:5#record')).toHaveAttribute(
      'data-state',
      'working',
    )
  })

  test('integrity and sample are beats of their own with the model fixed', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-integrity').click()

    await expect(page.getByTestId('beat-operate-integrity')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('receipt-model')).toContainText('Fraud model')
    await expect(page).toHaveURL(/~integrity/)

    await page.getByTestId('receipt-model').click()
    await page.getByTestId('hub-open-sample').click()

    await expect(page.getByTestId('beat-operate-sample')).toHaveAttribute('data-state', 'working')
  })

  test('advanced analytics lives under the model and lists only its instances', async ({
    page,
  }) => {
    await page.route('**/api/AskJoobyAnalyticsInstance**', (route) =>
      route.fulfill({
        json: [
          {
            id: 1,
            guid: 'a',
            name: 'Fraud scorer',
            entityAnalysisModelId: 12,
            active: true,
            version: 1,
          },
          {
            id: 2,
            guid: 'b',
            name: 'AML scorer',
            entityAnalysisModelId: 13,
            active: true,
            version: 1,
          },
          {
            id: 3,
            guid: 'c',
            name: 'Loose scorer',
            entityAnalysisModelId: null,
            active: false,
            version: 1,
          },
        ],
      }),
    )
    await page.route('**/api/EntityApproval/ValueStatus', (route) => route.fulfill({ json: [] }))
    await page.route('**/api/Me', (route) =>
      route.fulfill({ json: { ...me, permissions: [6, 45, 46, 47, 48] } }),
    )
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-analytics').click()

    const rows = page.getByRole('grid').locator('.tabulator-row')

    await expect(page.getByTestId('beat-operate-analytics')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(rows).toHaveCount(2)
    await expect(rows.first()).toContainText('Fraud scorer')
    await expect(page.getByText('AML scorer')).toHaveCount(0)
    await expect(page).toHaveURL(/~analytics/)
  })

  test('hides the operate cards from someone without the permissions', async ({ page }) => {
    await page.route('**/api/Me', (route) => route.fulfill({ json: { ...me, permissions: [] } }))
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('hub-CaseWorkflow')).toBeVisible()
    await expect(page.getByTestId('hub-integrity')).toHaveCount(0)
    await expect(page.getByTestId('hub-sample')).toHaveCount(0)
    await expect(page.getByTestId('hub-analytics')).toHaveCount(0)
  })

  test('starts a new model as its own beat', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page.getByTestId('models-new').click()

    await expect(page.getByTestId('beat-create')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('receipt-models')).toContainText('New model')
  })

  test("shows the model's own approval in the table, the receipt and the hub", async ({ page }) => {
    await page.goto('/tools?tool=model-story')

    const rows = page.getByRole('grid', { name: 'Models' }).locator('.tabulator-row')

    await expect(rows.first()).toContainText('Awaiting approval')
    await expect(rows.nth(1)).toContainText('Approved')

    await rows.first().getByTestId('grid-action-open').click()

    await expect(page.getByTestId('beat-model')).toContainText('Awaiting approval')
    await expect(page.getByTestId('model-approval')).toHaveText('Awaiting approval')
    await expect(page.getByTestId('model-live')).toHaveText('Live version 12')
    await expect(page.getByTestId('model-current')).toHaveText('Current version 14')
    await expect(page.getByTestId('model-maker')).toHaveText('Made by bob')
    await expect(page.getByTestId('model-approvals')).toHaveText('1 of 2 approvals')
  })

  test('shows nothing about versions that agree for an approved model', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'AML model' })
      .getByTestId('grid-action-open')
      .click()

    await expect(page.getByTestId('model-approval')).toHaveText('Approved')
    await expect(page.getByTestId('model-current')).toHaveCount(0)
    await expect(page.getByTestId('model-maker')).toHaveCount(0)
    await expect(page.getByTestId('model-approvals')).toHaveCount(0)
  })

  async function openWorkflowForm(page: Page) {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
    await page
      .getByTestId('beat-CaseWorkflow:12')
      .locator('.tabulator-row')
      .first()
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('record-properties').locator('summary').click()
    await page
      .getByRole('checkbox', { name: /active/i })
      .first()
      .check()
    await expect(page.getByTestId('crud-dirty')).toBeVisible()
  }

  test('asks before a receipt discards unsaved edits, and keeps them on request', async ({
    page,
  }) => {
    await openWorkflowForm(page)

    await expect(page.getByTestId('unsaved-CaseWorkflow:12#record')).toHaveText('Unsaved')

    await page.getByTestId('receipt-model').click()

    await expect(page.getByTestId('story-discard-dialog')).toBeVisible()
    await expect(page.getByTestId('story-discard-dialog')).toContainText('Card fraud')

    await page.getByTestId('story-discard-cancel').click()

    await expect(page.getByTestId('story-discard-dialog')).toBeHidden()
    await expect(page.getByTestId('beat-CaseWorkflow:12#record')).toHaveAttribute(
      'data-state',
      'working',
    )
    await expect(page.getByTestId('crud-dirty')).toBeVisible()
  })

  test('discarding unsaved edits goes back and drops the beats below', async ({ page }) => {
    await openWorkflowForm(page)
    await page.getByTestId('receipt-model').click()
    await page.getByTestId('story-discard-confirm').click()

    await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-CaseWorkflow:12')).toHaveCount(0)
  })

  test('asks before descending from a record with unsaved edits', async ({ page }) => {
    await openWorkflowForm(page)
    await page.getByTestId('hub-open-CaseWorkflowXPath').click()

    await expect(page.getByTestId('story-discard-dialog')).toBeVisible()

    await page.getByTestId('story-discard-confirm').click()

    await expect(page.getByTestId('beat-CaseWorkflowXPath:5')).toHaveAttribute(
      'data-state',
      'working',
    )
  })

  test('does not ask when nothing has changed', async ({ page }) => {
    await page.goto('/tools?tool=model-story')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-CaseWorkflow').click()
    await page.getByTestId('receipt-model').click()

    await expect(page.getByTestId('story-discard-dialog')).toBeHidden()
    await expect(page.getByTestId('beat-model')).toHaveAttribute('data-state', 'working')
  })

  test('offers a whole-tenant synchronisation on the table, and the hub carries the model one', async ({
    page,
  }) => {
    const posts: Record<string, unknown>[] = []

    await page.route('**/api/EntityAnalysisModelSynchronisationSchedule**', (route) => {
      if (route.request().method() === 'POST') {
        posts.push(route.request().postDataJSON())

        return route.fulfill({ json: { id: 1 } })
      }

      return route.fulfill({ json: { scheduleDate: '2026-10-21T12:00:00Z' } })
    })
    await page.route('**/api/GetEntityAnalysisModelSynchronisationNodeStatusEntries', (route) =>
      route.fulfill({ json: [] }),
    )
    await page.goto('/tools?tool=model-story')
    await page.getByTestId('models-sync-all').click()

    await expect(page.getByTestId('model-synchronisation')).toContainText('Synchronise all models')

    await page.getByTestId('sync-now').click()
    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toEqual({})
  })

  test('hides the whole-tenant synchronisation from someone without the permission', async ({
    page,
  }) => {
    await page.route('**/api/Me', (route) =>
      route.fulfill({ json: { ...me, permissions: [6, 40, 60] } }),
    )
    await page.goto('/tools?tool=model-story')

    await expect(page.getByTestId('models-new')).toBeVisible()
    await expect(page.getByTestId('models-sync-all')).toHaveCount(0)
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme`, async ({ page }) => {
      await page.goto('/tools?tool=model-story')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      const table = await new AxeBuilder({ page }).include('[data-testid="story"]').analyze()

      expect(
        table.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90))}`),
      ).toEqual([])

      await page
        .locator('.tabulator-row', { hasText: 'Fraud model' })
        .getByTestId('grid-action-open')
        .click()
      await settleAnimations(page)

      const hub = await new AxeBuilder({ page }).include('[data-testid="story"]').analyze()

      expect(
        hub.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90))}`),
      ).toEqual([])
    })
  }
})

test.describe('Models route', () => {
  test.use({ viewport: { width: 1500, height: 1100 } })

  test.beforeEach(async ({ page }) => {
    await stub(page)
  })

  test('is the Models entry in the menu, with no tree and no Lists, Dictionary or Integrity', async ({
    page,
  }) => {
    await page.goto('/models')

    const menu = page.getByRole('navigation', { name: 'Main navigation' })

    await expect(menu.getByRole('link', { name: 'Models' })).toBeVisible()
    await expect(menu.getByRole('link', { name: 'Lists' })).toHaveCount(0)
    await expect(menu.getByRole('link', { name: 'Dictionary' })).toHaveCount(0)
    await expect(menu.getByRole('link', { name: 'Integrity' })).toHaveCount(0)
    await expect(page.getByTestId('beat-models')).toHaveAttribute('data-state', 'working')
    await expect(page).toHaveTitle(/Models/)
  })

  test('starts the journey at the table and keeps the story in the address', async ({ page }) => {
    await page.goto('/models')
    await page
      .locator('.tabulator-row', { hasText: 'Fraud model' })
      .getByTestId('grid-action-open')
      .click()
    await page.getByTestId('hub-open-integrity').click()

    await expect(page).toHaveURL(/\/models\?path=EntityAnalysisModel:12,~integrity/)
  })

  test('a deep link opens the story and the menu entry returns to the table', async ({ page }) => {
    await page.goto('/models?path=EntityAnalysisModel:12,~integrity')

    await expect(page.getByTestId('beat-operate-integrity')).toHaveAttribute(
      'data-state',
      'working',
    )

    await page
      .getByRole('navigation', { name: 'Main navigation' })
      .getByRole('link', { name: 'Models' })
      .click()

    await expect(page.getByTestId('beat-models')).toHaveAttribute('data-state', 'working')
    await expect(page.getByTestId('beat-operate-integrity')).toHaveCount(0)
    await expect(page).not.toHaveURL(/path=/)
  })
})
