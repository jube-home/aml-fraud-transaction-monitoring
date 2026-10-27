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
import { row } from '../src/cases/__tests__/fixtures'
import type { CaseSummary } from '../src/cases/types'
import { me } from './hierarchyStub'
import { setTheme, settleAnimations } from './support'

const workflow = '11111111-1111-1111-1111-111111111111'
const filter = '33333333-3333-3333-3333-333333333333'
const own = '44444444-4444-4444-4444-444444444444'
const role = '55555555-5555-5555-5555-555555555555'

const chartSvg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 40" role="img"><title>Chart</title><rect width="120" height="40" fill="#cfccc4"/></svg>'

const emptyGroup = { condition: 'AND', rules: [], valid: true }

const builderCompletions = [
  {
    name: 'Payload.Name',
    value: 'Payload.Name',
    meta: 'Name:string',
    score: 1000,
    dataType: 'string',
    group: 'Payload',
    field: `"Json" ->> 'Name'`,
  },
  {
    name: 'Payload.Amount',
    value: 'Payload.Amount',
    meta: 'Amount:double',
    score: 1000,
    dataType: 'double',
    group: 'Payload',
    field: `("Json" ->> 'Amount')::double precision`,
  },
  {
    name: 'Payload.Counterparty',
    value: 'Payload.Counterparty',
    meta: 'Counterparty:string',
    score: 1000,
    dataType: 'string',
    group: 'Payload',
    field: `"Json" ->> 'Counterparty'`,
  },
]

const rows = [
  row(),
  row({
    caseId: 9042,
    caseKeyValue: 'ACC-502',
    qaSample: true,
    value: null,
    locked: true,
    lockedUser: 'sam',
    attention: {
      needed: true,
      reasons: [{ code: 1, text: null }],
      lastViewedDate: '2026-10-21T09:00:00Z',
      changesSince: 2,
      priorityRise: 8,
    },
  }),
]

const counts = [
  { kind: 'View', key: '1', name: 'Mine', count: 2, mine: false, failed: false },
  { kind: 'View', key: '2', name: 'Returned', count: 1, mine: false, failed: false },
  { kind: 'View', key: '6', name: 'All', count: 120, mine: false, failed: false },
  { kind: 'View', key: '7', name: 'QA', count: 2, mine: false, failed: false },
  { kind: 'Workflow', key: filter, name: 'Structuring', count: 14, mine: false, failed: false },
  { kind: 'Own', key: own, name: 'High value', count: 3, mine: true, failed: false },
]

const summary: CaseSummary = {
  case: rows[0]!,
  contributions: [
    {
      family: 'Rule',
      label: 'Structuring 9d',
      source: 'rule',
      confirmed: 30,
      closed: 50,
      precision: 0.6,
      logLikelihoodRatio: 1.2,
      decay: 1,
      weight: 1,
      logOdds: 1.2,
      sharesInputs: false,
    },
  ],
  baseRate: 0.2,
  impact: null,
  riskHistory: [
    {
      sequence: 1,
      createdDate: '2026-10-20T09:00:00Z',
      trigger: 1,
      lane: 4,
      likelihood: 0.2,
      impact: 2,
      priority: 20,
    },
    {
      sequence: 2,
      createdDate: '2026-10-20T10:00:00Z',
      trigger: 2,
      lane: 3,
      likelihood: 0.4,
      impact: 3,
      priority: 41,
    },
    {
      sequence: 3,
      createdDate: '2026-10-20T12:00:00Z',
      trigger: 7,
      lane: 3,
      likelihood: 0.5,
      impact: 3,
      priority: 48,
    },
    {
      sequence: 4,
      createdDate: '2026-10-21T12:00:00Z',
      trigger: 6,
      lane: 1,
      likelihood: 0.8,
      impact: 4,
      priority: 77,
    },
    {
      sequence: 5,
      createdDate: '2026-10-23T12:00:00Z',
      trigger: 5,
      lane: 2,
      likelihood: 0.9,
      impact: 5,
      priority: 90,
    },
  ],
  stays: [
    {
      caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222222',
      statusName: 'Awaiting review',
      enterReason: 1,
      enteredDate: '2026-10-20T09:00:00Z',
      enteredUser: null,
      exitedDate: null,
      exitReason: null,
      minutes: 300,
      maxMinutes: null,
    },
  ],
  recentEvents: [],
  notes: [],
  keyHistory: { closed: 3, confirmed: 1, newestClosedDate: null, otherOpenCases: 0 },
  since: {
    firstLook: true,
    lastViewedDate: null,
    priorityThen: null,
    priorityNow: 62,
    changes: [],
  },
}

const analytics = {
  from: '2026-09-23T00:00:00Z',
  to: '2026-10-23T00:00:00Z',
  casesAnalysed: 400,
  truncated: false,
  qaSample: {
    picked: 12,
    open: 2,
    closed: 10,
    decided: 10,
    confirmed: 3,
    confirmedRate: 0.3,
    overallConfirmedRate: 0.35,
    enough: true,
  },
  headline: {
    opened: 100,
    closed: 90,
    openNow: 120,
    netChange: 10,
    unassessedOpen: 0,
    urgentOpen: 7,
    decided: 60,
    confirmedRate: 0.35,
    medianFirstViewMinutes: 95,
    medianResolutionMinutes: 2000,
    slaBreachedOpen: 4,
    slaAtRiskOpen: 9,
    reopenRate: 0.02,
  },
  flow: Array.from({ length: 14 }, (_, index) => ({
    date: `2026-10-${String(10 + index).padStart(2, '0')}T00:00:00Z`,
    opened: 8 + (index % 3),
    closed: 5 + (index % 4),
    backlog: 100 + index,
  })),
  myDay: {
    goal: 10,
    closedToday: 7,
    streak: 4,
    bestDay: 12,
    typicalDay: 5,
    days: Array.from({ length: 14 }, (_, index) => ({
      date: `2026-10-${String(10 + index).padStart(2, '0')}T00:00:00Z`,
      closed: index % 5,
    })),
  },
}

const caseDetail = {
  id: 9041,
  entityAnalysisModelInstanceEntryGuid: '33333333-3333-3333-3333-333333333333',
  diaryDate: '2026-10-25T09:00:00Z',
  caseWorkflowGuid: workflow,
  caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222222',
  createdDate: '2026-10-20T09:00:00Z',
  locked: true,
  lockedUser: 'alex',
  lockedDate: '2026-10-24T09:00:00Z',
  closedStatusId: 0,
  closedDate: '2026-10-24T09:00:00Z',
  closedUser: null,
  caseKey: 'AccountId',
  diary: true,
  diaryUser: 'alex',
  rating: 3,
  caseKeyValue: 'ACC-441',
  foreColor: '#112233',
  backColor: '#e8eef2',
  formattedPayload: [
    {
      name: 'Amount',
      value: '45000',
      cellFormatForeColor: null,
      cellFormatForeRow: false,
      cellFormatBackColor: null,
      cellFormatBackRow: false,
      existsMatch: false,
      conditionalRegularExpressionFormatting: false,
    },
    {
      name: 'Counterparty',
      value: 'ACME Ltd',
      cellFormatForeColor: null,
      cellFormatForeRow: false,
      cellFormatBackColor: null,
      cellFormatBackRow: false,
      existsMatch: false,
      conditionalRegularExpressionFormatting: false,
    },
  ],
  activation: [{ name: 'Structuring 9d' }, { name: 'High value' }],
  enableVisualisation: false,
  entityAnalysisModelId: 1,
}

const keyCases = [
  {
    id: 9040,
    caseWorkflowStatusName: 'Closed',
    createdDate: '2026-09-20T09:00:00Z',
    locked: false,
    lockedUser: null,
    closedStatusId: 1,
    closedDate: '2026-09-21T09:00:00Z',
    closedUser: 'sam',
    caseKey: 'AccountId',
    caseKeyValue: 'ACC-441',
  },
]

const keyNotes = [
  {
    id: 1,
    caseId: 9040,
    createdDate: '2026-09-21T09:00:00Z',
    createdUser: 'sam',
    note: '<p>Customer <b>confirmed</b> the transfer</p><script>window.hacked = true</script>',
    action: 'Close',
    priority: 'Normal',
  },
]

const keyEvents = [
  {
    id: 1,
    caseId: 9040,
    caseEventType: 'Status change',
    createdDate: '2026-09-21T09:00:00Z',
    createdUser: 'sam',
    before: 'Open',
    after: 'Closed',
  },
]

const journalColumns = (lens: string) => {
  const base = [
    ['Id', 'number', 'base'],
    ['EntityAnalysisModelInstanceEntryGuid', 'string', 'base'],
    ['ReferenceDate', 'date', 'base'],
    ['CreatedDate', 'date', 'base'],
    ['ResponseElevation', 'number', 'base'],
    ['EntryKeyValue', 'string', 'base'],
    ['Activations', 'string', 'base'],
    ['ActivationCount', 'number', 'base'],
    ['Tag', 'array', 'base'],
  ].map(([name, type, group]) => ({
    name,
    title: name,
    type,
    roleId: 1,
    roleInferred: true,
    lens: 'base',
    group,
    drill: false,
  }))

  const own =
    lens === 'rules'
      ? [{ name: 'rule:Velocity', title: 'Velocity', type: 'string', group: 'rule' }]
      : [
          { name: 'Amount', title: 'Amount', type: 'number', group: 'payload' },
          { name: 'Counterparty', title: 'Counterparty', type: 'string', group: 'payload' },
        ]

  return [
    ...base,
    ...own.map((column) => ({ ...column, roleId: 1, roleInferred: true, lens, drill: false })),
  ]
}

const journalRows = (lens: string) => ({
  meta: {
    key: 'AccountId',
    value: 'ACC-441',
    caseWorkflowGuid: workflow,
    lens,
    rowCount: 2,
    total: 2,
    drillKeys: [
      { key: 'DeviceId', title: 'Device' },
      { key: 'IpAddress', title: 'Ip address' },
    ],
    limit: 1000,
    truncated: false,
    from: '2026-07-26T00:00:00Z',
    to: '2026-10-24T00:00:00Z',
  },
  columns: journalColumns(lens),
  lenses: [
    { lens: 'payload', rows: 2 },
    { lens: 'rules', rows: 1 },
    { lens: 'features', rows: 2 },
    { lens: 'models', rows: 1 },
    { lens: 'sanctions', rows: 0 },
  ],
  rows: [
    {
      Id: 1,
      EntityAnalysisModelInstanceEntryGuid: 'tx-1',
      ReferenceDate: '2026-10-19T09:00:00Z',
      CreatedDate: '2026-10-19T09:00:00Z',
      ResponseElevation: 2,
      Activations: '',
      ActivationCount: 0,
      Tag: ['mule'],
      Amount: 120.5,
      Counterparty: 'ACME Ltd',
    },
    {
      Id: 2,
      EntityAnalysisModelInstanceEntryGuid: 'tx-2',
      ReferenceDate: '2026-10-20T09:00:00Z',
      CreatedDate: '2026-10-20T09:00:00Z',
      ResponseElevation: 9,
      Activations: 'Velocity',
      ActivationCount: 1,
      Tag: [],
      Amount: 45000,
      Counterparty: 'Globex',
      'rule:Velocity': 'fired',
    },
  ],
})

const journalAnalytics = {
  meta: { rowCount: 2, limit: 2000, truncated: false, lowConfidence: true, sections: [] },
  fields: [
    {
      name: 'Amount',
      title: 'Amount',
      lens: 'payload',
      group: 'payload',
      roleId: 1,
      roleInferred: true,
      kind: 'measure',
    },
  ],
  summary: {
    count: 2,
    first: '2026-10-19T09:00:00Z',
    last: '2026-10-20T09:00:00Z',
    activeDays: 2,
    activations: 1,
    activationRows: 1,
    maxElevation: 9,
    fields: [
      {
        field: 'Amount',
        count: 2,
        sum: 45120.5,
        mean: 22560,
        median: 120.5,
        max: 45000,
        latest: 45000,
      },
    ],
  },
  notable: {
    rows: [
      {
        id: 2,
        score: 5,
        worst: 'Amount',
        reasons: [{ field: 'Amount', value: 45000, median: 120.5, z: 5 }],
      },
    ],
    lowConfidenceFields: ['Amount'],
  },
  novelty: { fields: [], rows: {} },
  peers: {
    available: true,
    reason: null,
    computedDate: '2026-10-20T03:00:00Z',
    windowDays: 90,
    segment: 'Tier',
    segmentValue: 'Gold',
    segmentFallback: false,
    comparisons: [
      {
        field: 'Amount',
        keyMedian: 22800,
        keyRows: 2,
        peerMedian: 310,
        peerScale: 140,
        peerQ1: 200,
        peerQ3: 420,
        peerKeys: 412,
        z: 5,
        percentile: 0.998,
        direction: 'above',
        lowConfidence: true,
      },
    ],
  },
}

const transactionCard = {
  id: 2,
  entityAnalysisModelInstanceEntryGuid: '55555555-5555-5555-5555-555555555555',
  referenceDate: '2026-10-20T09:00:00Z',
  createdDate: '2026-10-20T09:00:00Z',
  responseElevation: 9,
  entryKeyValue: 'ACC-441',
  modelName: 'Retail',
  tags: [],
  activations: [{ name: 'Velocity', forced: false }],
  values: [{ group: 'abstraction', name: 'Volume', value: 12.5 }],
  keys: [
    { name: 'Device', value: 'D-9', drill: true, payloadKey: 'Device' },
    { name: 'Account', value: 'A1', drill: false, payloadKey: null },
  ],
  adaptations: [
    {
      source: 'exhaustive',
      name: 'StructNet',
      value: 0.91,
      error: null,
      humanLabel: 'Very likely',
      narrative: 'Large cash in, then out.',
      model: {
        name: 'StructNet',
        family: 'NeuralNetwork',
        version: '7',
        artifactHash: 'abcdef1234',
        trainedDate: null,
        featureCount: 4,
      },
      result: { threshold: 0.8, activated: true, expectedPositiveRate: 0.31 },
      calibration: {
        space: 'Probability',
        calibrated: true,
        method: 'Platt',
        sample: 212,
        brier: 0.12,
        bands: [{ lower: 0, upper: 1, expected: 0.8, observed: 0.7, count: 112 }],
      },
      contribution: {
        space: 'LogOdds',
        method: 'TreePathContribution',
        exact: true,
        baseValue: -3.18,
        items: [
          {
            name: 'Amount',
            weight: 2.2,
            direction: null,
            significance: null,
            source: 'Payload',
            humanLabel: null,
            signed: 2.2,
            hidden: false,
          },
          {
            name: null,
            weight: null,
            direction: null,
            significance: null,
            source: 'Payload',
            humanLabel: null,
            signed: null,
            hidden: true,
          },
        ],
        hiddenItems: 1,
      },
      journey: [
        {
          feature: 'Amount',
          operator: '>',
          threshold: 5000,
          thresholdCategory: null,
          source: 'Payload',
          humanLabel: null,
          hidden: false,
        },
        {
          feature: null,
          operator: null,
          threshold: null,
          thresholdCategory: null,
          source: 'Payload',
          humanLabel: null,
          hidden: true,
        },
      ],
    },
  ],
}

