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
import {canDecide, canRetract, proposalReady, type RatingRequest} from '../entityRiskApi'

const request = (stateId: number): RatingRequest => ({
  guid: 'g',
  keyName: 'Account',
  keyValue: '1',
  tierId: 3,
  score: null,
  reviewByDate: null,
  reason: 'r',
  basedOnVersion: 0,
  stateId,
  resultingVersion: null,
  requestedUser: 'asker',
  requestedDate: '2026-11-11T00:00:00Z',
  decidedUser: null,
  decidedDate: null,
  decisionNote: null,
})

describe('entity risk proposals', () => {
  it('needs a key, a tier within range and a reason', () => {
    expect(proposalReady('Account', '1', 3, 'Adverse media')).toBe(true)
    expect(proposalReady('', '1', 3, 'r')).toBe(false)
    expect(proposalReady('Account', '', 3, 'r')).toBe(false)
    expect(proposalReady('Account', '1', 0, 'r')).toBe(false)
    expect(proposalReady('Account', '1', 3, '  ')).toBe(false)
  })

  it('lets someone else decide a pending proposal, never the proposer', () => {
    expect(canDecide(request(1), 'other')).toBe(true)
    expect(canDecide(request(1), 'asker')).toBe(false)
    expect(canDecide(request(2), 'other')).toBe(false)
  })

  it('lets only the proposer retract a pending proposal', () => {
    expect(canRetract(request(1), 'asker')).toBe(true)
    expect(canRetract(request(1), 'other')).toBe(false)
  })
})
