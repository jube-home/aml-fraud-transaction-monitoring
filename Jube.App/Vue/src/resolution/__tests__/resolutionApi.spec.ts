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
import {canDecide, matchDecision, percent, type EntityMatch} from '../resolutionApi'

const match = (decisionId: number): EntityMatch => ({
  id: 1,
  nodeIdA: 1,
  keyA: 'Account',
  valueA: 'A',
  nodeIdB: 2,
  keyB: 'Account',
  valueB: 'B',
  sourceId: 2,
  posterior: 0.9,
  evidence: null,
  decisionId,
  decidedUser: null,
  decidedDate: null,
  decisionNote: null,
  resolvedEntityId: null,
  createdDate: '2026-11-11T00:00:00Z',
})

describe('entity resolution', () => {
  it('lets only a pending match be decided', () => {
    expect(canDecide(match(matchDecision.pending))).toBe(true)
    expect(canDecide(match(matchDecision.confirmed))).toBe(false)
    expect(canDecide(match(matchDecision.rejected))).toBe(false)
  })

  it('shows the posterior probability as a percentage', () => {
    expect(percent(0.9)).toBe('90.0%')
  })
})
