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

export type CaseLane = 1 | 2 | 3 | 4

export interface CaseRisk {
  assessed: boolean
  sequence: number
  lane: number
  laneName: string | null
  laneReason: string | null
  likelihood: number
  likelihoodBand: number
  likelihoodBandName: string | null
  impact: number
  impactName: string | null
  priority: number
  expectedHarm: number
  method: number
  evidenceCount: number
  nextDeadline: string | null
  deadlineBreached: boolean
  topDriver: string | null
  topDriverFamily: string | null
  assessedDate: string | null
}

export interface CaseAttentionReason {
  code: number
  text: string | null
}

export interface CaseAttention {
  needed: boolean
  reasons: CaseAttentionReason[]
  lastViewedDate: string | null
  changesSince: number
  priorityRise: number
}

export interface CaseServiceLevelMeasure {
  dimension: number
  dimensionName: string | null
  state: number
  stateName: string | null
  dueDate: string
  elapsedFraction: number
  actualMinutes: number | null
  targetMinutes: number
}

export interface CaseServiceLevel {
  state: number
  stateName: string | null
  worst: CaseServiceLevelMeasure | null
  measures: CaseServiceLevelMeasure[]
}

export interface CaseRow {
  caseId: number
  caseKey: string | null
  caseKeyValue: string | null
  createdDate: string
  ageMinutes: number
  caseWorkflowGuid: string
  caseWorkflowName: string | null
  caseWorkflowStatusGuid: string
  statusName: string | null
  foreColor: string | null
  backColor: string | null
  statusEnteredDate: string | null
  locked: boolean
  lockedUser: string | null
  mine: boolean
  closedStatusId: number
  diary: boolean
  diaryDate: string | null
  diaryTypeId: number
  diaryType: string | null
  qaSample: boolean
  value: number | null
  risk: CaseRisk
  attention: CaseAttention
  serviceLevel: CaseServiceLevel
  firstViewedDate: string | null
  firstViewedBy: string | null
  firstOwnedDate: string | null
  firstOwnedBy: string | null
  lastEventTypeId: number | null
  lastEventUser: string | null
  lastEventDate: string | null
}

export interface CasePage {
  total: number
  take: number
  skip: number
  sort: number
  generatedAt: string
  rows: CaseRow[]
}

export interface CaseCount {
  kind: 'View' | 'Workflow' | 'Own'
  key: string
  name: string
  count: number
  mine: boolean
  failed: boolean
}

export interface CaseWorkflowChoice {
  guid: string
  name: string
  modelName: string
}

export interface CaseMatrixCell {
  likelihoodBand: number
  impact: number
  cases: number
}

export interface CaseMatrix {
  total: number
  cells: CaseMatrixCell[]
}

export interface CaseSkim {
  case: CaseRow | null
  nextCaseIds: number[]
  remaining: number
}

export interface CaseContribution {
  family: string | null
  label: string | null
  source: string | null
  confirmed: number
  closed: number
  precision: number
  logLikelihoodRatio: number
  decay: number
  weight: number
  logOdds: number
  sharesInputs: boolean
}

export interface CaseImpactComponents {
  typologySeverity: number
  moneyBand: number | null
  value: number | null
  exposureUplift: number
  breadthUplift: number
  floor: number
}

export interface CaseRiskHistoryItem {
  sequence: number
  createdDate: string
  trigger: number
  lane: number
  likelihood: number
  impact: number
  priority: number
}

export interface CaseStay {
  caseWorkflowStatusGuid: string
  statusName: string | null
  enterReason: number
  enteredDate: string
  enteredUser: string | null
  exitedDate: string | null
  exitReason: number | null
  minutes: number
  maxMinutes: number | null
}

export interface CaseEventSummary {
  eventTypeId: number
  eventType: string | null
  user: string | null
  date: string | null
  before: string | null
  after: string | null
  new: boolean
}

export interface CaseNoteSummary {
  note: string | null
  user: string | null
  date: string | null
}

export interface CaseKeyHistory {
  closed: number
  confirmed: number
  newestClosedDate: string | null
  otherOpenCases: number
}

export interface CaseDelta {
  firstLook: boolean
  lastViewedDate: string | null
  priorityThen: number | null
  priorityNow: number
  changes: CaseEventSummary[]
}

export interface CaseSummary {
  case: CaseRow
  contributions: CaseContribution[]
  baseRate: number | null
  impact: CaseImpactComponents | null
  riskHistory: CaseRiskHistoryItem[]
  stays: CaseStay[]
  recentEvents: CaseEventSummary[]
  notes: CaseNoteSummary[]
  keyHistory: CaseKeyHistory
  since: CaseDelta
}

export interface CaseSearchRequest {
  caseWorkflowGuid: string
  view?: number
  caseWorkflowFilterGuid?: string
  caseSearchFilterGuid?: string
  sessionGuid?: string
  sort?: number
  take?: number
  skip?: number
}

export interface CaseAnalyticsHeadline {
  opened: number
  closed: number
  openNow: number
  netChange: number
  unassessedOpen: number
  urgentOpen: number
  decided: number
  confirmedRate: number | null
  medianFirstViewMinutes: number | null
  medianResolutionMinutes: number | null
  slaBreachedOpen: number
  slaAtRiskOpen: number
  reopenRate: number
}

export interface CaseFlowDay {
  date: string
  opened: number
  closed: number
  backlog: number
}

export interface CaseMyDay {
  goal: number | null
  closedToday: number
  streak: number
  bestDay: number
  typicalDay: number
  days: { date: string; closed: number }[]
}

export interface CaseAnalyticsSummary {
  from: string
  to: string
  casesAnalysed: number
  truncated: boolean
  headline: CaseAnalyticsHeadline
  flow: CaseFlowDay[]
  myDay: CaseMyDay
}

export interface CaseQaSample {
  picked: number
  open: number
  closed: number
  decided: number
  confirmed: number
  confirmedRate: number | null
  overallConfirmedRate: number | null
  enough: boolean
}