async function stub(
  page: Page,
  options: {
    permissions?: number[]
    skim?: unknown
    peersNotComputed?: boolean
    layout?: string
    holds?: boolean
    holdStatus?: number
  } = {},
) {
  const pages: URLSearchParams[] = []
  const skims: unknown[] = []
  const saves: { method: string; body: unknown }[] = []
  const charts: URL[] = []
  const journalCalls: URLSearchParams[] = []
  const analyticsCalls: URLSearchParams[] = []
  const payloadCalls: URLSearchParams[] = []
  const tagPuts: { path: string; body: Record<string, unknown> }[] = []
  const pinPuts: Record<string, unknown>[] = []
  const layoutPosts: { caseWorkflowGuid: string; json: string }[] = []
  let storedLayout: string | null = options.layout ?? null
  const jobCalls: { path: string; method: string; body: unknown }[] = []
  let jobPolls = 0
  const pinned = new Set<string>()
  const transactionCharts: URL[] = []
  const changes: Record<string, unknown>[] = []
  const notesPosted: Record<string, unknown>[] = []
  const macrosRun: Record<string, unknown>[] = []
  const formsPosted: Record<string, unknown>[] = []
  const holdDecisions: { path: string; body: Record<string, unknown> }[] = []
  let heldState = 1
  let decisionReason = ''

  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => route.fulfill({ status: 404, json: {} }),
  )
  await page.route('**/api/Me', (route) =>
    route.fulfill({ json: { ...me, permissions: options.permissions ?? [1] } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/CaseSearch/Workflows', (route) =>
    route.fulfill({ json: [{ guid: workflow, name: 'AML structuring', modelName: 'Retail' }] }),
  )
  await page.route('**/api/CaseSearch/Counts/**', (route) => route.fulfill({ json: counts }))
  await page.route('**/api/GetCaseByIdQuery/*', (route) => route.fulfill({ json: caseDetail }))
  await page.route(/\/api\/GetCaseByCaseKeyValueQuery/, (route) =>
    route.fulfill({ json: keyCases }),
  )
  await page.route(/\/api\/GetCaseNoteByCaseKeyValueQuery/, (route) =>
    route.fulfill({ json: keyNotes }),
  )
  await page.route(/\/api\/GetCaseEventByCaseKeyValueQuery/, (route) =>
    route.fulfill({ json: keyEvents }),
  )
  await page.route('**/api/Case/9041', (route) =>
    route.fulfill({
      json: {
        id: 9041,
        caseWorkflowStatusGuid: caseDetail.caseWorkflowStatusGuid,
        locked: false,
        lockedUser: null,
        closedStatusId: 0,
        rating: 0,
        diary: false,
        diaryDate: null,
        caseKey: 'AccountId',
        caseKeyValue: 'ACC-441',
      },
    }),
  )
  await page.route('**/api/Case', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback()

    changes.push(route.request().postDataJSON())

    return route.fulfill({ json: route.request().postDataJSON() })
  })
  await page.route('**/api/CaseWorkflowAction/**', (route) =>
    route.fulfill({ json: [{ id: 7, name: 'Called customer' }] }),
  )
  await page.route('**/api/CaseWorkflowPriority/**', (route) =>
    route.fulfill({ json: [{ id: 2, name: 'Normal' }] }),
  )
  await page.route('**/api/CaseWorkflowMacro/**', (route) =>
    route.fulfill({ json: [{ id: 5, name: 'Email the branch', description: 'Tells the branch' }] }),
  )
  await page.route('**/api/CaseWorkflowMacroExecution', (route) => {
    macrosRun.push(route.request().postDataJSON())

    return route.fulfill({ json: route.request().postDataJSON() })
  })
  await page.route('**/api/CaseWorkflowDisplay/**', (route) =>
    route.fulfill({ json: [{ id: 3, name: 'STR summary' }] }),
  )
  await page.route('**/api/CaseWorkflowDisplayExecution', (route) =>
    route.fulfill({ json: '<h1>Summary for the branch</h1><script>window.shown = true</script>' }),
  )
  await page.route('**/api/CaseWorkflowForm/**', (route) =>
    route.fulfill({ json: [{ id: 7, name: 'Suspicious activity report' }] }),
  )
  await page.route('**/api/CaseWorkflowDisplayExecution/Form', (route) => {
    const values = (route.request().postDataJSON().values ?? {}) as Record<string, string>
    const other = values.reason === 'other'

    return route.fulfill({
      json: {
        html:
          '<form><fieldset><legend>Report</legend><label for="reason">Reason</label><select name="reason"><option value="fraud">Fraud</option><option value="other">Other</option></select>' +
          (other ? '<label for="detail">Detail</label><input name="detail">' : '') +
          '<label for="amount">Amount</label><input type="number" name="amount"></fieldset></form>',
        visibleFields: other ? ['reason', 'detail', 'amount'] : ['reason', 'amount'],
        fields: [],
      },
    })
  })
  await page.route('**/api/CaseHold/ByCaseId/*', (route) =>
    route.fulfill({
      json: options.holds
        ? [
            {
              entityAnalysisModelInstanceEntryGuid: '33333333-3333-3333-3333-333333333333',
              entryKeyValue: 'PAY-77',
              referenceDate: '2026-10-21T09:00:00Z',
              stateId: heldState,
              rangeName: 'Review hold',
              rangeMinimum: 10,
              rangeMaximum: 19,
              elevation: 12,
              stampedDate: '2026-10-21T09:00:05Z',
              deadlineDate: heldState === 1 ? '2099-10-21T17:00:00Z' : null,
              decision:
                heldState === 1
                  ? null
                  : {
                      actor: 'alex',
                      date: '2026-10-21T10:00:00Z',
                      reason: decisionReason,
                      releaseHookId: 4,
                      releaseHookName: 'Faster payments',
                      httpStatus: 200,
                    },
              legs: [
                {
                  kindId: 3,
                  outcomeId: 2,
                  date: '2026-10-21T09:00:07Z',
                  destination: 'https://hooks.example.com/hold',
                  httpStatus: 200,
                  detail: null,
                },
              ],
              releaseHookName: 'Faster payments',
              refuseCallsHook: false,
            },
          ]
        : [],
    }),
  )
  await page.route('**/api/CaseHold/Release', (route) => {
    const body = route.request().postDataJSON() as Record<string, unknown>

    holdDecisions.push({ path: 'Release', body })

    if (options.holdStatus === 502) {
      return route.fulfill({
        status: 502,
        json: { code: 'HoldHookFailed', message: 'The release hook did not release the payment.' },
      })
    }

    heldState = 3
    decisionReason = String(body.reason)

    return route.fulfill({
      json: { stateId: 3, releaseHookName: 'Faster payments', httpStatus: 200, version: 2 },
    })
  })
  await page.route('**/api/CaseHold/Refuse', (route) => {
    const body = route.request().postDataJSON() as Record<string, unknown>

    holdDecisions.push({ path: 'Refuse', body })
    heldState = 4
    decisionReason = String(body.reason)

    return route.fulfill({
      json: { stateId: 4, releaseHookName: null, httpStatus: null, version: 2 },
    })
  })
  await page.route('**/api/CaseWorkflowDisplayExecution/Dispatches', (route) =>
    route.fulfill({
      json: [
        {
          id: 52,
          caseId: 9041,
          sourceKindId: 2,
          sourceName: 'Email the branch',
          channelId: 1,
          modeId: 2,
          destination: 'branch@example.com',
          subject: 'Case AC-9',
          body: '<b>Body</b> of the message',
          outcomeId: 1,
          detail: null,
          responseStatus: null,
          correlationId: '00000000-0000-0000-0000-000000000052',
          rows: '[{"balance":1234.5}]',
          createdUser: 'alex',
          createdDate: '2026-10-20T10:30:00Z',
        },
        {
          id: 51,
          caseId: 9041,
          sourceKindId: 4,
          sourceName: 'Escalated',
          channelId: 3,
          modeId: 2,
          destination: 'https://hooks.example.com/case/9041',
          subject: null,
          body: null,
          outcomeId: 3,
          detail: 'The endpoint was refused: hostChanged.',
          responseStatus: null,
          correlationId: '00000000-0000-0000-0000-000000000051',
          rows: null,
          createdUser: 'sam',
          createdDate: '2026-10-19T09:00:00Z',
        },
      ],
    }),
  )
  await page.route('**/api/CaseWorkflowDisplayExecution/FormEntries', (route) =>
    route.fulfill({
      json: [
        {
          id: 31,
          caseId: 9041,
          formName: 'Suspicious activity report',
          createdUser: 'alex',
          createdDate: '2026-10-20T10:30:00Z',
          html: '<form><label for="reason">Reason</label><select name="reason" disabled><option value="fraud" selected>Fraud</option></select><script>window.entryShown = true</script></form>',
          otherValues: [{ name: 'retired', value: 'old value' }],
          error: null,
        },
        {
          id: 30,
          caseId: 9041,
          formName: 'Old form',
          createdUser: 'sam',
          createdDate: '2026-10-19T09:00:00Z',
          html: '',
          otherValues: [{ name: 'detail', value: 'kept' }],
          error: 'Template line 1, column 1: the tag <script> is not allowed.',
        },
      ],
    }),
  )
  await page.route('**/api/CaseWorkflowFormEntry', (route) => {
    const body = route.request().postDataJSON() as { payload: Record<string, string> }

    formsPosted.push(body)

    return body.payload.amount === '999'
      ? route.fulfill({
          status: 400,
          json: {
            isValid: false,
            errors: [
              {
                propertyName: 'Payload.amount',
                errorMessage: 'That value is not valid for the type of the field.',
              },
            ],
          },
        })
      : route.fulfill({ json: { id: 1 } })
  })
  await page.route('**/api/CaseNote', (route) => {
    if (route.request().method() !== 'POST') return route.fallback()

    notesPosted.push(route.request().postDataJSON())

    return route.fulfill({ json: route.request().postDataJSON() })
  })
  await page.route(/\/api\/CaseKeyJournal(\?|$)/, (route) => {
    const params = new URL(route.request().url()).searchParams

    journalCalls.push(params)

    const merged = params.has('merge')
    const only = params.get('pinnedOnly') === 'true'
    const body = journalRows(params.get('lens') ?? 'payload')
    const narrowed = params.has('filter')

    return route.fulfill({
      json: {
        ...body,
        meta: {
          ...body.meta,
          total: body.rows.length,
          rowCount: narrowed ? body.rows.length - 1 : body.rows.length,
        },
        rows: body.rows
          .filter((_, index) => !narrowed || index > 0)
          .filter((row) => !only || pinned.has(row.EntityAnalysisModelInstanceEntryGuid))
          .map((row) => ({
            ...row,
            Pinned: pinned.has(row.EntityAnalysisModelInstanceEntryGuid),
            ...(merged ? { Lane: row.Id === 1 ? 'AB' : 'B' } : {}),
          })),
      },
    })
  })
  await page.route(/\/api\/SessionCaseJournal/, (route) => {
    const request = route.request()

    if (request.method() === 'POST') {
      const body = request.postDataJSON() as { caseWorkflowGuid: string; json: string }

      layoutPosts.push(body)
      storedLayout = body.json

      return route.fulfill({ json: { json: storedLayout } })
    }

    return storedLayout === null
      ? route.fulfill({ status: 204 })
      : route.fulfill({ json: { json: storedLayout } })
  })

  await page.route(/\/api\/CaseKeyJournal\/Replays/, (route) =>
    route.fulfill({ json: { replays: [] } }),
  )

  await page.route(/\/api\/CaseKeyJournal\/Pins/, (route) => {
    const request = route.request()

    if (request.method() === 'PUT') {
      const body = request.postDataJSON() as { transactions: string[]; pinned: boolean }

      pinPuts.push(body)

      for (const guid of body.transactions) {
        if (body.pinned) pinned.add(guid)
        else pinned.delete(guid)
      }
    }

    return route.fulfill({ json: { transactions: [...pinned] } })
  })
  await page.route(/\/api\/CaseKeyJournal\/TagJobs/, (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname.replace('/api/CaseKeyJournal/TagJobs', '') || '/'
    const job = (overrides: Record<string, unknown>) => ({
      guid: 'job-1',
      caseWorkflowGuid: workflow,
      kind: 'Tag',
      state: 'Queued',
      key: 'AccountId',
      value: 'ACC-441',
      add: ['review'],
      remove: [],
      undoOf: null,
      total: 2340,
      processed: 0,
      changed: 0,
      skipped: 0,
      locked: 0,
      error: null,
      canUndo: false,
      createdDate: '2026-10-25T10:00:00Z',
      startedDate: null,
      finishedDate: null,
      ...overrides,
    })

    jobCalls.push({
      path,
      method: request.method(),
      body:
        request.method() === 'POST' ? request.postDataJSON() : Object.fromEntries(url.searchParams),
    })

    if (path === '/Preview') {
      return route.fulfill({ json: { matching: 2340, limit: 100000, tooMany: false } })
    }

    if (path === '/' && request.method() === 'GET') return route.fulfill({ json: [] })

    if (path === '/' && request.method() === 'POST') return route.fulfill({ json: job({}) })

    if (path === '/job-1/Undo') {
      return route.fulfill({
        json: job({
          guid: 'job-2',
          kind: 'Undo',
          undoOf: 'job-1',
          state: 'Running',
          processed: 100,
        }),
      })
    }

    if (path === '/job-2') {
      return route.fulfill({
        json: job({ guid: 'job-2', kind: 'Undo', state: 'Running', processed: 100 }),
      })
    }

    jobPolls += 1

    if (jobPolls === 1)
      return route.fulfill({ json: job({ state: 'Running', processed: 1200, changed: 1100 }) })

    return route.fulfill({
      json: job({
        state: 'Done',
        processed: 2340,
        changed: 2300,
        skipped: 3,
        locked: 12,
        canUndo: true,
        finishedDate: '2026-10-25T10:01:00Z',
      }),
    })
  })
  await page.route(/\/api\/CaseKeyJournal\/Around/, (route) =>
    route.fulfill({
      json: {
        from: '2026-10-20T00:00:00Z',
        to: '2026-10-21T00:00:00Z',
        groups: [
          {
            name: 'Device',
            key: 'Device',
            value: 'D-9',
            truncated: true,
            rows: [
              {
                id: 1,
                referenceDate: '2026-10-20T08:00:00Z',
                responseElevation: 2,
                activationCount: 0,
                activations: '',
              },
            ],
          },
        ],
      },
    }),
  )
  await page.route(/\/api\/CaseKeyJournal\/Tags/, (route) => {
    const request = route.request()

    if (request.method() === 'GET') return route.fulfill({ json: { tag: ['mule', 'review'] } })

    const body = request.postDataJSON() as Record<string, unknown>

    tagPuts.push({ path: new URL(request.url()).pathname, body })

    const restore = Array.isArray(body.sets)

    return route.fulfill({
      json: restore
        ? { previous: [], current: body.sets, skipped: 0 }
        : {
            previous: [
              { transaction: 'tx-1', tag: ['mule'] },
              { transaction: 'tx-2', tag: [] },
            ],
            current: [
              { transaction: 'tx-1', tag: ['mule', 'review'] },
              { transaction: 'tx-2', tag: ['review'] },
            ],
            skipped: 1,
            locked: 2,
          },
    })
  })
  await page.route(/\/api\/CaseKeyJournal\/Analytics/, (route) => {
    analyticsCalls.push(new URL(route.request().url()).searchParams)

    return route.fulfill({
      json: options.peersNotComputed
        ? {
            ...journalAnalytics,
            peers: { available: false, reason: 'NotComputed', comparisons: [] },
          }
        : journalAnalytics,
    })
  })
  await page.route(/\/api\/CaseKeyJournal\/Chart\//, (route) =>
    route.fulfill({
      contentType: 'image/svg+xml',
      body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><title>chart</title></svg>',
    }),
  )
  await page.route(/\/api\/CaseKeyJournal\/Transaction(\?|$)/, (route) =>
    route.fulfill({ json: transactionCard }),
  )
  await page.route(/\/api\/CaseKeyJournal\/Transaction\/Payload/, (route) => {
    payloadCalls.push(new URL(route.request().url()).searchParams)

    return route.fulfill({ json: { id: 2, fields: { Amount: 45000, Device: 'D-9' } } })
  })
  await page.route(/\/api\/CaseKeyJournal\/Transaction\/Chart\//, (route) => {
    transactionCharts.push(new URL(route.request().url()))

    return route.fulfill({
      contentType: 'image/svg+xml',
      body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><title>chart</title></svg>',
    })
  })
  await page.route('**/api/CaseSearch/Skim', (route) => {
    skims.push(route.request().postDataJSON())

    return route.fulfill({
      json: options.skim ?? { case: rows[0], nextCaseIds: [], remaining: 1 },
    })
  })
  await page.route(/\/api\/CaseSearch\?/, (route) => {
    pages.push(new URL(route.request().url()).searchParams)

    return route.fulfill({
      json: {
        total: rows.length,
        take: 100,
        skip: 0,
        sort: 0,
        generatedAt: '2026-10-23T10:00:00Z',
        rows,
      },
    })
  })
  await page.route('**/api/Completions/**', (route) => route.fulfill({ json: builderCompletions }))
  await page.route('**/api/CaseWorkflowStatus/**', (route) =>
    route.fulfill({
      json: [
        {
          guid: caseDetail.caseWorkflowStatusGuid,
          name: 'Awaiting review',
          priority: 3,
          escalationTargetCaseWorkflowStatusGuid: '44444444-4444-4444-4444-444444444444',
        },
        { guid: '44444444-4444-4444-4444-444444444444', name: 'Escalated', priority: 2 },
      ],
    }),
  )
  await page.route('**/api/RoleRegistry', (route) =>
    route.fulfill({ json: [{ guid: role, name: 'Fraud analysts' }] }),
  )
  await page.route('**/api/CaseSearchFilter/ByGuid/**', (route) =>
    route.fulfill({
      json: {
        guid: own,
        caseWorkflowGuid: workflow,
        name: 'High value',
        filterJson: JSON.stringify(emptyGroup),
        selectJson: JSON.stringify(emptyGroup),
        sharedRoleRegistryGuid: null,
        mine: true,
        version: 3,
      },
    }),
  )
  await page.route('**/api/CaseSearchFilter', (route) => {
    saves.push({ method: route.request().method(), body: route.request().postDataJSON() })

    return route.fulfill({ json: { ...(route.request().postDataJSON() as object), guid: own } })
  })
  await page.route('**/api/CaseSearchFilter/*', (route) => {
    if (route.request().method() !== 'DELETE') return route.fallback()

    saves.push({ method: 'DELETE', body: null })

    return route.fulfill({ status: 204 })
  })
  await page.route(/\/api\/CaseAnalytics\/Chart\//, (route) => {
    charts.push(new URL(route.request().url()))

    return route.fulfill({ contentType: 'image/svg+xml', body: chartSvg })
  })
  await page.route('**/api/CaseSummary/*/Waterfall**', (route) =>
    route.fulfill({ contentType: 'image/svg+xml', body: chartSvg }),
  )
  await page.route(/\/api\/CaseSummary\/\d+$/, (route) => route.fulfill({ json: summary }))
  await page.route(/\/api\/CaseAnalytics(\?|$)/, (route) => route.fulfill({ json: analytics }))

  return {
    pages,
    skims,
    saves,
    charts,
    journalCalls,
    analyticsCalls,
    payloadCalls,
    tagPuts,
    pinPuts,
    layoutPosts,
    jobCalls,
    transactionCharts,
    changes,
    notesPosted,
    macrosRun,
    formsPosted,
    holdDecisions,
  }
}

