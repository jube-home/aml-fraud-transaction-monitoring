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

export type OverrideKindId = 0 | 1

export interface SuppressionCounts {
  countInCase: number
  firstSeenDate: string | null
  lastSeenDate: string | null
  widerCountStateId: number
  widerTransactions: number | null
  widerKeys: number | null
  widerCapped: boolean
}

export interface SuppressionTarget {
  id: number
  entityAnalysisModelGuid: string
  entityAnalysisModelName: string | null
  ruleName: string | null
  overrideKindId: number
  appliedOverrideId: number | null
  appliedDate: string | null
  standingId: number
}

export interface SuppressionEvidence {
  id: number
  archiveId: number
  referenceDate: string | null
  laneKey: string | null
}

export interface SuppressionApproval {
  id: number
  suppressionVersion: number
  decisionId: number
  note: string | null
  createdUser: string | null
  createdDate: string | null
}

export interface CaseSuppression {
  id: number
  caseId: number
  caseKey: string | null
  caseKeyValue: string | null
  overrideKey: string
  overrideKeyValue: string
  pathId: number
  reason: string | null
  expiryDate: string
  stateId: number
  standingId: number
  sourceId: number
  version: number
  approvalsRequired: number
  approvalsRecorded: number
  canDecide: boolean
  canRetract: boolean
  canResubmit: boolean
  then: SuppressionCounts
  now: SuppressionCounts | null
  targets: SuppressionTarget[]
  transactions: SuppressionEvidence[]
  approvals: SuppressionApproval[]
  createdUser: string | null
  createdDate: string | null
  decidedUser: string | null
  decidedDate: string | null
}

export interface CandidateKey {
  name: string
  isCaseKey: boolean
  enabled: boolean
  reason: string | null
}

export interface CandidateLane {
  laneKey: string
  laneValue: string
  count: number
}

export interface CandidateValue {
  value: string
  count: number
  firstSeenDate: string | null
  lastSeenDate: string | null
  lanes: CandidateLane[]
  widerCountStateId: number
  widerTransactions: number | null
  widerKeys: number | null
  widerCapped: boolean
}

export interface CandidateTransaction {
  archiveId: number
  referenceDate: string | null
  responseElevation: number
  activationCount: number
  activations: string
  laneKey: string | null
}

export interface InForce {
  id: number
  isRule: boolean
  overrideKey: string
  overrideKeyValue: string
  entityAnalysisModelName: string | null
  ruleName: string | null
  overrideKindId: number
  expiryDate: string | null
  createdUser: string | null
  createdDate: string | null
  originCaseId: number | null
}

export interface PickedTarget {
  entityAnalysisModelGuid: string
  ruleName: string | null
  overrideKind: OverrideKindId
}

export interface SuppressionRequest {
  caseId: number
  pathId: 1 | 2
  overrideKey: string
  overrideKeyValue: string | null
  reason: string
  expiryDate: string | null
  targets: { entityAnalysisModelGuid: string; ruleName: string | null; overrideKindId: number }[]
  archiveIds: number[]
  merge: string | null
}

export interface SuppressionDecision {
  id: number
  note?: string | null
  expiryDate?: string | null
}
