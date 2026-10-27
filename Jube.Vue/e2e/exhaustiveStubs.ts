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
import { readFileSync } from 'node:fs'
import type { Page } from '@playwright/test'
import { me } from './hierarchyStub'

export const instance = (overrides: Record<string, unknown> = {}) => ({
  id: 1,
  guid: 'g-1',
  name: 'Fraud v1',
  statusId: 16,
  models: 142,
  modelsSinceBest: 12,
  score: 0.9123,
  topologyComplexity: 31,
  anomalySummary: 'Isolation forest over 12 variables',
  completedDate: '2026-10-21T09:00:00Z',
  updatedDate: '2026-10-21T09:00:00Z',
  createdDate: '2026-10-21T08:00:00Z',
  createdUser: 'alex',
  dataSourceId: 0,
  filter: true,
  anomaly: false,
  bootstrap: true,
  ...overrides,
})

const variable = (id: number, name: string, correlation: number, extra = {}) => ({
  id,
  name,
  mean: 10.5,
  standardDeviation: 2,
  maximum: 99,
  minimum: 1,
  kurtosis: 0.3,
  skewness: 0.1,
  iqr: 3,
  normalisationType: 'ZScore',
  distinctValues: 40,
  correlation,
  correlationAbsRank: id,
  categoricalSourceName: null,
  categoricalLevel: null,
  ...extra,
})

const variables = [variable(1, 'Amount', 0.95), variable(2, 'Velocity', 0.4)]

const promoted = [
  {
    id: 11,
    exhaustiveSearchInstanceTrialInstanceId: 5,
    active: true,
    score: 0.9123,
    topologyComplexity: 31,
    kind: 'ExhaustiveTopologyNetwork',
    createdDate: '2026-10-21T08:30:00Z',
    calibrationMethod: 'platt',
  },
  {
    id: 12,
    exhaustiveSearchInstanceTrialInstanceId: 6,
    active: false,
    score: 0.88,
    topologyComplexity: 20,
    kind: 'GradientBoostingPipeline',
    createdDate: '2026-10-21T08:10:00Z',
    calibrationMethod: 'isotonic',
  },
]

const prescriptions = [
  {
    id: 1,
    name: 'Amount',
    variableMean: 10,
    variableStandardDeviation: 2,
    variableMaximum: 99,
    variableMinimum: 1,
    prescriptionMean: 70,
    prescriptionStandardDeviation: 5,
    prescriptionMaximum: 99,
    prescriptionMinimum: 40,
    sensitivity: 0.6,
    positive: true,
    promotedTrialInstanceVariableId: 901,
  },
]

const sliders = [
  {
    id: 1,
    name: 'Amount',
    mean: 50,
    maximum: 100,
    minimum: 0,
    emptyRange: false,
    categoricalSourceName: null,
    categoricalKind: null,
    categoricalLevel: null,
    categoricalReferenceLabel: null,
  },
  {
    id: 2,
    name: 'c1',
    mean: 0,
    maximum: 1,
    minimum: 0,
    emptyRange: false,
    categoricalSourceName: 'Country',
    categoricalKind: 1,
    categoricalLevel: 'GB',
    categoricalReferenceLabel: 'Other',
  },
]

const trials = [
  {
    id: 5,
    createdDate: '2026-10-21T08:30:00Z',
    kind: 'ExhaustiveTopologyNetwork',
    score: 0.9123,
    promoted: true,
  },
  {
    id: 6,
    createdDate: '2026-10-21T08:10:00Z',
    kind: 'GradientBoostingPipeline',
    score: 0.88,
    promoted: false,
  },
]

const narrative = {
  holistic: ['The model separates fraud well.'],
  groups: {
    Prescription: ['Amount drives the score.'],
    Gains: ['The top decile captures most of the value.'],
    LearningCurve: ['Learning flattened after 100 models.'],
  },
}

const chart =
  '<svg class="jc" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 100" role="img" aria-label="chart"><rect x="10" y="10" width="100" height="30"/></svg>'

const chartStylesheet = readFileSync('../Jube.App/wwwroot/css/jube-charting.css', 'utf8')

export interface Seen {
  charts: { method: string; path: string; body: unknown }[]
  stops: string[]
  activations: Record<string, unknown>[]
  recalls: Record<string, unknown>[]
  batches: number
}