test.describe('Cases page', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('is reached from the menu and shows the ranked list with its why', async ({ page }) => {
    const { pages } = await stub(page)

    await page.goto('/tools')
    const link = page.getByRole('link', { name: 'Cases' }).first()

    await expect(link).toHaveAttribute('href', '/cases')
    await link.dispatchEvent('click')

    await expect(page).toHaveURL(/\/cases/)
    await expect(page.getByTestId('cases-list')).toBeVisible()
    await expect(page.getByText('C-9041')).toBeVisible()
    await expect(page.getByText('Structuring 9d').first()).toBeVisible()
    await expect(page.getByTestId('cases-total')).toHaveText('2 cases')
    await expect(page.getByTestId('cases-headline')).toContainText('120')
    await expect(page.getByTestId('cases-pulse-progress')).toHaveText('7 of 10 closed')
    await expect(page.getByTestId('cases-pulse-nudge')).toHaveText('3 to go')
    await expect(page.getByTestId('cases-pulse-streak')).toContainText('4-day streak')
    expect(pages[0]?.get('caseWorkflowGuid')).toBe(workflow)
    expect(pages[0]?.get('view')).toBe('6')
  })

  test('flags a case picked for quality review and offers the quality review view', async ({
    page,
  }) => {
    const { pages } = await stub(page)

    await page.goto('/cases')
    await expect(page.getByTestId('cases-list')).toContainText('QA sample')

    await page
      .getByTestId('cases-filters')
      .getByRole('button', { name: /Quality review/ })
      .click()
    await expect(page).toHaveURL(/view=7/)
    await expect.poll(() => pages.some((p) => p.get('view') === '7')).toBe(true)
  })

  test('downloads every row of the current filter as a flat csv', async ({ page }) => {
    const { pages } = await stub(page)

    await page.goto('/cases')
    await expect(page.getByTestId('cases-list')).toBeVisible()

    const download = page.waitForEvent('download')

    await page.getByTestId('export-csv').click()

    const file = await download
    const text = await (await import('node:fs/promises')).readFile((await file.path())!, 'utf8')

    expect(file.suggestedFilename()).toMatch(/^cases-\d{4}-\d{2}-\d{2}\.csv$/)
    expect(text.startsWith('\ufeffCase,Case key value')).toBe(true)
    expect(text).toContain('C-9041')
    expect(text).not.toContain('[object Object]')
    expect(pages.some((p) => p.get('take') === '200')).toBe(true)
  })

  test('returns to the last used queue when opened with no address state', async ({ page }) => {
    await stub(page)

    await page.goto('/cases?view=2')
    await expect(page.getByTestId('cases-list')).toBeVisible()
    await page.goto('/cases')
    await expect(page).toHaveURL(/view=2/)
  })

  test('holds the chosen queue and its sort in the address', async ({ page }) => {
    const { pages } = await stub(page)

    await page.goto('/cases')
    await expect(page.getByTestId('cases-list')).toBeVisible()

    await page
      .getByTestId('cases-filters')
      .getByRole('button', { name: /Returned to me/ })
      .click()
    await expect(page).toHaveURL(/view=2/)

    await page
      .getByTestId('cases-filters')
      .getByRole('button', { name: /Structuring/ })
      .click()
    await expect(page).toHaveURL(new RegExp(`filter=${filter}`))

    await page
      .getByTestId('cases-own')
      .getByRole('button', { name: /High value/ })
      .click()
    await expect(page).toHaveURL(new RegExp(`mine=${own}`))

    await page.getByTestId('cases-sort').selectOption('2')
    await expect(page).toHaveURL(/sort=2/)

    expect(pages.some((p) => p.get('caseSearchFilterGuid') === own)).toBe(true)
    expect(pages.some((p) => p.get('caseWorkflowFilterGuid') === filter)).toBe(true)
  })

  test('peeks a case without leaving the list and closes with Escape', async ({ page }) => {
    await stub(page)

    await page.goto('/cases')
    await page.getByRole('button', { name: 'Peek' }).first().click()

    await expect(page).toHaveURL(/case=9041/)
    await expect(page.getByTestId('cases-peek')).toBeVisible()
    await expect(page.getByTestId('cases-peek-contributions')).toContainText('Structuring 9d')
    await expect(page.getByTestId('cases-list')).toBeVisible()

    await page.keyboard.press('Escape')
    await expect(page.getByTestId('cases-peek')).toHaveCount(0)
    await expect(page).not.toHaveURL(/case=/)
  })

  test('takes the next case with the button and the W key', async ({ page }) => {
    const { skims } = await stub(page)

    await page.goto('/cases')
    await expect(page.getByTestId('cases-list')).toBeVisible()

    await page.getByTestId('cases-skim').click()
    await expect(page).toHaveURL(/\/cases\/9041/)
    await expect(page.getByTestId('case-page')).toBeVisible()
    await page.goBack()

    await expect(page.getByTestId('cases-list')).toBeVisible()
    await page.keyboard.press('w')
    await expect.poll(() => skims.length).toBe(2)
  })

  test('says so when there is nothing to take', async ({ page }) => {
    await stub(page, { skim: { case: null, nextCaseIds: [], remaining: 0 } })

    await page.goto('/cases')
    await page.getByTestId('cases-skim').click()

    await expect(page.getByTestId('cases-skim-empty')).toBeVisible()
  })

  test('refuses a role without the cases permission', async ({ page }) => {
    await stub(page, { permissions: [29] })

    await page.goto('/cases')

    await expect(page.getByTestId('cases-refused')).toBeVisible()
  })

  test('builds a new search, saves it and lands on it', async ({ page }) => {
    const { saves } = await stub(page)

    await page.goto('/cases')
    await page.getByTestId('cases-new-filter').click()

    await expect(page.getByTestId('cases-editor-dialog')).toBeVisible()
    await expect(page.getByTestId('cases-editor-save')).toBeDisabled()

    await page.getByTestId('cases-editor-name').fill('Smith cases')
    await page.getByTestId('cases-editor-share').selectOption(role)

    const where = page.getByTestId('where-builder')
    const select = page.getByTestId('select-builder')

    await select.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-filter-container select').selectOption('Payload.Name')
    await where.locator('.rule-operator-container select').selectOption('equal')
    await where.locator('.rule-value-container input').fill('Smith')

    await expect(page.getByTestId('cases-editor-save')).toBeEnabled()
    await page.getByTestId('cases-editor-save').click()

    await expect(page).toHaveURL(new RegExp(`mine=${own}`))
    await expect(page.getByTestId('cases-editor-dialog')).toBeHidden()

    const sent = saves[0]!.body as Record<string, unknown>

    expect(saves[0]!.method).toBe('POST')
    expect(sent.name).toBe('Smith cases')
    expect(sent.caseWorkflowGuid).toBe(workflow)
    expect(sent.sharedRoleRegistryGuid).toBe(role)
    expect(String(sent.filterJson)).toContain('Smith')
  })

  test('edits an own search with its saved name and deletes it after confirming', async ({
    page,
  }) => {
    const { saves } = await stub(page)

    await page.goto(`/cases?mine=${own}`)
    await page.getByTestId('cases-edit-search').click()

    await expect(page.getByTestId('cases-editor-name')).toHaveValue('High value')

    await page.getByTestId('cases-editor-delete').click()
    await page.getByTestId('cases-editor-delete-confirm').click()

    await expect(page).not.toHaveURL(/mine=/)
    expect(saves.map((save) => save.method)).toEqual(['DELETE'])
  })

  test("offers no edit for a search that is not the caller's own", async ({ page }) => {
    await stub(page)

    await page.goto(`/cases?filter=${filter}`)
    await expect(page.getByTestId('cases-list')).toBeVisible()

    await expect(page.getByTestId('cases-edit-search')).toHaveCount(0)
  })

  test('has no accessibility violations with the editor open', async ({ page }) => {
    await stub(page)

    await page.setViewportSize({ width: 1440, height: 1000 })
    await page.goto('/cases')
    await page.getByTestId('cases-new-filter').click()
    await expect(page.getByTestId('where-builder').locator('.rule-container')).toHaveCount(1)
    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })

  test('shows how the evidence moved the likelihood in the peek', async ({ page }) => {
    await stub(page)

    await page.goto('/cases?case=9041')

    await expect(
      page.getByTestId('cases-peek-contributions').getByTestId('server-chart'),
    ).toBeVisible()
  })

  test('opens the queue health canvas from the page and asks for the workflow and period', async ({
    page,
  }) => {
    const { charts } = await stub(page)

    await page.goto('/cases')
    await page.getByTestId('cases-tab-health').click()

    await expect(page).toHaveURL(/tab=health/)
    await expect(page.getByTestId('canvas-ageing')).toBeVisible()
    await expect.poll(() => charts.some((url) => url.pathname.endsWith('/Aging'))).toBe(true)

    const aging = charts.find((url) => url.pathname.endsWith('/Aging'))!

    expect(aging.searchParams.get('caseWorkflowGuid')).toBe(workflow)

    await page.getByTestId('cases-analytics-period').selectOption('90')
    await expect(page).toHaveURL(/days=90/)
    await expect
      .poll(() => charts.filter((url) => url.pathname.endsWith('/Aging')).length)
      .toBeGreaterThan(1)

    await page.getByTestId('cases-tab-queue').click()
    await expect(page).not.toHaveURL(/tab=/)
    await expect(page.getByTestId('cases-queue-panel')).toBeVisible()
  })

  test('says there is nothing to chart rather than failing when a workflow has no data', async ({
    page,
  }) => {
    await stub(page)
    await page.route(/\/api\/CaseAnalytics\/Chart\/Aging/, (route) =>
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify('There is no open case to chart.'),
      }),
    )

    await page.goto('/cases?tab=health')

    await expect(page.getByTestId('server-chart-empty').first()).toHaveText(
      'There is no open case to chart.',
    )
    await expect(page.getByTestId('canvas-ageing').getByRole('alert')).toHaveCount(0)
  })

  test('opens the quality canvas with the rule, ranking and calibration sections', async ({
    page,
  }) => {
    const { charts } = await stub(page)

    await page.goto('/cases/quality')

    await expect(page.getByTestId('canvas-rules')).toBeVisible()
    await expect(page.getByTestId('canvas-ranking')).toBeVisible()
    await expect(page.getByTestId('canvas-calibration')).toBeVisible()
    await expect.poll(() => charts.length).toBeGreaterThan(0)
  })

  test('shows what the randomly picked cases turned out to be on the quality canvas', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/quality')

    await expect(page.getByTestId('cases-qa-picked')).toHaveText('12')
    await expect(page.getByTestId('cases-qa-rate')).toHaveText('30%')
    await expect(page.getByTestId('cases-qa-card')).toContainText('35%')
    await expect(page.getByTestId('cases-qa-few')).toHaveCount(0)
  })

  test('refuses the canvases to a role without the cases permission', async ({ page }) => {
    await stub(page, { permissions: [29] })

    await page.goto('/cases/queue-health')

    await expect(page.getByTestId('cases-refused')).toBeVisible()
  })

  for (const canvas of ['queue-health', 'quality'] as const) {
    test(`has no accessibility violations on the ${canvas} canvas`, async ({ page }) => {
      await stub(page)

      await page.setViewportSize({ width: 1440, height: 1000 })
      await page.goto(`/cases/${canvas}`)
      await expect(page.getByTestId('server-chart').first()).toBeVisible()

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })
  }

  test('draws the risk history on a true time scale and shows the selected change', async ({
    page,
  }) => {
    await stub(page)

    await page.setViewportSize({ width: 1600, height: 1000 })
    await page.goto('/cases?case=9041')

    const markers = page.getByTestId('risk-timeline-marker')

    await expect(markers).toHaveCount(5)
    await expect(page.getByTestId('risk-timeline').getByRole('listitem')).toHaveCount(5)

    const xs = await markers.evaluateAll((nodes) =>
      nodes.map((node) => node.getBoundingClientRect().left),
    )
    const lanes = await markers.evaluateAll((nodes) =>
      nodes.map((node) => Number(node.getAttribute('data-lane'))),
    )

    expect((xs[3]! - xs[1]!) / (xs[4]! - xs[3]!)).toBeGreaterThan((26 / 48) * 0.95)
    expect((xs[3]! - xs[1]!) / (xs[4]! - xs[3]!)).toBeLessThan((26 / 48) * 1.05)
    expect((xs[4]! - xs[3]!) / (xs[3]! - xs[2]!)).toBeGreaterThan(1.9)
    expect((xs[4]! - xs[3]!) / (xs[3]! - xs[2]!)).toBeLessThan(2.1)

    expect(new Set(lanes.slice(0, 3)).size).toBe(3)

    const buttons = page.getByTestId('risk-timeline').getByRole('button')
    const boxes = await buttons.evaluateAll((nodes) =>
      nodes.map((node) => {
        const r = node.getBoundingClientRect()

        return [r.left, r.top, r.right, r.bottom]
      }),
    )

    for (let a = 0; a < boxes.length; a++) {
      for (let b = a + 1; b < boxes.length; b++) {
        const [al, at, ar, ab] = boxes[a]!
        const [bl, bt, br, bb] = boxes[b]!

        expect(al < br && bl < ar && at < bb && bt < ab).toBe(false)
      }
    }

    await buttons.nth(3).focus()
    await expect(page.getByTestId('risk-timeline-detail')).toContainText('Payment hold')
    await expect(page.getByTestId('risk-timeline-detail')).toContainText('Priority 48 to 77')

    await buttons.nth(1).click()
    await expect(page.getByTestId('risk-timeline-detail')).toContainText('New activation')
  })

  test('keeps the risk history timeline inside its own scroller at phone width', async ({
    page,
  }) => {
    await stubSummary(page, { riskHistory: historyOver(800, 5) })

    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/cases?case=9041')

    const scroll = page.getByTestId('risk-timeline-scroll')

    await scroll.scrollIntoViewIfNeeded()
    await expect(scroll).toBeVisible()

    const metrics = await scroll.evaluate((node) => ({
      client: node.clientWidth,
      scroll: node.scrollWidth,
      page: document.documentElement.scrollWidth,
      view: document.documentElement.clientWidth,
    }))

    expect(metrics.scroll).toBeGreaterThan(metrics.client)
    expect(metrics.page).toBeLessThanOrEqual(metrics.view)
  })

  const stayLife: CaseSummary['stays'] = [
    {
      caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222221',
      statusName: 'Triage',
      enterReason: 1,
      enteredDate: '2026-10-20T09:00:00Z',
      enteredUser: 'alex',
      exitedDate: '2026-10-20T10:00:00Z',
      exitReason: 1,
      minutes: 60,
      maxMinutes: 120,
    },
    {
      caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222222',
      statusName: 'Awaiting review',
      enterReason: 2,
      enteredDate: '2026-10-20T10:00:00Z',
      enteredUser: 'sam',
      exitedDate: '2026-10-20T20:00:00Z',
      exitReason: 1,
      minutes: 600,
      maxMinutes: 240,
    },
    {
      caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222221',
      statusName: 'Triage',
      enterReason: 3,
      enteredDate: '2026-10-20T20:00:00Z',
      enteredUser: null,
      exitedDate: '2026-10-21T01:00:00Z',
      exitReason: 1,
      minutes: 300,
      maxMinutes: 600,
    },
    {
      caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222223',
      statusName: 'Escalated review',
      enterReason: 4,
      enteredDate: '2026-10-21T01:00:00Z',
      enteredUser: 'alex',
      exitedDate: '2026-10-21T01:20:00Z',
      exitReason: 1,
      minutes: 20,
      maxMinutes: null,
    },
    {
      caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222224',
      statusName: 'Decision',
      enterReason: 2,
      enteredDate: '2026-10-21T01:20:00Z',
      enteredUser: 'sam',
      exitedDate: null,
      exitReason: null,
      minutes: 300,
      maxMinutes: null,
    },
  ]

  async function stubSummary(page: Page, patch: Partial<CaseSummary>) {
    await stub(page)
    await page.route(/\/api\/CaseSummary\/\d+$/, (route) =>
      route.fulfill({ json: { ...summary, ...patch } }),
    )
  }

  function historyOver(days: number, count: number): CaseSummary['riskHistory'] {
    const start = Date.parse('2026-10-23T12:00:00Z') - days * 86_400_000

    return Array.from({ length: count }, (_, index) => ({
      ...summary.riskHistory[0]!,
      sequence: index + 1,
      createdDate: new Date(start + (days * 86_400_000 * index) / (count - 1)).toISOString(),
    }))
  }

  async function timelineMetrics(page: Page) {
    return page.getByTestId('risk-timeline-scroll').evaluate((node) => ({
      client: node.clientWidth,
      scroll: node.scrollWidth,
      inner: (node.firstElementChild as HTMLElement).getBoundingClientRect().width,
      page: document.documentElement.scrollWidth,
      view: document.documentElement.clientWidth,
    }))
  }

  test('fills the container with a short risk history and does not scroll', async ({ page }) => {
    await stubSummary(page, { riskHistory: historyOver(2 / 24, 3) })

    await page.setViewportSize({ width: 1600, height: 1000 })
    await page.goto('/cases?case=9041')
    await expect(page.getByTestId('risk-timeline-marker')).toHaveCount(3)

    await expect
      .poll(async () => {
        const m = await timelineMetrics(page)

        return Math.abs(m.inner - m.client) <= 3 && m.client > 0
      })
      .toBe(true)

    const m = await timelineMetrics(page)

    expect(m.scroll).toBeLessThanOrEqual(m.client + 1)
    expect(m.page).toBeLessThanOrEqual(m.view)
  })

  test('refits the risk history when the viewport is resized', async ({ page }) => {
    await stubSummary(page, { riskHistory: historyOver(2 / 24, 3) })

    await page.setViewportSize({ width: 1600, height: 1000 })
    await page.goto('/cases?case=9041')
    await expect(page.getByTestId('risk-timeline-marker')).toHaveCount(3)
    await expect
      .poll(async () => {
        const m = await timelineMetrics(page)

        return Math.abs(m.inner - m.client) <= 3
      })
      .toBe(true)

    const before = await timelineMetrics(page)

    await page.setViewportSize({ width: 800, height: 1000 })

    await expect
      .poll(async () => {
        const m = await timelineMetrics(page)

        return m.client !== before.client && Math.abs(m.inner - m.client) <= 3
      })
      .toBe(true)
  })

  test('scrolls a long risk history inside its scroller and the page does not scroll', async ({
    page,
  }) => {
    await stubSummary(page, { riskHistory: historyOver(800, 5) })

    await page.setViewportSize({ width: 1600, height: 1000 })
    await page.goto('/cases?case=9041')
    await expect(page.getByTestId('risk-timeline-marker')).toHaveCount(5)

    const m = await timelineMetrics(page)

    expect(m.scroll).toBeGreaterThan(m.client)
    expect(m.page).toBeLessThanOrEqual(m.view)

    const scrolled = await page
      .getByTestId('risk-timeline-scroll')
      .evaluate((node) => Math.abs(node.scrollLeft))

    expect(scrolled).toBeGreaterThan(0)
  })

  test('draws the time in each status as a proportional bar in time order', async ({ page }) => {
    await stubSummary(page, { stays: stayLife })

    await page.setViewportSize({ width: 1600, height: 1000 })
    await page.goto('/cases?case=9041')

    const stays = page.getByTestId('cases-peek-stays')
    const segments = stays.getByTestId('stay-segment')

    await expect(stays).toBeVisible()
    await expect(segments).toHaveCount(5)
    await expect(stays.getByTestId('stay-row')).toHaveCount(5)
    await expect(stays.getByRole('list')).toHaveCount(2)

    const widths = await segments.evaluateAll((nodes) =>
      nodes.map((node) => node.getBoundingClientRect().width),
    )

    expect(widths[1]! / widths[2]!).toBeGreaterThan(1.9)
    expect(widths[1]! / widths[2]!).toBeLessThan(2.1)
    expect(widths[2]! / widths[4]!).toBeGreaterThan(0.95)
    expect(widths[2]! / widths[4]!).toBeLessThan(1.05)
    expect(widths[3]!).toBeGreaterThanOrEqual(16)

    const names = await stays.getByTestId('stay-row').locator('span.font-medium').allTextContents()

    expect(names.map((name) => name.trim())).toEqual([
      'Triage',
      'Awaiting review',
      'Triage',
      'Escalated review',
      'Decision',
    ])

    await expect(segments.nth(4)).toHaveAttribute('data-current', 'true')
    await expect(segments.nth(0)).toHaveAttribute('data-current', 'false')
    await expect(stays.getByTestId('stay-row').nth(4)).toContainText('Current')
    await expect(stays.getByTestId('stay-row').nth(0)).toContainText('1 h')
    await expect(stays.getByTestId('stay-row').nth(0)).toContainText('over 2 visits')
    await expect(stays.getByTestId('stay-row').nth(0)).toContainText('alex')
    await expect(stays.getByTestId('stay-row').nth(1).locator('progress')).toBeVisible()
    await expect(stays.getByTestId('stay-row').nth(3).locator('progress')).toHaveCount(0)

    await segments.nth(2).getByRole('button').focus()
    await expect(stays.getByTestId('stay-row').nth(2)).toHaveClass(/ring-primary/)
  })

  test('shows a status breach with the words over limit', async ({ page }) => {
    await stubSummary(page, { stays: stayLife })

    await page.setViewportSize({ width: 1600, height: 1000 })
    await page.goto('/cases?case=9041')

    const stays = page.getByTestId('cases-peek-stays')
    const segments = stays.getByTestId('stay-segment')

    await expect(segments.nth(1)).toHaveAttribute('data-breach', 'true')
    await expect(segments.nth(0)).toHaveAttribute('data-breach', 'false')
    await expect(stays.getByTestId('stay-row').nth(1)).toContainText('6 h over limit')
    await expect(stays.getByTestId('stay-row').nth(0)).not.toContainText('over limit')
    await expect(segments.nth(1).getByRole('button')).toHaveAttribute('aria-label', /over limit/)
  })

  test('shows no time in each status block without stays', async ({ page }) => {
    await stubSummary(page, { stays: [] })

    await page.goto('/cases?case=9041')
    await expect(page.getByTestId('case-summary-body').first()).toBeVisible()
    await expect(page.getByTestId('cases-peek-stays')).toHaveCount(0)
  })

  test('keeps the time in each status inside the page at phone width', async ({ page }) => {
    await stubSummary(page, { stays: stayLife })

    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/cases?case=9041')

    const stays = page.getByTestId('cases-peek-stays')

    await stays.scrollIntoViewIfNeeded()
    await expect(stays).toBeVisible()
    await expect(stays.getByTestId('stay-segment')).toHaveCount(5)

    const metrics = await page.evaluate(() => ({
      page: document.documentElement.scrollWidth,
      view: document.documentElement.clientWidth,
    }))

    expect(metrics.page).toBeLessThanOrEqual(metrics.view)
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme with the status bar`, async ({
      page,
    }) => {
      await stubSummary(page, { stays: stayLife })

      await page.setViewportSize({ width: 1600, height: 1000 })
      await page.goto('/cases?case=9041')
      await expect(page.getByTestId('cases-peek-stays')).toBeVisible()
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="cases-peek-stays"]')
        .analyze()

      expect(results.violations).toEqual([])
    })
  }

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme with the risk timeline`, async ({
      page,
    }) => {
      await stub(page)

      await page.setViewportSize({ width: 1600, height: 1000 })
      await page.goto('/cases?case=9041')
      await expect(page.getByTestId('risk-timeline')).toBeVisible()
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="risk-timeline"]')
        .analyze()

      expect(results.violations).toEqual([])
    })
  }

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme with the peek open`, async ({
      page,
    }) => {
      await stub(page)

      await page.setViewportSize({ width: 1600, height: 1000 })
      await page.goto('/cases?case=9041')
      await expect(page.getByTestId('cases-peek-stays')).toBeVisible()
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await settleAnimations(page)

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })
  }
})

const supervision = {
  caseWorkflowGuid: workflow,
  from: '2026-09-23T00:00:00Z',
  to: '2026-10-23T00:00:00Z',
  unassigned: 5,
  held: 3,
  analysts: [
    {
      user: 'alex',
      holding: 2,
      oldestHoldMinutes: 600,
      taken: 14,
      peeks: 9,
      closed: 6,
      medianResolutionMinutes: 1440,
    },
    {
      user: 'sam',
      holding: 1,
      oldestHoldMinutes: 30,
      taken: 3,
      peeks: 0,
      closed: 0,
      medianResolutionMinutes: null,
    },
  ],
}

const stuckLocks = [
  {
    caseId: 9041,
    caseKey: 'AccountId',
    caseKeyValue: 'ACC-441',
    statusName: 'Awaiting review',
    lockedUser: 'sam',
    lockedDate: '2026-10-22T08:00:00Z',
    lastActivityDate: '2026-10-22T08:00:00Z',
    idleMinutes: 1500,
    priority: 62,
    lane: 4,
  },
]

async function stubFormatting(page: Page) {
  await page.route(/\/api\/CaseKeyJournal(\?|$)/, (route) => {
    const body = journalRows('payload')

    body.rows[1] = {
      ...body.rows[1]!,
      BoldLine: {
        BoldLineKey: 'Counterparty',
        BoldLineValue: 'Globex',
        BoldLineFormatForeColor: '#111111',
        BoldLineFormatBackColor: '#ffe9a8',
      },
      CellFormat: [
        {
          CellFormatKey: 'Amount',
          CellFormatPattern: '^4',
          CellFormatForeColor: '#7a0000',
          CellFormatBackColor: '#ffd6d6',
          CellFormatForeRow: false,
          CellFormatBackRow: false,
        },
      ],
    }

    return route.fulfill({ json: body })
  })
}

async function stubLocation(page: Page, calls?: URLSearchParams[]) {
  await page.route(/\/api\/CaseKeyJournal(\?|$)/, (route) => {
    const params = new URL(route.request().url()).searchParams

    calls?.push(params)

    const body = journalRows('payload')

    body.meta = {
      ...body.meta,
      rowCount: params.has('filter') ? 1 : 2,
      location: { latitude: 'Latitude', longitude: 'Longitude' },
    }
    body.columns = [
      ...body.columns,
      {
        name: 'Latitude',
        title: 'Latitude',
        type: 'number',
        roleId: 6,
        roleInferred: false,
        lens: 'payload',
        group: 'payload',
        drill: false,
      },
      {
        name: 'Longitude',
        title: 'Longitude',
        type: 'number',
        roleId: 6,
        roleInferred: false,
        lens: 'payload',
        group: 'payload',
        drill: false,
      },
    ]
    body.rows = body.rows.map((row, index) => ({
      ...row,
      Latitude: index === 0 ? 51.5 : 40.7,
      Longitude: index === 0 ? -0.12 : -74,
    }))

    return route.fulfill({ json: body })
  })

  const trackCalls: URLSearchParams[] = []
  const replays: Record<string, unknown>[] = []
  const posts: Record<string, unknown>[] = []
  const deletes: string[] = []

  await page.route(/\/api\/CaseKeyJournal\/Track/, (route) => {
    const params = new URL(route.request().url()).searchParams

    trackCalls.push(params)

    const points = [
      {
        Id: 1,
        EntityAnalysisModelInstanceEntryGuid: 'tx-1',
        ReferenceDate: '2001-08-01T09:00:00Z',
        ResponseElevation: 2,
        Activations: '',
        Pinned: false,
        Latitude: 51.5,
        Longitude: -0.12,
      },
      {
        Id: 2,
        EntityAnalysisModelInstanceEntryGuid: 'tx-2',
        ReferenceDate: '2001-08-01T11:00:00Z',
        ResponseElevation: 9,
        Activations: 'Velocity',
        Pinned: false,
        Latitude: 40.7,
        Longitude: -74,
      },
    ]
    const filtered = params.get('filter') !== null
    const everything = filtered && params.get('includeUnmatched') === 'true'
    const shown = filtered ? points.slice(1) : points
    const drawn = everything
      ? points.map((point, index) => ({ ...point, Matched: index > 0 }))
      : shown

    return route.fulfill({
      json: {
        meta: {
          key: 'AccountId',
          value: 'ACC-441',
          from: '2026-09-26T00:00:00Z',
          to: '2026-10-26T00:00:00Z',
          total: 2,
          matched: shown.length,
          located: shown.length,
          unlocated: 0,
          limit: 20000,
          truncated: false,
          location: { latitude: 'Latitude', longitude: 'Longitude' },
        },
        summary: {
          legs: shown.length - 1,
          impossible: filtered ? 0 : 1,
          limitKmh: 900,
          minimumKilometres: 50,
          worst: filtered
            ? []
            : [
                {
                  lane: 'A',
                  fromTransaction: 'tx-1',
                  toTransaction: 'tx-2',
                  fromDate: '2026-10-19T09:00:00Z',
                  toDate: '2026-10-19T11:00:00Z',
                  kilometres: 5570.4,
                  hours: 2,
                  speedKmh: 2785.2,
                  impossible: true,
                },
              ],
        },
        points: drawn,
      },
    })
  })

  await page.route(/\/api\/CaseKeyJournal\/Replays/, (route) => {
    const request = route.request()
    const url = new URL(request.url())

    if (request.method() === 'POST') {
      const body = request.postDataJSON() as Record<string, unknown>
      const created = {
        guid: `replay-${replays.length + 1}`,
        caseId: 9041,
        createdUser: 'me',
        createdDate: '2026-10-26T10:00:00Z',
        merge: null,
        filter: null,
        ...body,
      }

      posts.push(body)
      replays.unshift(created)

      return route.fulfill({ json: created })
    }

    if (request.method() === 'DELETE') {
      const guid = url.pathname.split('/').at(-1) ?? ''

      deletes.push(guid)
      replays.splice(
        replays.findIndex((item) => item.guid === guid),
        1,
      )

      return route.fulfill({ status: 204 })
    }

    return route.fulfill({ json: { replays } })
  })

  return { trackCalls, replays, posts, deletes }
}

async function stubSupervision(page: Page, options: { permissions?: number[] } = {}) {
  const posts: { path: string; body: unknown }[] = []
  const idles: string[] = []

  await stub(page, { permissions: options.permissions ?? [1, 18] })
  await page.route('**/api/CaseSupervision?**', (route) => route.fulfill({ json: supervision }))
  await page.route('**/api/CaseSupervision/StuckLocks**', (route) => {
    idles.push(new URL(route.request().url()).searchParams.get('idleMinutes') ?? '')

    return route.fulfill({ json: stuckLocks })
  })
  await page.route('**/api/CaseSupervision/BreakLock', (route) => {
    posts.push({ path: 'BreakLock', body: route.request().postDataJSON() })

    return route.fulfill({ status: 204 })
  })
  await page.route('**/api/CaseSupervision/Reassign', (route) => {
    posts.push({ path: 'Reassign', body: route.request().postDataJSON() })

    return route.fulfill({ status: 204 })
  })

  return { posts, idles }
}

test.describe('Case diary', () => {
  test.use({ viewport: { width: 1700, height: 1000 } })

  async function stubDiary(page: Page, diary: Record<string, unknown> = {}) {
    const posts: { path: string; body: unknown }[] = []

    await stub(page)
    await page.route(/\/api\/CaseSummary\/\d+$/, (route) =>
      route.fulfill({ json: { ...summary, case: { ...rows[0]!, ...diary } } }),
    )
    await page.route('**/api/CaseDiary/Set', (route) => {
      posts.push({ path: 'Set', body: route.request().postDataJSON() })

      return route.fulfill({ status: 204 })
    })
    await page.route('**/api/CaseDiary/Clear', (route) => {
      posts.push({ path: 'Clear', body: route.request().postDataJSON() })

      return route.fulfill({ status: 204 })
    })

    return posts
  }

  test('sets a regulatory deadline from the peek, reading the picker as UTC', async ({ page }) => {
    const posts = await stubDiary(page)

    await page.goto('/cases?case=9041')
    await expect(page.getByTestId('cases-peek-diary')).toBeVisible()
    await expect(page.getByTestId('cases-diary-save')).toBeDisabled()
    await expect(page.getByTestId('cases-diary-clear')).toHaveCount(0)

    await page.getByTestId('cases-diary-type').selectOption('3')
    await page.getByTestId('cases-diary-due').fill('2026-10-30T09:30')
    await page.getByTestId('cases-diary-save').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toEqual({
      path: 'Set',
      body: { caseId: 9041, diaryTypeId: 3, dueDate: '2026-10-30T09:30:00.000Z' },
    })
  })

  test('shows the diary a case already has and clears it', async ({ page }) => {
    const posts = await stubDiary(page, {
      diary: true,
      diaryTypeId: 4,
      diaryType: 'Payment hold',
      diaryDate: '2026-10-30T09:30:00Z',
    })

    await page.goto('/cases?case=9041')

    await expect(page.getByTestId('cases-diary-current')).toContainText('Payment hold')
    await expect(page.getByTestId('cases-diary-current')).toContainText('30 Oct 2026')
    await expect(page.getByTestId('cases-diary-type')).toHaveValue('4')
    await expect(page.getByTestId('cases-diary-due')).toHaveValue('2026-10-30T09:30')
    await expect(page.getByTestId('cases-diary-save')).toHaveText('Update diary')

    await page.getByTestId('cases-diary-clear').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toEqual({ path: 'Clear', body: { caseId: 9041 } })
  })

  test('offers no form on a case that is not open', async ({ page }) => {
    await stubDiary(page, { closedStatusId: 3 })

    await page.goto('/cases?case=9041')

    await expect(page.getByTestId('cases-peek-diary')).toContainText('Only an open case')
    await expect(page.getByTestId('cases-diary-form')).toHaveCount(0)
  })

  test('has no accessibility violations with the diary form open', async ({ page }) => {
    await stubDiary(page)

    await page.goto('/cases?case=9041')
    await expect(page.getByTestId('cases-diary-form')).toBeVisible()

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})

test.describe('Case supervision', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('is offered to a supervisor and hidden from everyone else', async ({ page }) => {
    await stubSupervision(page)
    await page.goto('/cases')
    await expect(page.getByTestId('cases-tab-supervision')).toBeVisible()

    const other = await page.context().newPage()

    await stubSupervision(other, { permissions: [1] })
    await other.goto('/cases')
    await expect(other.getByTestId('cases-list')).toBeVisible()
    await expect(other.getByTestId('cases-tab-supervision')).toHaveCount(0)
  })

  test('shows the workload of each analyst and the locks that have gone stale', async ({
    page,
  }) => {
    const { idles } = await stubSupervision(page)

    await page.goto('/cases/supervision')

    await expect(page.getByTestId('cases-supervision-headline')).toContainText('5')
    await expect(page.getByTestId('cases-supervision-workload')).toContainText('alex')
    await expect(page.getByTestId('cases-supervision-workload')).toContainText('sam')
    await expect(page.getByTestId('cases-supervision-stuck')).toContainText('C-9041')
    await expect(page.getByTestId('cases-supervision-stuck')).toContainText('sam')
    expect(idles[0]).toBe('120')

    await page.getByTestId('cases-supervision-idle').selectOption('480')
    await expect(page).toHaveURL(/idle=480/)
    await expect.poll(() => idles.includes('480')).toBe(true)
  })

  test('breaks a lock with a reason after the supervisor confirms', async ({ page }) => {
    const { posts } = await stubSupervision(page)

    await page.goto('/cases/supervision')
    await page.getByRole('button', { name: 'Break lock' }).first().click()
    await expect(page.getByTestId('cases-supervision-dialog')).toBeVisible()

    await page.getByTestId('cases-supervision-reason').fill('Analyst on leave')
    await page.getByTestId('cases-supervision-confirm').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toEqual({
      path: 'BreakLock',
      body: { caseId: 9041, reason: 'Analyst on leave' },
    })
    await expect(page.getByTestId('cases-supervision-dialog')).toBeHidden()
  })

  test('reassigns a case to another analyst and never offers the current holder', async ({
    page,
  }) => {
    const { posts } = await stubSupervision(page)

    await page.goto('/cases/supervision')
    await page.getByRole('button', { name: 'Reassign' }).first().click()

    const choices = page.getByTestId('cases-supervision-analyst').locator('option')

    await expect(choices).toHaveText(['Choose an analyst', 'alex'])
    await expect(page.getByTestId('cases-supervision-confirm')).toBeDisabled()

    await page.getByTestId('cases-supervision-analyst').selectOption('alex')
    await page.getByTestId('cases-supervision-confirm').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toEqual({ path: 'Reassign', body: { caseId: 9041, toUser: 'alex' } })
  })

  test('refuses a role without the supervision permission', async ({ page }) => {
    await stubSupervision(page, { permissions: [1] })

    await page.goto('/cases/supervision')

    await expect(page.getByTestId('cases-tab-supervision')).toHaveCount(0)
    await expect(page.getByTestId('cases-queue-panel')).toBeVisible()
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme with the dialog open`, async ({
      page,
    }) => {
      await stubSupervision(page)

      await page.goto('/cases/supervision')
      await expect(page.getByTestId('cases-supervision-stuck')).toContainText('C-9041')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)
      await page.getByRole('button', { name: 'Reassign' }).first().click()
      await expect(page.getByTestId('cases-supervision-analyst')).toBeVisible()
      await expect
        .poll(() =>
          page.evaluate(
            () =>
              document
                .getAnimations()
                .filter(
                  (animation) =>
                    animation.playState === 'running' &&
                    animation.effect?.getComputedTiming().iterations !== Infinity &&
                    (animation.effect as KeyframeEffect).target?.closest('dialog') !== null,
                ).length,
          ),
        )
        .toBe(0)

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })
  }
})

