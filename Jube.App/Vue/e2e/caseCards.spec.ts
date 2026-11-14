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

import {expect, type Page, type Request} from '@playwright/test'
import {test} from './fixtures'
import {me} from './hierarchyStub'

const workflow = '11111111-1111-1111-1111-111111111111'
const iso = '2026-11-10T09:00:00Z'

const caseDetail = {
  id: 9041,
  entityAnalysisModelInstanceEntryGuid: '33333333-3333-3333-3333-333333333333',
  diaryDate: null,
  caseWorkflowGuid: workflow,
  caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222222',
  createdDate: '2026-10-20T09:00:00Z',
  locked: false,
  lockedUser: null,
  lockedDate: null,
  closedStatusId: 0,
  closedDate: null,
  closedUser: null,
  caseKey: 'AccountId',
  diary: false,
  diaryUser: null,
  rating: 3,
  caseKeyValue: 'ACC-441',
  foreColor: null,
  backColor: null,
  formattedPayload: [],
  activation: [],
  enableVisualisation: false,
  entityAnalysisModelId: 1,
}

const report = (patch: Record<string, unknown> = {}) => ({
  guid: 'f0000000-0000-0000-0000-000000000001',
  caseId: 9041,
  profileName: 'Example FIU',
  profileVersion: 1,
  reportCode: 'STR',
  stateId: 1,
  currentVersion: 1,
  fiuReference: null,
  createdUser: 'alex',
  createdDate: iso,
  fieldsJson: '{"reason":"Structuring"}',
  versions: [],
  approvals: [],
  feedback: [],
  issues: [],
  ...patch,
})

function body(request: Request): Record<string, unknown> {
  return request.postDataJSON() as Record<string, unknown>
}

async function stub(page: Page, permissions: number[]) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({status: 404, json: {}}),
  )
  await page.route('**/api/Me', (route) =>
    route.fulfill({json: {...me, userName: 'alex', permissions}}),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/CaseSearch/Workflows', (route) =>
    route.fulfill({json: [{guid: workflow, name: 'AML structuring', modelName: 'Retail'}]}),
  )
  await page.route('**/api/GetCaseByIdQuery/*', (route) => route.fulfill({json: caseDetail}))
}

