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

import type { HoldLeg } from '../detail/operationsApi'

export type JournalLens = 'payload' | 'rules' | 'features' | 'models' | 'sanctions'

export const journalLenses: readonly JournalLens[] = [
  'payload',
  'rules',
  'features',
  'models',
  'sanctions',
]

export interface JournalColumn {
  name: string
  title: string
  type: 'number' | 'string' | 'date' | 'boolean' | 'array'
  roleId: number
  roleInferred: boolean
  lens: string
  group: string
  drill: boolean
}

export interface JournalMeta {
  key: string
  value: string
  caseWorkflowGuid: string
  lens: string
  rowCount: number
  total: number
  drillKeys: { key: string; title: string }[]
  limit: number
  truncated: boolean
  location: { latitude: string; longitude: string } | null
  from: string
  to: string
}

export interface JournalLensCount {
  lens: JournalLens
  rows: number
}

export type JournalRow = Record<string, unknown> & { Id: number }

export interface JournalRows {
  meta: JournalMeta
  columns: JournalColumn[]
  lenses: JournalLensCount[]
  rows: JournalRow[]
}

export interface JournalField {
  name: string
  title: string
  lens: string
  group: string
  roleId: number
  roleInferred: boolean
  kind: 'measure' | 'category' | 'counterparty'
}

export interface NotableReason {
  field: string
  value: number
  median: number
  z: number
}

export interface NotableRow {
  id: number
  score: number
  worst: string
  reasons: NotableReason[]
}

export interface FieldSummary {
  field: string
  count: number
  sum: number
  mean: number
  median: number
  max: number
  latest: number
}

export interface JournalSummary {
  count: number
  first: string | null
  last: string | null
  activeDays: number
  activations: number
  activationRows: number
  maxElevation: number
  fields: FieldSummary[]
}

export interface NewValue {
  value: string
  firstSeen: string
  eventId: number
}

export interface NoveltyField {
  field: string
  distinct: number
  recent: NewValue[]
}

export interface JournalAnalytics {
  meta: {
    rowCount: number
    limit: number
    truncated: boolean
    lowConfidence: boolean
    sections: string[]
  }
  fields: JournalField[]
  summary: JournalSummary | null
  notable: { rows: NotableRow[]; lowConfidenceFields: string[] } | null
  novelty: { fields: NoveltyField[]; rows: Record<string, string[]> } | null
  peers: JournalPeers | null
}

export interface PeerComparison {
  field: string
  keyMedian: number
  keyRows: number
  peerMedian: number
  peerScale: number
  peerQ1: number
  peerQ3: number
  peerKeys: number
  z: number | null
  percentile: number
  direction: 'above' | 'below' | 'typical'
  lowConfidence: boolean
}

export interface JournalPeers {
  available: boolean
  reason: string | null
  computedDate: string | null
  windowDays: number | null
  segment: string | null
  segmentValue: string | null
  segmentFallback: boolean
  comparisons: PeerComparison[]
}

export interface TransactionActivation {
  name: string
  forced: boolean
}

export interface TransactionValue {
  group: string
  name: string
  value: unknown
}

export interface TransactionKey {
  name: string
  value: string | null
  drill: boolean
  payloadKey: string | null
}

export interface AdaptationModel {
  name: string | null
  family: string | null
  version: string | null
  artifactHash: string | null
  trainedDate: string | null
  featureCount: number | null
}

export interface AdaptationResult {
  threshold: number | null
  activated: boolean | null
  expectedPositiveRate: number | null
}

export interface CalibrationBand {
  lower: number | null
  upper: number | null
  expected: number | null
  observed: number | null
  count: number | null
}

export interface AdaptationCalibration {
  space: string | null
  calibrated: boolean | null
  method: string | null
  sample: number | null
  brier: number | null
  bands: CalibrationBand[]
}