test.describe('Case page', () => {
  test.use({ viewport: { width: 1400, height: 1000 } })

  test('opens from the queue with the key, status, lock and risk in the header', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases')
    await expect(page.getByTestId('cases-list')).toBeVisible()
    await page.getByRole('button', { name: 'Open case' }).first().click()

    await expect(page).toHaveURL(/\/cases\/9041/)
    await expect(page.getByTestId('case-title')).toHaveText('C-9041')
    await expect(page.getByTestId('case-status')).toHaveText('Awaiting review')
    await expect(page.getByTestId('case-lock')).toContainText('Held by alex')
    await expect(page.getByTestId('case-key-value')).toHaveText('ACC-441')
  })

  test('tells the case as one story: why it is here, then the payload fields, then what fired', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-lede-ranked')).toContainText('Standard lane')
    await expect(page.getByTestId('case-lede-similar')).toContainText('1 of 3 earlier cases')
    await expect(page.getByTestId('case-lede-stake')).toContainText('45,000')
    await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
    await expect(page.getByTestId('case-activations')).toContainText('Structuring 9d')
    await expect(page.getByTestId('case-decide')).toBeVisible()
  })

  test('jumps to a section from the index and keeps it in the address', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')
    await page.getByRole('button', { name: /Notes/ }).first().click()

    await expect(page).toHaveURL(/section=notes/)
    await expect(page.getByTestId('case-notes')).toBeVisible()
  })

  test('shows the journal with the strip, what stands out and the grid, asking for the case key', async ({
    page,
  }) => {
    const { journalCalls, analyticsCalls } = await stub(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect(journal).toContainText('Globex')
    await expect(page.getByTestId('journal-strip-count')).toHaveText('2')
    await expect(page.getByTestId('journal-notable')).toContainText('Amount was 45,000')
    await expect(page.getByTestId('journal-strip-few')).toBeVisible()
    await expect(journal).toContainText('Stands out')
    expect(journalCalls[0]?.get('key')).toBe('AccountId')
    expect(journalCalls[0]?.get('value')).toBe('ACC-441')
    expect(journalCalls[0]?.get('lens')).toBe('payload')
    expect(analyticsCalls[0]?.get('sections')).toContain('notable')
  })

  test('tags the selected transactions together and undoes it', async ({ page }) => {
    const { tagPuts } = await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')
    await expect(page.getByTestId('journal-tag-open')).toHaveCount(0)
    await expect(page.getByTestId('journal-selected')).toHaveCount(0)

    await page.getByTestId('journal-select-all').click()
    await expect(page.getByTestId('journal-selected')).toHaveText('2 selected')
    await expect(page.getByTestId('journal-select').first()).toBeChecked()

    await page.getByTestId('journal-tag-open').click()
    await expect(page.getByTestId('journal-tag-dialog')).toBeVisible()
    await expect(page.getByTestId('journal-tag-apply')).toBeDisabled()
    await expect(page.getByTestId('journal-tag-remove')).toHaveCount(1)

    await page.getByTestId('journal-tag-add').nth(1).check()
    await page.getByTestId('journal-tag-remove').check()
    await page.getByTestId('journal-tag-apply').click()

    await expect(page.getByTestId('journal-tag-undo')).toContainText('Tagged 2 transactions.')
    await expect(page.getByTestId('journal-tag-undo')).toContainText('1 were not changed')
    await expect(page.getByTestId('journal-tag-undo')).toContainText('2 were left alone')
    expect(tagPuts[0]?.path).toBe('/api/CaseKeyJournal/Tags')
    expect(tagPuts[0]?.body).toMatchObject({
      transactions: ['tx-1', 'tx-2'],
      add: ['review'],
      remove: ['mule'],
    })

    await page.getByTestId('journal-tag-undo-run').click()
    await expect(page.getByTestId('journal-tag-undo')).toHaveCount(0)
    expect(tagPuts[1]?.path).toBe('/api/CaseKeyJournal/Tags/Restore')
    expect(tagPuts[1]?.body.sets).toEqual([
      { transaction: 'tx-1', tag: ['mule'] },
      { transaction: 'tx-2', tag: [] },
    ])
  })

  test('selects single rows and clears the selection', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')

    await page.getByTestId('journal-select').nth(1).check()
    await expect(page.getByTestId('journal-selected')).toHaveText('1 selected')

    await page.getByTestId('journal-select-clear').click()
    await expect(page.getByTestId('journal-selected')).toHaveCount(0)
    await expect(page.getByTestId('journal-tag-open')).toHaveCount(0)
    await expect(page.getByTestId('journal-select').nth(1)).not.toBeChecked()
  })

  test('switches lens, window and activations-only through the address', async ({ page }) => {
    const { journalCalls } = await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')

    await page.getByTestId('journal-lens-rules').click()
    await expect(page).toHaveURL(/jl=rules/)
    await expect(page.getByTestId('case-journal')).toContainText('Velocity')
    await expect.poll(() => journalCalls.some((call) => call.get('lens') === 'rules')).toBe(true)

    await page.getByTestId('journal-window').selectOption('365')
    await page.getByTestId('journal-activations-only').check()
    await expect(page).toHaveURL(/jw=365/)
    await expect(page).toHaveURL(/ja=1/)
    await expect
      .poll(() => journalCalls.some((call) => call.get('activationsOnly') === 'true'))
      .toBe(true)
  })

  test('opens a transaction from the grid and shows each model with its decision journey', async ({
    page,
  }) => {
    const { transactionCharts } = await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')
    await page.getByRole('button', { name: 'Open' }).last().click()

    const drawer = page.getByTestId('journal-drawer')

    await expect(page).toHaveURL(/jt=2/)
    await expect(drawer).toContainText('Transaction 2')
    await expect(page.getByTestId('journal-adaptation-StructNet')).toContainText('Very likely')
    await expect(page.getByTestId('journal-adaptation-StructNet')).toContainText(
      'Large cash in, then out.',
    )
    await expect(page.getByTestId('journal-adaptation-StructNet')).toContainText('Activated')
    await expect(page.getByTestId('journal-journey-node').first()).toHaveText('Amount > 5,000')
    await expect(page.getByTestId('journal-journey-node').last()).toHaveText(
      'An input you cannot see',
    )
    await expect(drawer).toContainText('Hidden inputs: 1')
    await expect
      .poll(() => transactionCharts.some((url) => url.pathname.endsWith('/Contribution')))
      .toBe(true)
    expect(transactionCharts[0]?.searchParams.get('adaptation')).toBe('StructNet')
    expect(transactionCharts[0]?.searchParams.get('source')).toBe('exhaustive')

    await page.getByTestId('journal-drawer-close').click()
    await expect(drawer).toHaveCount(0)
    await expect(page).not.toHaveURL(/jt=/)
  })

  test('opens a transaction from what stands out', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('journal-notable-2').click()

    await expect(page.getByTestId('journal-drawer')).toBeVisible()
    await expect(page).toHaveURL(/jt=2/)
  })

  test('reads the payload only when asked and says the read is recorded', async ({ page }) => {
    const { payloadCalls } = await stub(page)

    await page.goto('/cases/9041?jt=2')
    await expect(page.getByTestId('journal-drawer')).toContainText(
      'Reading the payload is recorded.',
    )
    expect(payloadCalls).toHaveLength(0)

    await page.getByTestId('journal-payload-load').click()

    await expect(page.getByTestId('journal-payload-fields')).toContainText('D-9')
    expect(payloadCalls).toHaveLength(1)
    expect(payloadCalls[0]?.get('id')).toBe('2')
  })

  test('pivots to an identifier from a transaction and comes back to the case key', async ({
    page,
  }) => {
    const { journalCalls } = await stub(page)

    await page.goto('/cases/9041?jt=2')
    await expect(page.getByTestId('journal-pivot-Account')).toHaveCount(0)
    await page.getByTestId('journal-pivot-Device').click()

    await expect(page).toHaveURL(/jk=Device/)
    await expect(page).toHaveURL(/jv=D-9/)
    await expect(page.getByTestId('journal-pivot')).toContainText('Showing Device D-9')
    await expect.poll(() => journalCalls.some((call) => call.get('key') === 'Device')).toBe(true)
    expect(journalCalls.find((call) => call.get('key') === 'Device')?.get('value')).toBe('D-9')

    await page.getByTestId('journal-pivot-back').click()

    await expect(page).not.toHaveURL(/jk=/)
    await expect(page.getByTestId('journal-pivot')).toHaveCount(0)
  })

  test('compares the key with its peers and says when peers are not built', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')

    const peers = page.getByTestId('journal-peers')

    await expect(peers).toContainText('Against other keys with Tier Gold.')
    await expect(peers).toContainText('Built 20 Oct 2026 from 90 days.')
    await expect(page.getByTestId('journal-peer-Amount')).toContainText('Above peers')
    await expect(page.getByTestId('journal-peer-Amount')).toContainText('At the 99th percentile')
    await expect(page.getByTestId('journal-peer-Amount')).toContainText('across 412 keys.')
    await expect(page.getByTestId('journal-peer-Amount')).toContainText('Few transactions')
  })

  test('tells the analyst peer comparison is not ready when no peers exist', async ({ page }) => {
    await stub(page, { peersNotComputed: true })

    await page.goto('/cases/9041')

    await expect(page.getByTestId('journal-peers-empty')).toContainText(
      'once the peer distributions',
    )
  })

  test('closes and reopens the left and right panes so the story can use the width', async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await stub(page)

    await page.goto('/cases/9041')

    const story = page.getByTestId('case-story')

    await expect(story).toBeVisible()
    await expect(page.getByTestId('case-left-pane')).toBeVisible()
    await expect(page.getByTestId('case-right-pane')).toBeVisible()

    const both = (await story.boundingBox())!.width

    await page.getByTestId('pane-toggle-left').click()
    await expect(page.getByTestId('case-left-pane')).toBeHidden()
    await expect(page.getByTestId('pane-toggle-left')).toHaveAttribute('aria-expanded', 'false')
    await expect(page.getByTestId('pane-toggle-left')).toContainText('Show the key panel')

    const rightOnly = (await story.boundingBox())!.width

    expect(rightOnly).toBeGreaterThan(both)

    await page.getByTestId('pane-toggle-right').click()
    await expect(page.getByTestId('case-right-pane')).toBeHidden()

    const neither = (await story.boundingBox())!.width

    expect(neither).toBeGreaterThan(rightOnly)

    await page.reload()
    await expect(page.getByTestId('case-story')).toBeVisible()
    await expect(page.getByTestId('case-left-pane')).toBeHidden()
    await expect(page.getByTestId('case-right-pane')).toBeHidden()

    await page.getByTestId('pane-toggle-left').click()
    await page.getByTestId('pane-toggle-right').click()
    await expect(page.getByTestId('case-left-pane')).toBeVisible()
    await expect(page.getByTestId('case-right-pane')).toBeVisible()
    expect((await page.getByTestId('case-story').boundingBox())!.width).toBeCloseTo(both, 0)
  })

  test('tags every matching transaction in the background, shows progress and undoes it', async ({
    page,
  }) => {
    const { jobCalls } = await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')

    await page.getByTestId('journal-tag-all-open').click()
    await expect(page.getByTestId('journal-tag-all-dialog')).toBeVisible()
    await expect(page.getByTestId('journal-tag-all-count')).toHaveText(
      '2,340 transactions match this view.',
    )
    await expect(page.getByTestId('journal-tag-all-apply')).toBeDisabled()

    await page.getByTestId('journal-tag-all-add').nth(1).check()
    await expect(page.getByTestId('journal-tag-all-apply')).toHaveText('Tag 2,340 transactions')
    await page.getByTestId('journal-tag-all-apply').click()

    const headline = page.getByTestId('journal-job-headline')

    await expect(headline).toHaveText('Waiting for the jobs server to pick up the tagging.')
    await expect(headline).toHaveText('Tagging 1,200 of 2,340…')
    await expect(page.getByTestId('journal-job-cancel')).toBeVisible()
    await expect(headline).toHaveText('Tagged 2,300 of 2,340 transactions.')
    await expect(page.getByTestId('journal-job-notes')).toContainText('3 are no longer')
    await expect(page.getByTestId('journal-job-notes')).toContainText('12 were left alone')
    await expect(page.getByTestId('journal-job-progress')).toHaveCount(0)

    const submit = jobCalls.find((call) => call.method === 'POST' && call.path === '/')

    expect(submit?.body).toMatchObject({
      caseWorkflowGuid: workflow,
      key: 'AccountId',
      value: 'ACC-441',
      add: ['review'],
      remove: [],
    })
    expect(jobCalls.find((call) => call.path === '/Preview')?.body).toMatchObject({
      key: 'AccountId',
      value: 'ACC-441',
    })

    await page.getByTestId('journal-job-undo').click()
    await expect(headline).toHaveText('Restoring 100 of 2,340…')
    expect(jobCalls.some((call) => call.path === '/job-1/Undo' && call.method === 'POST')).toBe(
      true,
    )
  })

  test('dismisses a finished tagging job', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')
    await page.getByTestId('journal-tag-all-open').click()
    await page.getByTestId('journal-tag-all-add').first().check()
    await page.getByTestId('journal-tag-all-apply').click()
    await expect(page.getByTestId('journal-job-headline')).toHaveText(
      'Tagged 2,300 of 2,340 transactions.',
      {
        timeout: 10_000,
      },
    )

    await page.getByTestId('journal-job-dismiss').click()
    await expect(page.getByTestId('journal-job')).toHaveCount(0)
  })

  test('pins a transaction for the whole case and shows only the pinned ones', async ({ page }) => {
    const { pinPuts, journalCalls } = await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-journal')).toContainText('Globex')
    await expect(page.getByTestId('journal-pinned-only')).not.toBeChecked()

    await page.getByTestId('journal-pin').nth(1).click()

    await expect(page.getByTestId('journal-pin').nth(1)).toHaveAttribute('aria-pressed', 'true')
    expect(pinPuts[0]).toEqual({ caseId: 9041, transactions: ['tx-2'], pinned: true })
    await expect(page.getByTestId('case-journal')).toContainText('Pinned only (1)')
    expect(journalCalls.at(-1)?.get('caseId')).toBe('9041')

    await page.getByTestId('journal-pinned-only').check()

    await expect(page).toHaveURL(/jp=1/)
    await expect(page.getByTestId('journal-pin')).toHaveCount(1)
    expect(journalCalls.at(-1)?.get('pinnedOnly')).toBe('true')

    await page.getByTestId('journal-pin').click()
    await expect(page.getByTestId('journal-pin')).toHaveCount(0)
    expect(pinPuts[1]).toEqual({ caseId: 9041, transactions: ['tx-2'], pinned: false })
  })

  test('merges another key onto the timeline as a lettered lane and removes it', async ({
    page,
  }) => {
    const { journalCalls } = await stub(page)

    await page.goto('/cases/9041?jt=2')
    await page.getByTestId('journal-merge-Device').click()

    await expect(page).toHaveURL(/jm=/)
    await expect(page.getByTestId('journal-merge-Device')).toBeDisabled()
    await expect(page.getByTestId('journal-lane')).toHaveCount(2)
    await expect(page.getByTestId('journal-lanes')).toContainText('D-9')
    await expect
      .poll(() => journalCalls.some((call) => call.get('merge')?.includes('Device')))
      .toBe(true)
    expect(JSON.parse(journalCalls.at(-1)?.get('merge') ?? '[]')).toEqual([
      { key: 'Device', value: 'D-9' },
    ])

    await page.getByTestId('journal-drawer-close').click()
    await expect(page.getByTestId('case-journal')).toContainText('AB')

    await page.getByTestId('journal-lane-remove').click()
    await expect(page).not.toHaveURL(/jm=/)
    await expect(page.getByTestId('journal-lane')).toHaveCount(1)
  })

  test('shows the same day around a transaction and opens one of them', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041?jt=2')
    await page.getByTestId('journal-around-load').click()

    await expect(page.getByTestId('journal-around-group')).toContainText('D-9')
    await expect(page.getByTestId('journal-around-group')).toContainText('Showing the newest 1')

    await page.getByTestId('journal-around-open').click()
    await expect(page).toHaveURL(/jt=1/)
  })

  test('says so when the journal cannot be loaded', async ({ page }) => {
    await stub(page)
    await page.route(/\/api\/CaseKeyJournal(\?|$)/, (route) =>
      route.fulfill({ status: 500, json: {} }),
    )

    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-journal').getByRole('alert')).toContainText(
      'The journal could not be loaded.',
    )
  })

  test('shows notes as plain text and never runs markup inside them', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041?section=notes')

    await expect(page.getByTestId('case-note-text')).toHaveText('Customer confirmed the transfer')
    expect(await page.evaluate(() => (window as { hacked?: boolean }).hacked)).toBeUndefined()
  })

  test('lists other cases and events on the key', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041?section=events')

    await expect(page.getByTestId('case-history-cases')).toContainText('C-9040')
    await expect(page.getByTestId('case-history-events')).toContainText('Status change')
  })

  test('holds places for the network and the forms', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-network')).toBeVisible()

    await page.goto('/cases/9041?section=forms')
    await expect(page.getByTestId('case-forms')).toBeVisible()
  })

  test('opens a case form from its own button in a lightbox, showing and hiding fields as the answers change', async ({
    page,
  }) => {
    const { formsPosted } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-form-7').click()

    const form = page.getByTestId('case-form')

    await expect(form.getByLabel('Reason')).toBeVisible()
    await expect(form.getByLabel('Detail')).toHaveCount(0)

    await form.getByLabel('Reason').selectOption('other')
    await expect(form.getByLabel('Detail')).toBeVisible()

    await form.getByLabel('Detail').fill('Odd pattern')
    await form.getByLabel('Amount').fill('999')
    await page.getByTestId('case-form-submit').click()

    await expect(form.getByRole('alert')).toContainText('not valid')
    expect(formsPosted[0]).toMatchObject({
      caseWorkflowFormId: 7,
      payload: { reason: 'other', detail: 'Odd pattern', amount: '999' },
    })

    await form.getByLabel('Amount').fill('12')
    await page.getByTestId('case-form-submit').click()

    await expect(page.getByTestId('case-forms-dialog')).toBeHidden()
    expect(formsPosted).toHaveLength(2)
  })

  test('draws the submitted forms read only, and lists what a form can no longer draw', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/9041?section=forms')

    const first = page.getByTestId('case-form-entry-31')

    await expect(first).toContainText('Suspicious activity report')
    await expect(first.getByLabel('Reason')).toBeDisabled()
    await expect(first.locator('script')).toHaveCount(0)
    expect(
      await page.evaluate(() => (window as { entryShown?: boolean }).entryShown),
    ).toBeUndefined()
    await expect(first.getByTestId('case-form-entry-other')).toContainText('old value')

    const second = page.getByTestId('case-form-entry-30')

    await expect(second.getByTestId('case-form-entry-error')).toContainText('no longer be drawn')
    await expect(second.getByTestId('case-form-entry-other')).toContainText('kept')
  })

  test('shows every message the case workflow sent, with its outcome and what was sent', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/9041?section=messages')

    const sent = page.getByTestId('case-message-52')

    await expect(sent).toContainText('Macro: Email the branch')
    await expect(page.getByTestId('case-message-outcome-52')).toHaveText('Sent')
    await expect(sent).toContainText('branch@example.com')
    await expect(sent).toContainText('Case AC-9')
    await expect(sent.locator('b')).toHaveCount(0)

    await sent.getByText('What was sent').click()
    await expect(sent).toContainText('<b>Body</b> of the message')
    await sent.getByText('The SQL rows it was drawn from').click()
    await expect(sent).toContainText('"balance": 1234.5')

    await expect(page.getByTestId('case-message-outcome-51')).toHaveText('Refused')
    await expect(page.getByTestId('case-message-detail-51')).toContainText('hostChanged')
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the messages section in the ${theme} theme`, async ({
      page,
    }) => {
      await stub(page)
      await page.goto('/cases/9041?section=messages')
      await expect(page.getByTestId('case-message-52')).toBeVisible()
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await expect(page.getByTestId('case-message-52')).toBeVisible()
      await settleAnimations(page)

      const results = await new AxeBuilder({ page })
        .include('[data-testid="case-messages"]')
        .analyze()

      expect(results.violations).toEqual([])
    })
  }

  test('takes and releases the case through the whole record', async ({ page }) => {
    const { changes } = await stub(page)

    await page.route('**/api/GetCaseByIdQuery/*', (route) =>
      route.fulfill({ json: { ...caseDetail, locked: false, lockedUser: null } }),
    )
    await page.goto('/cases/9041')
    await page.getByTestId('case-take').click()

    await expect.poll(() => changes.length).toBe(1)
    expect(changes[0]).toMatchObject({ id: 9041, locked: true, lockedUser: 'alex' })
    expect(changes[0]).toHaveProperty('caseKeyValue', 'ACC-441')
  })

  test('moves the case to another workflow status', async ({ page }) => {
    const { changes } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-status').click()
    await expect(page.getByTestId('case-status-move')).toBeDisabled()
    await page.getByTestId('case-status-option-44444444-4444-4444-4444-444444444444').check()
    await page.getByTestId('case-status-move').click()

    await expect.poll(() => changes.length).toBe(1)
    expect(changes[0]).toMatchObject({
      caseWorkflowStatusGuid: '44444444-4444-4444-4444-444444444444',
    })
  })

  test('escalates to the status the current one escalates to', async ({ page }) => {
    const { changes } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-status').click()
    await expect(page.getByTestId('case-status-dialog')).toContainText('Moves to Escalated')
    await page.getByTestId('case-status-escalate').click()

    await expect.poll(() => changes.length).toBe(1)
    expect(changes[0]).toMatchObject({
      caseWorkflowStatusGuid: '44444444-4444-4444-4444-444444444444',
    })
  })

  test('asks before closing and then closes', async ({ page }) => {
    const { changes } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-close').click()
    await expect(page.getByTestId('case-close-dialog')).toBeVisible()
    await page.getByTestId('case-close-cancel').click()
    expect(changes).toHaveLength(0)

    await page.getByTestId('case-close').click()
    await page.getByTestId('case-close-confirm').click()

    await expect.poll(() => changes.length).toBe(1)
    expect(changes[0]).toMatchObject({ closedStatusId: 3 })
  })

  test('adds a coded note against the key', async ({ page }) => {
    const { notesPosted } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-note').click()
    await expect(page.getByTestId('case-note-add')).toBeDisabled()
    await page.getByTestId('case-note-action').selectOption('7')
    await page.getByTestId('case-note-text-input').fill('Spoke to the customer')
    await page.getByTestId('case-note-add').click()

    await expect.poll(() => notesPosted.length).toBe(1)
    expect(notesPosted[0]).toEqual({
      note: 'Spoke to the customer',
      actionId: 7,
      priorityId: 3,
      caseKey: 'AccountId',
      caseKeyValue: 'ACC-441',
      caseId: 9041,
    })
  })

  test('runs a macro on the case', async ({ page }) => {
    const { macrosRun } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-macro-5').click()

    await expect.poll(() => macrosRun.length).toBe(1)
    expect(macrosRun[0]).toEqual({ caseId: 9041, caseWorkflowMacroId: 5 })
  })

  test('chooses between the payload fields and a case display, shown with no scripts', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-details')).toBeVisible()

    await page.getByTestId('case-view-select').selectOption('3')
    await expect(page).toHaveURL(/display=3/)

    await expect(page.getByTestId('case-display').locator('h1')).toHaveText(
      'Summary for the branch',
    )
    await expect(page.getByTestId('case-display').locator('script')).toHaveCount(0)
    await expect(page.locator('iframe')).toHaveCount(0)
    expect(await page.evaluate(() => (window as { shown?: boolean }).shown)).toBeUndefined()
  })

  test('offers the held payment only when one is held', async ({ page }) => {
    await stub(page)
    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-decide')).toBeVisible()
    await expect(page.getByTestId('case-decide-tab-hold')).toHaveCount(0)
  })

  test('shows the held payment with its range and releases it with a reason', async ({ page }) => {
    const { holdDecisions } = await stub(page, { holds: true, permissions: [1, 68] })

    await page.goto('/cases/9041')
    await expect(page.getByTestId('case-decide-tab-hold')).toContainText('1 held')
    await page.getByTestId('case-decide-tab-hold').click()

    const row = page.getByTestId('case-hold-33333333-3333-3333-3333-333333333333')

    await expect(row).toContainText('Review hold')
    await expect(row).toContainText('Elevation 12 (range 10-19)')
    await expect(row).toContainText('Faster payments')
    await expect(
      page.getByTestId('case-hold-state-33333333-3333-3333-3333-333333333333'),
    ).toHaveText('Held')
    await expect(
      page.getByTestId('case-hold-legs-33333333-3333-3333-3333-333333333333'),
    ).toContainText('Web hook')

    await page.getByTestId('case-hold-release-33333333-3333-3333-3333-333333333333').click()
    await expect(page.getByTestId('case-hold-confirm')).toBeDisabled()
    await page.getByTestId('case-hold-reason').fill('Verified with the customer')
    await page.getByTestId('case-hold-confirm').click()

    await expect.poll(() => holdDecisions.length).toBe(1)
    expect(holdDecisions[0]).toEqual({
      path: 'Release',
      body: {
        caseId: 9041,
        entityAnalysisModelInstanceEntryGuid: '33333333-3333-3333-3333-333333333333',
        reason: 'Verified with the customer',
      },
    })
    await expect(
      page.getByTestId('case-hold-state-33333333-3333-3333-3333-333333333333'),
    ).toHaveText('Released')
    await expect(page.getByTestId('case-hold-decision')).toContainText('alex')
    await expect(page.getByTestId('case-hold-decision')).toContainText('Verified with the customer')
  })

  test('refuses a held payment with a reason', async ({ page }) => {
    const { holdDecisions } = await stub(page, { holds: true, permissions: [1, 68] })

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-hold').click()
    await page.getByTestId('case-hold-refuse-33333333-3333-3333-3333-333333333333').click()
    await page.getByTestId('case-hold-reason').fill('Mule account')
    await page.getByTestId('case-hold-confirm').click()

    await expect.poll(() => holdDecisions.length).toBe(1)
    expect(holdDecisions[0]!.path).toBe('Refuse')
    await expect(
      page.getByTestId('case-hold-state-33333333-3333-3333-3333-333333333333'),
    ).toHaveText('Refused')
  })

  test('keeps the payment held when the release hook fails', async ({ page }) => {
    const { holdDecisions } = await stub(page, {
      holds: true,
      permissions: [1, 68],
      holdStatus: 502,
    })

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-hold').click()
    await page.getByTestId('case-hold-release-33333333-3333-3333-3333-333333333333').click()
    await page.getByTestId('case-hold-reason').fill('Cleared')
    await page.getByTestId('case-hold-confirm').click()

    await expect.poll(() => holdDecisions.length).toBe(1)
    await expect(
      page.getByTestId('case-hold-state-33333333-3333-3333-3333-333333333333'),
    ).toHaveText('Held')
    await expect(page.getByTestId('case-hold-confirm')).toBeVisible()
  })

  test('does not offer release or refusal without the release permission', async ({ page }) => {
    await stub(page, { holds: true, permissions: [1] })

    await page.goto('/cases/9041')
    await page.getByTestId('case-decide-tab-hold').click()

    await expect(page.getByTestId('case-hold-dialog')).toContainText('needs the release permission')
    await expect(
      page.getByTestId('case-hold-release-33333333-3333-3333-3333-333333333333'),
    ).toHaveCount(0)
  })

  test('shows the messages sent about a held payment in the messages section', async ({ page }) => {
    await stub(page, { holds: true })

    await page.goto('/cases/9041?section=messages')

    await expect(page.getByTestId('case-hold-messages')).toContainText('Web hook')
  })

  test('shows where the planned features will land', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-checklist')).toBeVisible()
    await page.getByTestId('case-decide-planned').locator('xpath=ancestor::details').click()
    await expect(page.getByTestId('case-soon-decide')).not.toContainText('Held payment')
    await expect(page.getByTestId('case-soon-activations')).toContainText('Activation cards')

    await page.goto('/cases/9041?section=events')
    await expect(page.getByTestId('case-soon-events')).toHaveCount(0)
  })

  test('filters the journal from the address and says how many remain', async ({ page }) => {
    const { jobCalls } = await stub(page)

    const filter = {
      condition: 'AND',
      rules: [{ id: 'Payload.Amount', operator: 'greater', value: '1000' }],
    }

    await page.goto(`/cases/9041?jf=${encodeURIComponent(JSON.stringify(filter))}`)

    await expect(page.getByTestId('journal-scope-count')).toContainText('1 of 2 match')
    await expect(page.getByTestId('journal-filter-chip')).toHaveCount(1)
    await expect(page.getByTestId('journal-filter-chip')).toContainText('Payload.Amount')
    await expect(page.getByTestId('journal-select')).toHaveCount(1)
    await page.getByTestId('journal-tag-all-open').click()
    await expect(page.getByTestId('journal-tag-all-filtered')).toBeVisible()

    const sent = jobCalls.find((call) => call.path === '/Preview')?.body as Record<string, string>

    expect(JSON.parse(sent.filter ?? '{}')).toEqual(filter)
    expect(sent).not.toHaveProperty('lens')
    await page.getByTestId('journal-tag-all-cancel').click()

    await page.getByTestId('journal-filter-chip-remove').click()

    await expect(page.getByTestId('journal-filter-chip')).toHaveCount(0)
    await expect(page.getByTestId('journal-select')).toHaveCount(2)
  })

  test('builds a journal filter in the dialog', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')

    await page.getByTestId('journal-filter-open').click()

    const dialog = page.getByTestId('journal-filter-dialog')

    await dialog
      .locator('.rule-filter-container select')
      .first()
      .selectOption({ label: 'Payload.Counterparty' })
    await dialog.locator('.rule-operator-container select').first().selectOption('like')
    await dialog.locator('.rule-value-container input').first().fill('%acme%')
    await page.getByTestId('journal-filter-apply').click()

    await expect(page.getByTestId('journal-scope-count')).toContainText('1 of 2 match')
    await expect(page.getByTestId('journal-filter-chip')).toContainText('Payload.Counterparty')
    await expect(page).toHaveURL(/jf=/)
  })

  test('paints the bold line and a formatted cell and says why', async ({ page }) => {
    await stub(page)
    await stubFormatting(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect(journal).toContainText('Same Counterparty as this case: Globex')
    await expect(journal).toContainText('Amount matches ^4')

    const painted = journal.locator('.tabulator-row[title*="Globex"]')

    await expect(painted).toHaveCount(1)
    await expect(painted).toHaveCSS('font-weight', '700')
    await expect(painted).toHaveCSS('background-color', 'rgb(255, 233, 168)')
    await expect(journal.locator('.tabulator-row').first()).not.toHaveCSS('font-weight', '700')
  })

  test('carries the highlight reasons into the transaction panel', async ({ page }) => {
    await stub(page)
    await stubFormatting(page)

    await page.goto('/cases/9041')

    await page.getByTestId('case-journal').getByRole('button', { name: 'Open' }).nth(1).click()

    await expect(page.getByTestId('journal-drawer-highlights')).toContainText(
      'Same Counterparty as this case: Globex',
    )
  })

  test('does not draw the correlation and drift charts when there is nothing to chart', async ({
    page,
  }) => {
    await stub(page)
    await page.route(/\/api\/CaseKeyJournal\/Chart\/(Correlation|Drift)/, (route) =>
      route.fulfill({ status: 422, contentType: 'application/json', body: '"Too few measures."' }),
    )

    await page.goto('/cases/9041')

    const charts = page.getByTestId('journal-charts')

    await expect(charts.getByTestId('journal-chart-card').first()).toBeVisible()
    await expect(charts.getByText('How the measures move together')).toBeHidden()
    await expect(charts.getByText('Drift between earlier and recent history')).toBeHidden()
    const cards = charts.getByTestId('journal-chart-card')

    await expect
      .poll(async () => (await cards.count()) - (await cards.filter({ visible: true }).count()))
      .toBe(2)
    await expect(charts.getByTestId('server-chart-empty').filter({ visible: true })).toHaveCount(0)
  })

  test('puts the workflow dashboard first, given only the case id and its own filters', async ({
    page,
  }) => {
    await stub(page)

    const cards: string[] = []

    await page.route('**/api/GetCaseByIdQuery/*', (route) =>
      route.fulfill({
        json: { ...caseDetail, enableVisualisation: true, visualisationRegistryGuid: 'reg-guid' },
      }),
    )
    await page.route('**/api/VisualisationRegistry/GetByGuidActiveOnly/*', (route) =>
      route.fulfill({
        json: {
          id: 5,
          guid: 'reg-guid',
          name: 'Account history',
          columns: 2,
          columnWidth: 400,
          rowHeight: 260,
        },
      }),
    )
    await page.route(
      '**/api/VisualisationRegistryParameter/ByVisualisationRegistryIdActiveOnly/5',
      (route) =>
        route.fulfill({
          json: [{ id: 11, name: 'Region', dataTypeId: 1, defaultValue: 'North', required: false }],
        }),
    )
    await page.route(
      '**/api/VisualisationRegistryDatasource/ByVisualisationRegistryIdActiveOnly/5',
      (route) =>
        route.fulfill({
          json: [{ id: 21, name: 'Totals', columnSpan: 1, rowSpan: 1, priority: 1 }],
        }),
    )
    await page.route('**/api/VisualisationRegistryDatasource/*/Card', (route) => {
      cards.push(route.request().postData() ?? '')

      return route.fulfill({
        json: {
          id: 21,
          guid: '00000000-0000-0000-0000-000000000000',
          name: 'Totals',
          columnSpan: 1,
          rowSpan: 1,
          priority: 0,
          stateId: 1,
          message: null,
          rowCount: 1,
          truncated: false,
          executionMilliseconds: 3,
          display: null,
          grid: { columns: [{ name: 'total', ordinal: 1, dataTypeId: 3 }], rows: [[12]] },
          style: { modeId: 1, version: 'v1', css: null },
        },
      })
    })

    await page.goto('/cases/9041')

    const story = page.getByTestId('case-story')

    await expect(story.getByTestId('case-dashboard')).toBeVisible()
    await expect(story.getByTestId('visualisation-parameters')).toBeVisible()
    await expect(story.getByTestId('dashboard-title')).toHaveCount(0)
    await expect.poll(() => cards.length).toBeGreaterThan(0)

    const sent = JSON.parse(cards[0] ?? '{}') as Record<string, unknown>

    expect(sent.contextId).toBe(9041)
    expect(sent).not.toHaveProperty('caseKey')
    expect(sent).not.toHaveProperty('caseKeyValue')
    expect(sent.parameters).toEqual([{ id: 11, value: 'North' }])

    const sections = await story
      .locator('[data-testid^="canvas-"]')
      .evaluateAll((nodes) => nodes.map((node) => node.getAttribute('data-testid')))

    expect(sections[0]).toBe('canvas-dashboard')
    expect(sections[1]).toBe('canvas-summary')
    await expect(page.getByTestId('case-left-pane')).toContainText('1  Dashboard')
    await expect(page.getByTestId('case-left-pane')).toContainText('2  Case detail views')
    await expect(story.getByText('More charts')).toHaveCount(0)
  })

  test('has no dashboard card and numbers the sections from one when it is not turned on', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-story').getByTestId('case-dashboard')).toHaveCount(0)
    await expect(page.locator('#canvas-dashboard')).toHaveCount(0)
    await expect(page.getByTestId('case-left-pane')).not.toContainText('Dashboard')
    await expect(page.getByTestId('case-left-pane')).toContainText('1  Case detail views')
  })

  test('tells the story in order: the window and statistics, then one card with the grid and the map', async ({
    page,
  }) => {
    await stub(page)
    await stubLocation(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    const card = journal.getByTestId('journal-card')

    await expect(journal.getByTestId('journal-window-bar')).toBeVisible()
    await expect(
      journal.getByTestId('journal-window-bar').getByTestId('journal-window'),
    ).toBeVisible()
    await expect(
      journal.getByTestId('journal-window-bar').getByTestId('journal-activations-only'),
    ).toBeVisible()
    await expect(card.getByTestId('journal-scope')).toBeVisible()
    await expect(card.getByTestId('journal-window')).toHaveCount(0)
    await expect(card.getByTestId('journal-lens-payload')).toBeVisible()
    await expect(card.getByTestId('grid-export')).toBeVisible()
    await expect(card.getByTestId('journal-map-block')).toBeVisible()
    await expect(journal.getByTestId('journal-strip')).toBeVisible()

    const order = await Promise.all(
      [
        'journal-window-bar',
        'journal-strip',
        'journal-scope',
        'journal-controls',
        'journal-map-block',
      ].map(async (id) => (await journal.getByTestId(id).boundingBox())?.y ?? -1),
    )

    expect(order).toEqual([...order].sort((a, b) => a - b))
    expect(new Set(order).size).toBe(order.length)

    await expect(journal.getByTestId('watcher-fade')).toHaveCount(0)
    await expect(journal.getByTestId('watcher-live')).toHaveCount(0)
    await expect(journal.getByTestId('watcher-from')).toHaveCount(0)
    await expect(journal.getByTestId('journal-map-trails')).toBeChecked()
    await expect(journal.getByTestId('journal-map-heat')).not.toBeChecked()
    await expect(journal.getByTestId('journal-map-fit')).toBeVisible()
    await expect(journal.getByTestId('watcher-trails')).toHaveCount(0)
    await expect(journal.getByTestId('watcher-counts')).toHaveCount(0)
    await expect(journal.getByTestId('journal-map-count')).toHaveCount(0)
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-fitted', '2')
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-impossible', '1')
    await expect(journal.getByTestId('journal-lane')).toHaveCount(1)

    await journal.getByTestId('journal-map-trails').uncheck()

    await expect(page).toHaveURL(/jn=0/)
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-trails', '0')

    await journal.getByTestId('journal-map-heat').check()

    await expect(page).toHaveURL(/jh=1/)
  })

  test('narrows the map with the shared filter and adds a key from the scope bar', async ({
    page,
  }) => {
    await stub(page)

    const { trackCalls } = await stubLocation(page)
    const filter = {
      condition: 'AND',
      rules: [{ id: 'Payload.Amount', operator: 'greater', value: '1000' }],
    }

    await page.goto(`/cases/9041?jf=${encodeURIComponent(JSON.stringify(filter))}`)

    const journal = page.getByTestId('case-journal')

    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '1')
    await expect(journal.getByTestId('journal-scope-count')).toContainText('1 of 2 match')
    await expect(journal.getByTestId('journal-filter-chip')).toHaveCount(1)

    const sent = trackCalls.find((call) => call.get('filter') !== null)

    expect(JSON.parse(sent?.get('filter') ?? '{}')).toEqual(filter)

    await journal.getByTestId('journal-lane-open').click()
    await expect(journal.getByTestId('journal-lane-key').locator('option')).toHaveText([
      'Choose a key',
      'Device',
      'Ip address',
    ])
    await expect(journal.getByTestId('journal-lane-add')).toBeDisabled()
    await journal.getByTestId('journal-lane-key').selectOption('DeviceId')
    await journal.getByTestId('journal-lane-value').fill('D-9')
    await journal.getByTestId('journal-lane-add').click()

    await expect(journal.getByTestId('journal-lane')).toHaveCount(2)
    await expect(page).toHaveURL(/jm=/)
    await expect
      .poll(() => trackCalls.some((call) => (call.get('merge') ?? '').includes('D-9')))
      .toBe(true)

    await journal.getByTestId('journal-lane-remove').click()
    await expect(journal.getByTestId('journal-lane')).toHaveCount(1)

    await journal.getByTestId('journal-filter-chip-remove').click()
    await expect(journal.getByTestId('journal-filter-chip')).toHaveCount(0)
    await expect(page).not.toHaveURL(/jf=/)
  })

  test('sends the speed and distance limits to the track and keeps them in the address', async ({
    page,
  }) => {
    await stub(page)

    const { trackCalls } = await stubLocation(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect.poll(() => trackCalls.length).toBeGreaterThan(0)
    expect(trackCalls[0]?.get('limitKmh')).toBe('900')
    expect(trackCalls[0]?.get('minimumKilometres')).toBe('50')

    await journal.getByTestId('journal-map-limits-open').click()

    await expect(journal.getByTestId('journal-map-limit')).toHaveValue('900')
    await expect(journal.getByTestId('journal-map-distance')).toHaveValue('50')
    await expect(journal.getByTestId('journal-map-limits-reset')).toHaveCount(0)

    await journal.getByTestId('journal-map-limit').fill('300')
    await journal.getByTestId('journal-map-limit').press('Enter')
    await journal.getByTestId('journal-map-distance').fill('400')
    await journal.getByTestId('journal-map-distance').press('Tab')

    await expect(page).toHaveURL(/js=300/)
    await expect(page).toHaveURL(/jd=400/)
    await expect
      .poll(() =>
        trackCalls.some(
          (call) => call.get('limitKmh') === '300' && call.get('minimumKilometres') === '400',
        ),
      )
      .toBe(true)

    await journal.getByTestId('journal-map-limits-reset').click()

    await expect(journal.getByTestId('journal-map-limit')).toHaveValue('900')
    await expect(journal.getByTestId('journal-map-distance')).toHaveValue('50')
    await expect(page).not.toHaveURL(/js=/)

    await journal.getByTestId('journal-map-limit').fill('999999')
    await journal.getByTestId('journal-map-limit').press('Tab')

    await expect(journal.getByTestId('journal-map-limit')).toHaveValue('10000')
  })

  test('shows only the impossible legs when asked and saves that with the view', async ({
    page,
  }) => {
    await stub(page)

    const { posts } = await stubLocation(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect(journal.getByTestId('journal-map-only')).not.toBeChecked()
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')

    await journal.getByTestId('journal-map-only').check()

    await expect(page).toHaveURL(/ji=1/)
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')

    await journal.getByTestId('journal-map-limits-open').click()
    await journal.getByTestId('journal-map-limit').fill('5000')
    await journal.getByTestId('journal-map-limit').press('Tab')

    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '0')

    await journal.getByTestId('journal-map-limit').fill('300')
    await journal.getByTestId('journal-map-limit').press('Tab')

    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')

    await journal.getByTestId('journal-views-save').click()
    await journal.getByTestId('journal-views-name').fill('Only the bad ones')
    await journal.getByTestId('journal-views-apply').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toMatchObject({ impossibleOnly: true, limitKmh: 300 })

    await page.goto('/cases/9041')
    await journal.getByTestId('journal-views-open').click()
    await journal.getByTestId('journal-views-load').click()

    await expect(journal.getByTestId('journal-map-only')).toBeChecked()
    await journal.getByTestId('journal-map-limits-open').click()
    await expect(journal.getByTestId('journal-map-limit')).toHaveValue('300')
  })

  test('can draw every transaction with the filtered ones picked out', async ({ page }) => {
    await stub(page)

    const { trackCalls } = await stubLocation(page)
    const filter = {
      condition: 'AND',
      rules: [{ id: 'Payload.Amount', operator: 'greater', value: '1000' }],
    }

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')
    await expect(journal.getByTestId('journal-map-all')).toHaveCount(0)

    await page.goto(`/cases/9041?jf=${encodeURIComponent(JSON.stringify(filter))}`)

    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '1')
    await expect(journal.getByTestId('watcher-legend')).not.toContainText('Not matching the filter')

    await journal.getByTestId('journal-map-all').check()

    await expect(page).toHaveURL(/jb=1/)
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')
    await expect(journal.getByTestId('watcher-legend')).toContainText('Not matching the filter')
    expect(trackCalls.some((call) => call.get('includeUnmatched') === 'true')).toBe(true)
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-trails', '0')

    await journal.getByTestId('journal-map-all').uncheck()

    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '1')
    await expect(page).not.toHaveURL(/jb=1/)
  })

  test('saves a view as settings only, opens it again and deletes it', async ({ page }) => {
    await stub(page)

    const { posts, deletes } = await stubLocation(page)
    const filter = {
      condition: 'AND',
      rules: [{ id: 'Payload.Amount', operator: 'greater', value: '1000' }],
    }

    await page.goto(
      `/cases/9041?jw=30&jl=rules&jb=1&jf=${encodeURIComponent(JSON.stringify(filter))}`,
    )

    const journal = page.getByTestId('case-journal')

    await expect(journal.getByTestId('activation-map')).toBeVisible()
    await expect(journal.getByTestId('journal-views-open')).toContainText('0')

    await journal.getByTestId('journal-views-save').click()
    await journal.getByTestId('journal-views-name').fill('Two countries')
    await journal.getByTestId('journal-views-apply').click()

    await expect.poll(() => posts.length).toBe(1)
    expect(posts[0]).toMatchObject({
      caseId: 9041,
      name: 'Two countries',
      days: 30,
      lens: 'rules',
      showAll: true,
    })
    expect(JSON.parse(String(posts[0]?.filter))).toEqual(filter)
    expect(posts[0]).not.toHaveProperty('points')
    await expect(journal.getByTestId('journal-views-open')).toContainText('1')

    await page.goto('/cases/9041')
    await expect(journal.getByTestId('journal-lens-payload')).toBeVisible()
    await journal.getByTestId('journal-views-open').click()
    await journal.getByTestId('journal-views-load').click()

    await expect(page).toHaveURL(/jw=30/)
    await expect(page).toHaveURL(/jb=1/)
    await expect(journal.getByTestId('activation-map')).toHaveAttribute('data-points', '2')

    await journal.getByTestId('journal-views-open').click()
    await journal.getByTestId('journal-views-delete').click()

    await expect.poll(() => deletes.length).toBe(1)
    await expect(journal.getByTestId('journal-views-open')).toContainText('0')
  })

  test('offers no map action when the key cannot be located', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect(journal.getByRole('button', { name: 'Open' }).first()).toBeVisible()
    await expect(journal.getByRole('button', { name: 'Map' })).toHaveCount(0)
  })

  test('takes a transaction from the grid to the map and from the map to the drawer', async ({
    page,
  }) => {
    await stub(page)
    await stubLocation(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')

    await expect(journal.getByRole('button', { name: 'Map' })).toHaveCount(2)
    await journal.getByRole('button', { name: 'Map' }).nth(1).click()

    await expect(journal.getByTestId('journal-map-selected')).toContainText('key A')

    await journal.getByTestId('journal-map-open-transaction').click()

    await expect(page.getByTestId('journal-drawer')).toBeVisible()
    await expect(page).toHaveURL(/jt=2/)

    await page.getByTestId('journal-drawer-close').click()

    await expect(journal.locator('.tabulator-row[aria-current="true"]')).toHaveCount(1)
    await expect(journal.locator('.tabulator-row[aria-current="true"]')).toContainText('Globex')
  })

  test('marks the row of the open transaction in the grid and moves the mark with it', async ({
    page,
  }) => {
    await stub(page)

    await page.goto('/cases/9041')

    const journal = page.getByTestId('case-journal')
    const marked = journal.locator('.tabulator-row[aria-current="true"]')

    await expect(journal.getByRole('button', { name: 'Open' }).first()).toBeVisible()
    await expect(marked).toHaveCount(0)

    await journal.getByRole('button', { name: 'Open' }).first().click()

    await expect(page.getByTestId('journal-drawer')).toBeVisible()
    await expect(marked).toHaveCount(1)

    await page.getByTestId('journal-drawer-close').click()

    await expect(marked).toHaveCount(0)
  })

  test('takes the open transaction from the drawer to its place on the map', async ({ page }) => {
    await stub(page)
    await stubLocation(page)
    await page.route(/\/api\/CaseKeyJournal\/Transaction(\?|$)/, (route) =>
      route.fulfill({
        json: { ...transactionCard, id: 1, entityAnalysisModelInstanceEntryGuid: 'tx-1' },
      }),
    )

    await page.goto('/cases/9041?jt=1')

    await expect(page.getByTestId('journal-drawer')).toBeVisible()
    await page.getByTestId('journal-drawer-map').click()

    await expect(page.getByTestId('case-journal').getByTestId('journal-map-selected')).toBeVisible()
  })

  test('says so when the model has no location columns the caller can see', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')

    await expect(
      page.getByTestId('case-journal').getByRole('button', { name: 'Open' }).first(),
    ).toBeVisible()
    await expect(page.getByTestId('journal-map-block')).toHaveCount(0)
  })

  test('hides a journal column with its switch and saves the layout', async ({ page }) => {
    const { layoutPosts } = await stub(page)

    await page.goto('/cases/9041')

    const header = page
      .getByTestId('canvas-journal')
      .getByRole('columnheader', { name: 'Elevation' })

    await expect(header).toHaveCount(1)

    await page.getByTestId('journal-columns-open').click()
    await page.getByTestId('journal-column-ResponseElevation').uncheck()

    await expect(header).toHaveCount(0)
    await expect.poll(() => layoutPosts.length).toBe(1)

    const saved = JSON.parse(layoutPosts[0]?.json ?? '{}') as {
      lenses: Record<string, { field: string; hidden?: boolean }[]>
    }

    expect(saved.lenses.payload?.find((e) => e.field === 'ResponseElevation')?.hidden).toBe(true)
  })

  test('saves the order after a journal column is dragged', async ({ page }) => {
    const { layoutPosts } = await stub(page)

    await page.goto('/cases/9041')

    const headers = page.getByTestId('canvas-journal').getByRole('columnheader')
    const first = headers.filter({ hasText: 'Activations' })
    const second = headers.filter({ hasText: 'Elevation' })

    await expect(second).toHaveCount(1)
    await second.scrollIntoViewIfNeeded()

    const from = await second.boundingBox()
    const to = await first.boundingBox()

    expect(from && to).toBeTruthy()

    await page.mouse.move(from!.x + from!.width / 2, from!.y + from!.height / 2)
    await page.mouse.down()
    await page.evaluate(() => new Promise((resolve) => setTimeout(resolve, 400)))
    await page.mouse.move(to!.x + to!.width * 0.75, to!.y + to!.height / 2, { steps: 30 })
    await page.mouse.move(to!.x + to!.width * 0.8, to!.y + to!.height / 2)
    await page.mouse.up()

    await expect.poll(() => layoutPosts.length).toBeGreaterThan(0)

    const saved = JSON.parse(layoutPosts.at(-1)?.json ?? '{}') as {
      lenses: Record<string, { field: string }[]>
    }
    const order = (saved.lenses.payload ?? []).map((entry) => entry.field)

    expect(order.indexOf('Activations')).toBeLessThan(order.indexOf('ResponseElevation'))
  })

  test('opens the journal with the saved column layout and resets it', async ({ page }) => {
    const layout = JSON.stringify({
      version: 1,
      lenses: { payload: [{ field: 'ResponseElevation', hidden: true }] },
    })

    const { layoutPosts } = await stub(page, { layout })

    await page.goto('/cases/9041')

    const header = page
      .getByTestId('canvas-journal')
      .getByRole('columnheader', { name: 'Elevation' })

    await expect(page.getByTestId('journal-controls')).toBeVisible()
    await expect(header).toHaveCount(0)

    await page.getByTestId('journal-columns-open').click()
    await page.getByTestId('journal-columns-reset').click()

    await expect(header).toHaveCount(1)
    await expect.poll(() => layoutPosts.length).toBe(1)
    expect(JSON.parse(layoutPosts[0]?.json ?? '{}')).toEqual({ version: 1, lenses: {} })
  })

  test('explains the journal controls in help popovers', async ({ page }) => {
    await stub(page)

    await page.goto('/cases/9041')

    const controls = page.getByTestId('journal-controls')
    await expect(controls.getByTestId('field-help').first()).toBeVisible()

    await controls.getByTestId('field-help').first().click()
    await expect(controls.getByTestId('field-help-text').first()).toBeVisible()
    await expect(controls.getByTestId('field-help-text').first()).toContainText('Payload')
  })

  test('takes the next case from inside a case', async ({ page }) => {
    const { skims } = await stub(page)

    await page.goto('/cases/9041')
    await page.getByTestId('case-next').click()

    await expect.poll(() => skims.length).toBe(1)
    expect((skims[0] as { caseWorkflowGuid: string }).caseWorkflowGuid).toBe(workflow)
  })

  test('says so when the case cannot be loaded', async ({ page }) => {
    await stub(page)
    await page.route('**/api/GetCaseByIdQuery/*', (route) =>
      route.fulfill({ status: 404, json: {} }),
    )

    await page.goto('/cases/9041')

    await expect(page.getByTestId('case-failed')).toBeVisible()
  })

  test('refuses a role without the cases permission', async ({ page }) => {
    await stub(page, { permissions: [29] })

    await page.goto('/cases/9041')

    await expect(page.getByRole('alert')).toBeVisible()
    await expect(page.getByTestId('case-header')).toHaveCount(0)
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`has no accessibility violations in the ${theme} theme`, async ({ page }) => {
      await stub(page)

      await page.goto('/cases/9041')
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('case-journal')).toContainText('Globex')
      await page.getByRole('button', { name: 'Open' }).last().click()
      await expect(page.getByTestId('journal-adaptation-StructNet')).toBeVisible()

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })

    test(`has no accessibility violations in the ${theme} theme with a merged lane and a pin`, async ({
      page,
    }) => {
      await stub(page)

      await page.goto(`/cases/9041?jm=${encodeURIComponent('[["Device","D-9"]]')}`)
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('case-journal')).toContainText('Globex')
      await page.getByTestId('journal-pin').nth(1).click()
      await expect(page.getByTestId('journal-pin').nth(1)).toHaveAttribute('aria-pressed', 'true')
      await expect(page.getByTestId('journal-lane')).toHaveCount(2)

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })

    test(`has no accessibility violations in the ${theme} theme with the tag all dialog and a job`, async ({
      page,
    }) => {
      await stub(page)

      await page.goto('/cases/9041')
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('case-journal')).toContainText('Globex')
      await page.getByTestId('journal-tag-all-open').click()
      await expect(page.getByTestId('journal-tag-all-count')).toBeVisible()
      await page.getByTestId('journal-tag-all-dialog').evaluate(async (dialog) => {
        await new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))
        await Promise.all(
          dialog.getAnimations({ subtree: true }).map((animation) => animation.finished),
        )
      })

      const dialog = await new AxeBuilder({ page }).analyze()

      expect(dialog.violations).toEqual([])

      await page.getByTestId('journal-tag-all-add').first().check()
      await page.getByTestId('journal-tag-all-apply').click()
      await expect(page.getByTestId('journal-tag-all-dialog')).toBeHidden()
      await expect(page.getByTestId('journal-job-progress')).toBeVisible()

      const card = await new AxeBuilder({ page }).analyze()

      expect(card.violations).toEqual([])
    })

    test(`has no accessibility violations in the ${theme} theme with the map open`, async ({
      page,
    }) => {
      await stub(page)
      await stubLocation(page)

      const filter = {
        condition: 'AND',
        rules: [{ id: 'Payload.Amount', operator: 'greater', value: '1000' }],
      }

      await page.goto(`/cases/9041?jb=1&jf=${encodeURIComponent(JSON.stringify(filter))}`)
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('activation-map')).toBeVisible()
      await expect(page.getByTestId('watcher-legend')).toContainText('Not matching the filter')

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })

    test(`has no accessibility violations in the ${theme} theme with painted rows`, async ({
      page,
    }) => {
      await stub(page)
      await stubFormatting(page)

      await page.goto('/cases/9041')
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('case-journal')).toContainText('Same Counterparty as this case')

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })

    test(`has no accessibility violations in the ${theme} theme with the filter dialog open`, async ({
      page,
    }) => {
      await stub(page)

      const filter = {
        condition: 'AND',
        rules: [{ id: 'Payload.Amount', operator: 'greater', value: '1000' }],
      }

      await page.goto(`/cases/9041?jf=${encodeURIComponent(JSON.stringify(filter))}`)
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('journal-filter-chip')).toBeVisible()
      await page.getByTestId('journal-filter-open').click()
      await expect(page.getByTestId('journal-filter-dialog')).toBeVisible()
      await expect(page.locator('.rule-container').first()).toBeVisible()
      await page
        .getByTestId('journal-filter-dialog')
        .evaluate((dialog) =>
          Promise.all(
            dialog.getAnimations({ subtree: true }).map((animation) => animation.finished),
          ),
        )

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })

    test(`has no accessibility violations in the ${theme} theme with the tag dialog open`, async ({
      page,
    }) => {
      await stub(page)

      await page.goto('/cases/9041')
      await expect(page.getByTestId('case-details')).toContainText('ACME Ltd')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      await expect(page.getByTestId('case-journal')).toContainText('Globex')
      await page.getByTestId('journal-select-all').click()
      await page.getByTestId('journal-tag-open').click()
      await expect(page.getByTestId('journal-tag-dialog')).toBeVisible()
      await page
        .getByTestId('journal-tag-dialog')
        .evaluate((dialog) =>
          Promise.all(
            dialog.getAnimations({ subtree: true }).map((animation) => animation.finished),
          ),
        )

      const results = await new AxeBuilder({ page }).analyze()

      expect(results.violations).toEqual([])
    })
  }
})
