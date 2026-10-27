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

import { describe, expect, it } from 'vitest'
import {
  legKindText,
  legOutcomeClass,
  legOutcomeText,
  rangeText,
  stateClass,
  stateText,
} from '../holdText'

const t = (key: string) => key

describe('hold text', () => {
  it('names every state of a hold and colours only the ones that need attention', () => {
    expect([1, 2, 3, 4].map((id) => stateText(t, id))).toEqual([
      'caseDetail.hold.state.held',
      'caseDetail.hold.state.declined',
      'caseDetail.hold.state.released',
      'caseDetail.hold.state.refused',
    ])
    expect(stateClass(1)).toBe('badge-warning')
    expect(stateClass(2)).toBe('badge-error')
    expect(stateClass(3)).toBe('badge-success')
    expect(stateClass(4)).toBe('badge-neutral')
  })

  it('names every message kind and outcome and shows a failure in the error colour', () => {
    expect([1, 2, 3, 4, 5].map((id) => legKindText(t, id))).toEqual([
      'caseDetail.hold.leg.kind.email',
      'caseDetail.hold.leg.kind.sms',
      'caseDetail.hold.leg.kind.webhook',
      'caseDetail.hold.leg.kind.release',
      'caseDetail.hold.leg.kind.refuse',
    ])
    expect([1, 2, 3, 4].map((id) => legOutcomeText(t, id))).toEqual([
      'caseDetail.hold.leg.outcome.queued',
      'caseDetail.hold.leg.outcome.sent',
      'caseDetail.hold.leg.outcome.failed',
      'caseDetail.hold.leg.outcome.refused',
    ])
    expect(legOutcomeClass(2)).toBe('badge-success')
    expect(legOutcomeClass(3)).toBe('badge-error')
    expect(legOutcomeClass(4)).toBe('badge-error')
    expect(legOutcomeClass(1)).toBe('badge-ghost')
  })

  it('writes a single level once and a range with both ends', () => {
    expect(rangeText(500, 500)).toBe('500')
    expect(rangeText(10, 19)).toBe('10-19')
  })
})