export async function stubExhaustive(
  page: Page,
  options: {
    statuses?: number[]
    instanceStatus?: number
    activateStatus?: number
    batch?: { status: number; json: object }
  } = {},
) {
  const seen: Seen = { charts: [], stops: [], activations: [], recalls: [], batches: 0 }
  let statuses = options.statuses ?? [options.instanceStatus ?? 16]

  await page.route('**/css/jube-charting.css', (route) =>
    route.fulfill({ contentType: 'text/css', body: chartStylesheet }),
  )
  await page.route('**/api/Chart/**', (route) => {
    const request = route.request()

    seen.charts.push({
      method: request.method(),
      path: new URL(request.url()).pathname + new URL(request.url()).search,
      body: request.method() === 'POST' ? request.postDataJSON() : null,
    })

    return route.fulfill({ contentType: 'image/svg+xml', body: chart })
  })

  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/EntityAnalysisModelIntegrity/Models', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Payments', active: true }] }),
  )
  await page.route('**/api/ExhaustiveSearchInstance/ByEntityAnalysisModelId/1', (route) =>
    route.fulfill({ json: [{ id: 1, name: 'Fraud v1' }] }),
  )
  await page.route('**/api/ExhaustiveSearchInstance/1', (route) => {
    const status = statuses.length > 1 ? statuses.shift()! : statuses[0]!

    return route.fulfill({ json: instance({ statusId: status }) })
  })
  await page.route('**/api/ExhaustiveSearchInstance/stop/g-1', (route) => {
    seen.stops.push('g-1')
    statuses = [18]

    return route.fulfill({ json: {} })
  })
  await page.route('**/api/GetExhaustiveSearchInstanceVariableQuery/1', (route) =>
    route.fulfill({ json: variables }),
  )
  await page.route(
    '**/api/GetExhaustiveSearchInstanceTrialInstanceVariableVarianceQuery/*',
    (route) =>
      route.fulfill({ json: [{ name: 'Velocity', correlation: 0.92, correlationAbsRank: 1 }] }),
  )
  await page.route('**/api/GetExhaustiveSearchInstancePromotedTrialInstanceQuery/1', (route) =>
    route.fulfill({ json: promoted }),
  )
  await page.route('**/api/ExhaustiveSearchInstancePromotedTrialInstance', (route) => {
    seen.activations.push(route.request().postDataJSON())

    return options.activateStatus && options.activateStatus !== 200
      ? route.fulfill({
          status: options.activateStatus,
          json: {
            errors: [{ propertyName: 'Active', errorMessage: 'One model must stay active.' }],
          },
        })
      : route.fulfill({ json: {} })
  })
  await page.route(
    '**/api/GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery/*',
    (route) => route.fulfill({ json: prescriptions }),
  )
  await page.route(
    '**/api/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery/1',
    (route) => route.fulfill({ json: sliders }),
  )
  await page.route('**/api/Invoke/ExhaustiveSearchInstance/g-1', (route) => {
    const body = route.request().postDataJSON()

    seen.recalls.push(body)

    return route.fulfill({
      json: {
        score: 0.5 + Number(body.Amount) / 1000,
        baseValue: 0.1,
        relevance: [],
        nodeRelevance: [[0.1, 0.2]],
      },
    })
  })
  await page.route('**/api/Invoke/ExhaustiveSearchInstance/g-1/Batch', (route) => {
    seen.batches += 1

    return options.batch
      ? route.fulfill({ status: options.batch.status, json: options.batch.json })
      : route.fulfill({
          json: {
            rowCount: 2,
            scoredCount: 1,
            mappedVariables: ['Amount'],
            unmappedVariables: ['Velocity'],
            warnings: ['One row was empty.'],
            rows: [
              { rowNumber: 1, score: 0.7, baseValue: 0.1, relevance: [], error: null },
              { rowNumber: 2, score: null, baseValue: null, relevance: [], error: 'No values' },
            ],
          },
        })
  })
  await page.route('**/api/GetExhaustiveSearchInstanceTrialInstanceListQuery/1', (route) =>
    route.fulfill({ json: trials }),
  )
  await page.route('**/api/GetExhaustiveSearchInstanceTrialInstanceDetail/*', (route) =>
    route.fulfill({
      json: {
        id: 5,
        kind: 'ExhaustiveTopologyNetwork',
        score: 0.9123,
        hasDiagram: true,
        evidence: { layers: 3 },
      },
    }),
  )
  await page.route('**/api/Chart/Exhaustive/Narrative/1', (route) =>
    route.fulfill({ json: narrative }),
  )
  return seen
}
