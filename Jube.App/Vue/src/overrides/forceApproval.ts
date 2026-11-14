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

import type {ApprovalKind, ApprovalState, ApprovalStatus} from '../approval/model'
import {stateOf} from '../approval/model'
import type {ApprovalTarget} from '../composables/useRecordApprovals'
import type {OverrideKind} from './types'

export interface ForceApprovalArea {
  endpoint: string
  approval: ApprovalKind
}

export const modelOverrideArea: ForceApprovalArea = {
  endpoint: '/api/EntityAnalysisModelOverride',
  approval: {kind: 22, historyRoute: '/api/EntityAnalysisModelOverrideVersionHistory'},
}

export const ruleOverrideArea: ForceApprovalArea = {
  endpoint: '/api/EntityAnalysisModelActivationRuleOverride',
  approval: {
    kind: 23,
    historyRoute: '/api/EntityAnalysisModelActivationRuleOverrideVersionHistory',
  },
}

export interface ForceRow {
  overrideKind: OverrideKind
  overrideId: number
  hasOverride: boolean
}

export function isGoverned(row: ForceRow): boolean {
  return row.hasOverride && row.overrideKind === 1 && row.overrideId > 0
}

export function forceTargets(rows: ForceRow[], area: ForceApprovalArea): ApprovalTarget[] {
  return rows.filter(isGoverned).map((row) => ({kind: area.approval.kind, id: row.overrideId}))
}

export function approvalStateOf(status: ApprovalStatus | undefined): ApprovalState | undefined {
  return status ? stateOf(status) : undefined
}

export function awaitingCount(states: (ApprovalState | undefined)[]): number {
  return states.filter((state) => state === 'awaiting' || state === 'deletionAwaiting').length
}
