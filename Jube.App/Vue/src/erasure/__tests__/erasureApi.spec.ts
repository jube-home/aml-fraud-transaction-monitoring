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
import {
  canDecide,
  canRetract,
  erasureState,
  subjectReady,
  type ErasureRequest,
} from '../erasureApi'

function request(overrides: Partial<ErasureRequest>): ErasureRequest {
  return {
    guid: 'g',
    keyName: 'AccountId',
    keyValue: '1',
    reason: 'r',
    stateId: erasureState.pending,
    refusalReasonId: null,
    archiveRowsFound: null,
    archiveRowsRemoved: null,
    graphNodesRemoved: null,
    graphEdgesRemoved: null,
    casePayloadsNulled: null,
    requestedUser: 'asker',
    requestedDate: '2026-11-11T00:00:00Z',
    decidedUser: null,
    decidedDate: null,
    decisionNote: null,
    executedDate: null,
    ...overrides,
  }
}

describe('erasure rules', () => {
  it('lets a different user decide a pending request but never the requester', () => {
    expect(canDecide(request({}), 'other')).toBe(true)
    expect(canDecide(request({}), 'asker')).toBe(false)
  })

  it('does not offer a decision once the request is no longer pending', () => {
    expect(canDecide(request({stateId: erasureState.executed}), 'other')).toBe(false)
  })

  it('lets only the requester retract, and only while pending', () => {
    expect(canRetract(request({}), 'asker')).toBe(true)
    expect(canRetract(request({}), 'other')).toBe(false)
    expect(canRetract(request({stateId: erasureState.approved}), 'asker')).toBe(false)
  })

  it('needs a key name and value within the limits', () => {
    expect(subjectReady('AccountId', '42')).toBe(true)
    expect(subjectReady('', '42')).toBe(false)
    expect(subjectReady('AccountId', '  ')).toBe(false)
    expect(subjectReady('AccountId', 'x'.repeat(501))).toBe(false)
  })
})
