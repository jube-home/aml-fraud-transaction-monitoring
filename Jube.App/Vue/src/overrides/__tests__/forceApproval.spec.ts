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

import {describe, expect, it} from 'vitest'
import type {ApprovalStatus} from '../../approval/model'
import {
  approvalStateOf,
  awaitingCount,
  forceTargets,
  isGoverned,
  modelOverrideArea,
  ruleOverrideArea,
} from '../forceApproval'

function status(overrides: Partial<ApprovalStatus>): ApprovalStatus {
  return {
    kind: 22,
    entityId: 1,
    name: 'IP = 1.2.3.4',
    currentVersion: 1,
    effectiveVersion: null,
    deleted: false,
    pending: false,
    rejected: false,
    makerUser: 'maker',
    approvals: 0,
    approvalsRequired: 1,
    canApprove: false,
    note: null,
    returnedNote: null,
    ...overrides,
  } as ApprovalStatus
}

describe('force override approval areas', () => {
  it('addresses the two approval kinds and their endpoints', () => {
    expect(modelOverrideArea.approval.kind).toBe(22)
    expect(modelOverrideArea.endpoint).toBe('/api/EntityAnalysisModelOverride')
    expect(ruleOverrideArea.approval.kind).toBe(23)
    expect(ruleOverrideArea.endpoint).toBe('/api/EntityAnalysisModelActivationRuleOverride')
    expect(modelOverrideArea.approval.historyRoute).toMatch(/^\/api\/.+VersionHistory$/)
    expect(ruleOverrideArea.approval.historyRoute).toMatch(/^\/api\/.+VersionHistory$/)
  })
})

describe('isGoverned and forceTargets', () => {
  const rows = [
    {overrideKind: 1 as const, overrideId: 7, hasOverride: true},
    {overrideKind: 0 as const, overrideId: 8, hasOverride: true},
    {overrideKind: 1 as const, overrideId: 0, hasOverride: true},
    {overrideKind: 1 as const, overrideId: 9, hasOverride: false},
  ]

  it('governs only a present Force override that has a row', () => {
    expect(rows.map(isGoverned)).toEqual([true, false, false, false])
  })

  it('targets only the governed rows, with the area kind', () => {
    expect(forceTargets(rows, modelOverrideArea)).toEqual([{kind: 22, id: 7}])
    expect(forceTargets(rows, ruleOverrideArea)).toEqual([{kind: 23, id: 7}])
  })
})

describe('approvalStateOf and awaitingCount', () => {
  it('reads a pending Force override as awaiting, never as approved', () => {
    expect(approvalStateOf(status({pending: true}))).toBe('awaiting')
    expect(approvalStateOf(status({effectiveVersion: 1}))).toBe('approved')
    expect(approvalStateOf(status({rejected: true}))).toBe('rejected')
    expect(approvalStateOf(status({pending: true, deleted: true}))).toBe('deletionAwaiting')
    expect(approvalStateOf(undefined)).toBeUndefined()
  })

  it('counts awaiting and awaiting deletion', () => {
    expect(awaitingCount(['awaiting', 'approved', undefined, 'deletionAwaiting', 'rejected'])).toBe(
      2,
    )
    expect(awaitingCount([])).toBe(0)
  })
})
