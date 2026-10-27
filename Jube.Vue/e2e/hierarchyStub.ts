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
import { modelOf, stubFormModels } from './formModels'

export const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const child = (
  area: string,
  label: string,
  hub: string,
  order: number,
  parentKey: string,
  listRoute: string,
) => ({ area, label, hub, order, parentKey, parentField: 'id' as const, listRoute })

export const models = [
  modelOf({
    area: 'EntityAnalysisModel',
    keys: { id: 'id', parent: null, naturalKey: 'name' },
    children: [
      child(
        'EntityAnalysisModelTag',
        'Tags',
        'References',
        10,
        'entityAnalysisModelId',
        '/api/EntityAnalysisModelTag/ByEntityAnalysisModelId',
      ),
      child(
        'CaseWorkflow',
        'Cases Workflows',
        '',
        20,
        'entityAnalysisModelId',
        '/api/CaseWorkflow/ByEntityAnalysisModelId',
      ),
    ],
  }),
  modelOf({
    area: 'CaseWorkflow',
    keys: { id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name' },
    fields: [{ name: 'active', type: 'boolean' }],
    lockFields: ['locked'],
    children: [
      child(
        'CaseWorkflowXPath',
        'XPath',
        '',
        10,
        'caseWorkflowId',
        '/api/CaseWorkflowXPath/ByCaseWorkflowId',
      ),
    ],
  }),
  modelOf({
    area: 'CaseWorkflowXPath',
    keys: { id: 'id', parent: 'caseWorkflowId', naturalKey: 'name' },
  }),
  modelOf({
    area: 'EntityAnalysisModelTag',
    keys: { id: 'id', parent: 'entityAnalysisModelId', naturalKey: 'name' },
  }),
]

export async function stubHierarchy(page: Page) {
  await stubFormModels(page, models)
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityAnalysisModel', (route) =>
    route.fulfill({
      json: [
        { id: 12, guid: 'g12', name: 'Fraud model' },
        { id: 13, guid: 'g13', name: 'AML model' },
      ],
    }),
  )
  await page.route('**/api/EntityAnalysisModel/*', (route) => {
    const id = Number(new URL(route.request().url()).pathname.split('/').pop())

    return route.fulfill({
      json: { id, guid: `g${id}`, name: id === 12 ? 'Fraud model' : 'AML model' },
    })
  })
  await page.route('**/api/CaseWorkflow/ByEntityAnalysisModelId/*', (route) =>
    route.fulfill({
      json: [
        {
          id: 5,
          guid: 'w5',
          name: 'Card fraud',
          entityAnalysisModelId: 12,
          active: true,
          locked: false,
        },
        {
          id: 6,
          guid: 'w6',
          name: 'AML review',
          entityAnalysisModelId: 12,
          active: true,
          locked: true,
        },
        {
          id: 7,
          guid: 'w7',
          name: 'Retired',
          entityAnalysisModelId: 12,
          active: false,
          locked: false,
        },
      ],
    }),
  )
  await page.route('**/api/CaseWorkflow/5', (route) =>
    route.fulfill({ json: { id: 5, guid: 'w5', name: 'Card fraud', entityAnalysisModelId: 12 } }),
  )
  await page.route('**/api/CaseWorkflowXPath/ByCaseWorkflowId/*', (route) =>
    route.fulfill({ json: [{ id: 9, guid: 'x9', name: 'Merchant', caseWorkflowId: 5 }] }),
  )
  await page.route('**/api/CaseWorkflowXPath/9', (route) =>
    route.fulfill({ json: { id: 9, guid: 'x9', name: 'Merchant', caseWorkflowId: 5 } }),
  )
  await page.route('**/api/EntityAnalysisModelTag/ByEntityAnalysisModelId/*', (route) =>
    route.fulfill({ json: [] }),
  )
}
