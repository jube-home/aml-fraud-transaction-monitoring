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
  defaultMinPercent,
  layoutStays,
  orderStays,
  sharePercents,
  type StayInput,
} from '../stayBar'

function stay(minutes: number, over: Partial<StayInput> = {}): StayInput {
  return {
    caseWorkflowStatusGuid: 'a',
    enteredDate: '2026-10-01T00:00:00Z',
    exitedDate: '2026-10-02T00:00:00Z',
    minutes,
    maxMinutes: null,
    ...over,
  }
}

describe('sharePercents', () => {
  it('is proportional to minutes and sums to one hundred', () => {
    const result = sharePercents([100, 300, 600])

    expect(result[0]).toBeCloseTo(10)
    expect(result[2]! / result[1]!).toBeCloseTo(2)
    expect(result.reduce((a, b) => a + b, 0)).toBeCloseTo(100)
  })

  it('never hides a tiny stay and keeps the rest proportional', () => {
    const result = sharePercents([1, 1000, 2000])

    expect(result[0]).toBe(defaultMinPercent)
    expect(result[2]! / result[1]!).toBeCloseTo(2)
    expect(result.reduce((a, b) => a + b, 0)).toBeCloseTo(100)
  })

  it('cascades the minimum when raising one pushes another under', () => {
    const result = sharePercents([1, 6, 1000], 5)

    expect(Math.min(...result)).toBeGreaterThanOrEqual(5)
    expect(result.reduce((a, b) => a + b, 0)).toBeCloseTo(100)
  })

  it('splits evenly with no time or too many stays', () => {
    expect(sharePercents([0, 0])).toEqual([50, 50])
    expect(sharePercents(Array.from({ length: 30 }, () => 5)).every((p) => p === 100 / 30)).toBe(
      true,
    )
  })

  it('treats negative and invalid minutes as zero and handles none', () => {
    expect(sharePercents([])).toEqual([])
    expect(sharePercents([-5, Number.NaN, 100])[2]).toBeCloseTo(100 - 2 * defaultMinPercent)
  })
})

describe('layoutStays', () => {
  it('flags a breach with the minutes over and caps used at the limit', () => {
    const [ok, over] = layoutStays([stay(30, { maxMinutes: 60 }), stay(90, { maxMinutes: 60 })])

    expect(ok).toMatchObject({ breach: false, overMinutes: 0, used: 30 })
    expect(over).toMatchObject({ breach: true, overMinutes: 30, used: 60 })
  })

  it('has no used figure without a limit', () => {
    expect(layoutStays([stay(30)])[0]).toMatchObject({ used: null, breach: false })
  })

  it('marks only the last open stay as current', () => {
    const result = layoutStays([stay(5), stay(5, { exitedDate: null })])

    expect(result.map((s) => s.current)).toEqual([false, true])
  })

  it('totals repeated visits to one status', () => {
    const result = layoutStays([stay(10), stay(20, { caseWorkflowStatusGuid: 'b' }), stay(30)])

    expect(result[0]).toMatchObject({ visits: 2, totalMinutes: 40 })
    expect(result[1]).toMatchObject({ visits: 1, totalMinutes: 20 })
  })
})

describe('orderStays', () => {
  it('sorts by entry time and keeps ties stable', () => {
    const ordered = orderStays([
      { enteredDate: '2026-10-03T00:00:00Z', id: 1 },
      { enteredDate: '2026-10-01T00:00:00Z', id: 2 },
      { enteredDate: '2026-10-01T00:00:00Z', id: 3 },
    ])

    expect(ordered.map((s) => s.id)).toEqual([2, 3, 1])
  })
})