test.describe('Case reports card', () => {
  test.use({viewport: {width: 1440, height: 1000}})

  test('is absent for a user with no report permission', async ({page}) => {
    await stub(page, [1, 2])
    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-decide-tab-research')).toBeVisible()
    await expect(page.getByTestId('case-reports')).toHaveCount(0)
  })

  test('shows a draft to the filer, who validates it and cannot approve it', async ({page}) => {
    await stub(page, [1, 2, 75])
    await page.route('**/api/CaseReport/ByCaseId/9041', (route) =>
      route.fulfill({json: [report()]}),
    )
    await page.route('**/api/CaseReportProfile', (route) =>
      route.fulfill({json: [{name: 'Example FIU', version: 1, description: null}]}),
    )
    const validate = page.waitForRequest(
      '**/api/CaseReport/Validate/f0000000-0000-0000-0000-000000000001',
    )
    await page.route('**/api/CaseReport/Validate/*', (route) =>
      route.fulfill({json: report({stateId: 2})}),
    )
    await page.goto('/cases/9041')

    await expect(
      page.getByTestId('case-report-state-f0000000-0000-0000-0000-000000000001'),
    ).toHaveText('Draft')
    await page.getByTestId('case-report-validate-f0000000-0000-0000-0000-000000000001').click()
    expect((await validate).method()).toBe('POST')
    await expect(
      page.getByTestId('case-report-approve-f0000000-0000-0000-0000-000000000001'),
    ).toHaveCount(0)
  })

  test('invalid JSON in the fields editor disables Save', async ({page}) => {
    await stub(page, [1, 2, 75])
    await page.route('**/api/CaseReport/ByCaseId/9041', (route) =>
      route.fulfill({json: [report()]}),
    )
    await page.route('**/api/CaseReportProfile', (route) => route.fulfill({json: []}))
    await page.goto('/cases/9041')

    const fields = page.getByTestId('case-report-fields-f0000000-0000-0000-0000-000000000001')
    await fields.fill('{not json')
    await expect(
      page.getByTestId('case-report-save-f0000000-0000-0000-0000-000000000001'),
    ).toBeDisabled()
    await fields.fill('{"reason":"ok"}')
    await expect(
      page.getByTestId('case-report-save-f0000000-0000-0000-0000-000000000001'),
    ).toBeEnabled()
  })

  test('an approver approves a pending report and rejecting needs a note', async ({page}) => {
    await stub(page, [1, 2, 76])
    await page.route('**/api/CaseReport/ByCaseId/9041', (route) =>
      route.fulfill({json: [report({stateId: 3, createdUser: 'sam'})]}),
    )
    const approve = page.waitForRequest('**/api/CaseReport/Approve')
    await page.route('**/api/CaseReport/Approve', (route) =>
      route.fulfill({json: report({stateId: 4})}),
    )
    await page.goto('/cases/9041')

    await expect(
      page.getByTestId('case-report-reject-f0000000-0000-0000-0000-000000000001'),
    ).toBeDisabled()
    await page.getByTestId('case-report-approve-f0000000-0000-0000-0000-000000000001').click()
    expect(body(await approve)).toMatchObject({guid: 'f0000000-0000-0000-0000-000000000001'})
  })

  test('an approved report records the FIU reference only when one is typed', async ({page}) => {
    await stub(page, [1, 2, 75])
    await page.route('**/api/CaseReport/ByCaseId/9041', (route) =>
      route.fulfill({json: [report({stateId: 4})]}),
    )
    await page.route('**/api/CaseReportProfile', (route) => route.fulfill({json: []}))
    const submit = page.waitForRequest('**/api/CaseReport/RecordSubmission')
    await page.route('**/api/CaseReport/RecordSubmission', (route) =>
      route.fulfill({json: report({stateId: 5})}),
    )
    await page.goto('/cases/9041')

    const button = page.getByTestId('case-report-submit-f0000000-0000-0000-0000-000000000001')
    await expect(button).toBeDisabled()
    await page
      .getByTestId('case-report-reference-f0000000-0000-0000-0000-000000000001')
      .fill('FIU-2026-1')
    await button.click()
    expect(body(await submit)).toMatchObject({fiuReference: 'FIU-2026-1'})
  })
})

test.describe('Legal hold card', () => {
  test.use({viewport: {width: 1440, height: 1000}})

  test('is absent without the supervision permission', async ({page}) => {
    await stub(page, [1, 2])
    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-decide-tab-research')).toBeVisible()
    await expect(page.getByTestId('case-legal-hold')).toHaveCount(0)
  })

  test('places a hold only with a reason, then shows it and lifts it', async ({page}) => {
    await stub(page, [1, 2, 18])
    let held = false
    await page.route('**/api/CaseSupervision/LegalHold/9041', (route) =>
      route.fulfill({
        json: {hold: held, reason: held ? 'Regulator request' : null, user: 'alex', date: iso},
      }),
    )
    const sent: Record<string, unknown>[] = []
    await page.route('**/api/CaseSupervision/LegalHold', (route) => {
      sent.push(body(route.request()))
      held = (sent.at(-1) as { hold: boolean }).hold

      return route.fulfill({status: 204, body: ''})
    })
    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-legal-hold-place')).toBeDisabled()
    await page.getByTestId('case-legal-hold-reason').fill('Regulator request')
    await page.getByTestId('case-legal-hold-place').click()
    await expect(page.getByTestId('case-legal-hold-state')).toContainText('Regulator request')
    expect(sent[0]).toEqual({caseId: 9041, hold: true, reason: 'Regulator request'})

    await page.getByTestId('case-legal-hold-lift').click()
    await expect(page.getByTestId('case-legal-hold-state')).toContainText('Not under legal hold')
    expect(sent[1]).toEqual({caseId: 9041, hold: false, reason: null})
  })
})
