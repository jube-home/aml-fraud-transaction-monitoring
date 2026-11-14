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
import {ApiError} from '../../api/useApi'
import {
  defaultExpiry,
  isolate,
  isWide,
  laneNames,
  mergeParameter,
  otherKeys,
  refusalText,
  standing,
  standingKey,
  standingTone,
  targetName,
  widerKnown,
  widerNumber,
  wideReach,
} from '../state'
import type {CandidateValue} from '../types'

function value(partial: Partial<CandidateValue> = {}): CandidateValue {
  return {
    value: '10.0.0.1',
    count: 3,
    firstSeenDate: null,
    lastSeenDate: null,
    lanes: [],
    widerCountStateId: 1,
    widerTransactions: 30,
    widerKeys: 12,
    widerCapped: false,
    ...partial,
  }
}

describe('standingKey and standingTone', () => {
  it('names every standing and gives it a tone', () => {
    expect(standingKey(standing.approved)).toBe('approved')
    expect(standingKey(standing.rejected)).toBe('rejected')
    expect(standingKey(standing.retracted)).toBe('retracted')
    expect(standingKey(standing.expired)).toBe('expired')
    expect(standingKey(standing.removed)).toBe('removed')
    expect(standingKey(standing.pending)).toBe('pending')
    expect(standingKey(99)).toBe('pending')
    expect(standingTone(standing.approved)).toBe('success')
    expect(standingTone(standing.pending)).toBe('warning')
    expect(standingTone(standing.rejected)).toBe('error')
    expect(standingTone(standing.removed)).toBe('error')
    expect(standingTone(standing.expired)).toBe('neutral')
  })
})

describe('the wider count', () => {
  it('is known only when it was counted and both figures are present', () => {
    expect(widerKnown(value())).toBe(true)
    expect(widerKnown(value({widerCountStateId: 2}))).toBe(false)
    expect(widerKnown(value({widerKeys: null}))).toBe(false)
  })

  it('counts the other case keys, not the one in hand', () => {
    expect(otherKeys({widerKeys: 12})).toBe(11)
    expect(otherKeys({widerKeys: 1})).toBe(0)
    expect(otherKeys({widerKeys: null})).toBe(0)
    expect(wideReach(value())).toBe(11)
    expect(wideReach(value({widerCountStateId: 2}))).toBe(0)
  })

  it('warns from ten other case keys', () => {
    expect(isWide(value({widerKeys: 11}))).toBe(true)
    expect(isWide(value({widerKeys: 10}))).toBe(false)
    expect(isWide(value({widerCountStateId: 2, widerKeys: 500}))).toBe(false)
  })

  it('marks a capped figure with a plus and an empty one with nothing', () => {
    expect(widerNumber(1000, true)).toBe('1,000+')
    expect(widerNumber(12, false)).toBe('12')
    expect(widerNumber(null, false)).toBe('')
  })

  it('keeps Western digits in Arabic and Urdu and groups by the locale otherwise', () => {
    expect(widerNumber(1234567, false, 'ar')).toBe('1,234,567')
    expect(widerNumber(1234567, false, 'ur')).toMatch(/^[0-9,]+$/)
    expect(widerNumber(1234567, true, 'sv')).toMatch(/^1\s234\s567\+$/)
  })
})

describe('bidi isolation of keys, values and names', () => {
  it('wraps a value in first strong isolate marks and leaves nothing empty', () => {
    expect(isolate('10.0.0.1')).toBe('\u206810.0.0.1\u2069')
    expect(isolate('')).toBe('')
    expect(isolate(null)).toBe('')
    expect(isolate(undefined)).toBe('')
  })

  it('isolates the model and the rule separately', () => {
    expect(targetName('Retail', 'Velocity')).toBe('\u2068Retail\u2069 / \u2068Velocity\u2069')
    expect(targetName('Retail', null)).toBe('\u2068Retail\u2069')
    expect(targetName(null, null)).toBe('')
  })
})

describe('laneNames and mergeParameter', () => {
  it('names only the merged lanes', () => {
    expect(
      laneNames(
        value({
          lanes: [
            {laneKey: '', laneValue: 'A', count: 2},
            {laneKey: 'AccountId', laneValue: 'B', count: 1},
          ],
        }),
      ),
    ).toBe('AccountId B')
  })

  it('sends no merge when there are none and plain key and value pairs otherwise', () => {
    expect(mergeParameter([])).toBeNull()
    expect(mergeParameter([{key: 'AccountId', value: 'B'}])).toBe(
      '[{"key":"AccountId","value":"B"}]',
    )
  })
})

describe('refusalText', () => {
  it('uses the server message on a refusal and the fallback otherwise', () => {
    expect(refusalText(new ApiError(409, {code: 'Refused', message: 'Not allowed'}), 'x')).toBe(
      'Not allowed',
    )
    expect(refusalText(new ApiError(409, {}), 'fallback')).toBe('fallback')
    expect(refusalText(new ApiError(403, {}), 'fallback')).toBe('fallback')
    expect(refusalText(new Error('boom'), 'fallback')).toBe('fallback')
    expect(
      refusalText(
        new ApiError(400, {
          errors: [{propertyName: 'Reason', errorMessage: 'A reason is required.'}],
        }),
        'x',
      ),
    ).toBe('A reason is required.')
  })
})

describe('defaultExpiry', () => {
  it('is thirty days on', () => {
    expect(defaultExpiry(new Date('2026-11-03T10:00:00Z')).toISOString()).toBe(
      '2026-12-03T10:00:00.000Z',
    )
  })
})