export interface ContributionItem {
  name: string | null
  weight: number | null
  direction: number | null
  significance: number | null
  source: string | null
  humanLabel: string | null
  signed: number | null
  hidden: boolean
}

export interface AdaptationContribution {
  space: string | null
  method: string | null
  exact: boolean | null
  baseValue: number | null
  items: ContributionItem[]
  hiddenItems: number
}

export interface JourneyNode {
  feature: string | null
  operator: string | null
  threshold: number | null
  thresholdCategory: string | null
  source: string | null
  humanLabel: string | null
  hidden: boolean
}

export interface Adaptation {
  source: 'exhaustive' | 'advanced' | 'http'
  name: string
  value: number | null
  error: string | null
  humanLabel: string | null
  narrative: string | null
  model: AdaptationModel | null
  result: AdaptationResult | null
  calibration: AdaptationCalibration | null
  contribution: AdaptationContribution | null
  journey: JourneyNode[]
}

export interface HoldBlock {
  stateId: number
  rangeName: string | null
  rangeMinimum: number
  rangeMaximum: number
  elevation: number
  stampedDate: string
  decision: {
    actor: string | null
    date: string
    reason: string | null
    releaseHookName: string | null
    httpStatus: number | null
  } | null
  legs: HoldLeg[]
}

export interface TransactionCard {
  id: number
  entityAnalysisModelInstanceEntryGuid: string
  referenceDate: string | null
  createdDate: string | null
  responseElevation: number
  responseElevationName?: string | null
  hold?: HoldBlock | null
  entryKeyValue: string | null
  modelName: string | null
  tags: string[]
  activations: TransactionActivation[]
  values: TransactionValue[]
  keys: TransactionKey[]
  adaptations: Adaptation[]
}

export interface TransactionPayload {
  id: number
  fields: Record<string, unknown>
}

export interface AroundRow {
  id: number
  referenceDate: string | null
  responseElevation: number
  activationCount: number
  activations: string
}

export interface AroundGroup {
  name: string
  key: string
  value: string
  truncated: boolean
  rows: AroundRow[]
}

export interface AroundResult {
  from: string
  to: string
  groups: AroundGroup[]
}

export interface TravelLeg {
  lane: string
  fromTransaction: string
  toTransaction: string
  fromDate: string
  toDate: string
  kilometres: number
  hours: number
  speedKmh: number | null
  impossible: boolean
}

export interface TrackResult {
  meta: {
    key: string
    value: string
    from: string
    to: string
    total: number
    matched: number
    located: number
    unlocated: number
    limit: number
    truncated: boolean
    location: { latitude: string; longitude: string } | null
  }
  summary: {
    legs: number
    impossible: number
    limitKmh: number
    minimumKilometres: number
    worst: TravelLeg[]
  }
  points: JournalRow[]
}

export interface SavedView {
  guid: string
  caseId: number
  name: string
  days: number
  merge: string | null
  filter: string | null
  limitKmh: number | null
  minimumKilometres: number | null
  impossibleOnly: boolean
  showAll: boolean
  lens: string | null
  createdUser: string
  createdDate: string
}

export interface PinList {
  transactions: string[]
}

export interface TagSet {
  transaction: string
  tag: string[]
}

export interface TagResult {
  previous: TagSet[]
  current: TagSet[]
  skipped: number
  locked: number
}

export type TagJobState = 'Queued' | 'Running' | 'Done' | 'Failed' | 'Cancelled'

export interface TagJob {
  guid: string
  caseWorkflowGuid: string
  kind: 'Tag' | 'Undo'
  state: TagJobState
  key: string
  value: string
  add: string[]
  remove: string[]
  undoOf: string | null
  total: number
  processed: number
  changed: number
  skipped: number
  locked: number
  error: string | null
  canUndo: boolean
  createdDate: string
  startedDate: string | null
  finishedDate: string | null
}

export interface TagJobPreview {
  matching: number
  limit: number
  tooMany: boolean
}

export interface TagOptions {
  tag: string[]
}
