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
  assignLanes,
  chooseUnit,
  dayMs,
  defaultTimelineOptions,
  floorToUnit,
  hourMs,
  layoutTimeline,
  scrollDelta,
  tickTimes,
  weekMs,
} from '../riskTimeline'

const base = Date.UTC(2026, 9, 5, 0, 0)

describe('unit choice', () => {
  it('moves up as the span grows', () => {
    expect(chooseUnit(10 * hourMs)).toBe('hour')
    expect(chooseUnit(10 * dayMs)).toBe('day')
    expect(chooseUnit(100 * dayMs)).toBe('week')
    expect(chooseUnit(900 * dayMs)).toBe('month')
  })
})

describe('tick alignment', () => {
  it('floors weeks to Monday and months to the first', () => {
    const wednesday = Date.UTC(2026, 9, 7, 15, 30)

    expect(new Date(floorToUnit(wednesday, 'week')).toISOString()).toBe('2026-10-05T00:00:00.000Z')
    expect(new Date(floorToUnit(wednesday, 'month')).toISOString()).toBe('2026-10-01T00:00:00.000Z')
    expect(new Date(floorToUnit(wednesday, 'day')).toISOString()).toBe('2026-10-07T00:00:00.000Z')
    expect(new Date(floorToUnit(wednesday, 'hour')).toISOString()).toBe('2026-10-07T15:00:00.000Z')
  })

  it('lists ticks inside the range only', () => {
    const ticks = tickTimes(base + hourMs / 2, base + 3 * hourMs, 'hour')

    expect(ticks).toEqual([base + hourMs, base + 2 * hourMs, base + 3 * hourMs])
  })

  it('steps months by calendar', () => {
    const ticks = tickTimes(Date.UTC(2026, 0, 15), Date.UTC(2026, 3, 2), 'month')

    expect(ticks.map((t) => new Date(t).toISOString().slice(0, 10))).toEqual([
      '2026-02-01',
      '2026-03-01',
      '2026-04-01',
    ])
  })
})

describe('layout scale', () => {
  it('is linear in time', () => {
    const layout = layoutTimeline([base, base + 2 * dayMs, base + 6 * dayMs])
    const [a, b, c] = layout.markers

    expect(layout.unit).toBe('day')
    expect((c!.x - b!.x) / (b!.x - a!.x)).toBeCloseTo(2, 1)
  })

  it('keeps input index and sorts by time', () => {
    const layout = layoutTimeline([base + dayMs, base])

    expect(layout.markers.map((m) => m.index)).toEqual([1, 0])
    expect(layout.markers[0]!.x).toBe(defaultTimelineOptions.startPad)
  })

  it('never goes below the minimum width and stays linear', () => {
    const layout = layoutTimeline([base, base + 3 * hourMs])

    expect(layout.width).toBe(defaultTimelineOptions.minWidth)
    expect(layout.markers[1]!.x - layout.markers[0]!.x).toBeGreaterThan(100)
  })

  it('never goes above the maximum width', () => {
    const layout = layoutTimeline([base, base + 3000 * dayMs])

    expect(layout.width).toBeLessThanOrEqual(defaultTimelineOptions.maxWidth)
    expect(layout.unit).toBe('month')
  })

  it('thins ticks so they never crowd', () => {
    const layout = layoutTimeline([base, base + 3000 * dayMs])
    const gaps = layout.ticks.slice(1).map((tick, i) => tick.x - layout.ticks[i]!.x)

    expect(Math.min(...gaps)).toBeGreaterThanOrEqual(defaultTimelineOptions.minTickSpacing * 0.5)
  })

  it('handles one event and none', () => {
    expect(layoutTimeline([base]).markers).toHaveLength(1)
    expect(layoutTimeline([])).toMatchObject({ markers: [], lanes: 0 })
  })

  it('handles identical timestamps', () => {
    const layout = layoutTimeline([base, base, base])

    expect(layout.markers.map((m) => m.lane)).toEqual([0, 1, 2])
    expect(layout.lanes).toBe(3)
  })

  it('uses weeks for a quarter', () => {
    expect(layoutTimeline([base, base + 12 * weekMs]).unit).toBe('week')
  })
})

describe('lanes', () => {
  it('staggers labels that would overlap and reuses a lane once free', () => {
    expect(assignLanes([0, 50, 100, 400], 120, 8)).toEqual([0, 1, 2, 0])
  })

  it('keeps clear labels on one lane', () => {
    expect(assignLanes([0, 128, 300], 120, 8)).toEqual([0, 0, 0])
  })

  it('never lets two markers in one lane overlap', () => {
    const times = [0, 1, 2, 5, 6, 30, 31, 32, 90].map((d) => base + d * hourMs * 6)
    const layout = layoutTimeline(times)

    for (const a of layout.markers) {
      for (const b of layout.markers) {
        if (a !== b && a.lane === b.lane) {
          expect(Math.abs(a.x - b.x)).toBeGreaterThanOrEqual(defaultTimelineOptions.labelWidth)
        }
      }
    }
  })
})

describe('fit to container', () => {
  const short = [base, base + 2 * dayMs, base + 6 * dayMs]
  const pads = defaultTimelineOptions.startPad + defaultTimelineOptions.endPad

  it('stretches a short span uniformly to fill the container exactly', () => {
    const natural = layoutTimeline(short)
    const fitted = layoutTimeline(short, defaultTimelineOptions, 1200)

    expect(fitted.width).toBe(1200)
    expect(fitted.unit).toBe(natural.unit)

    const [a, b, c] = fitted.markers

    expect(a!.x).toBe(defaultTimelineOptions.startPad)
    expect(c!.x).toBe(1200 - defaultTimelineOptions.endPad)
    expect((c!.x - b!.x) / (b!.x - a!.x)).toBeCloseTo(2, 1)
  })

  it('does not stretch when the natural width already exceeds the container', () => {
    const natural = layoutTimeline(short)
    const fitted = layoutTimeline(short, defaultTimelineOptions, natural.width - 10)

    expect(fitted.width).toBe(natural.width)
    expect(fitted.pxPerMs).toBe(natural.pxPerMs)
  })

  it('keeps a long span at its natural scale so the scroller scrolls', () => {
    const long = [base, base + 800 * dayMs]
    const fitted = layoutTimeline(long, defaultTimelineOptions, 600)

    expect(fitted.unit).toBe('month')
    expect(fitted.width).toBeGreaterThan(600)
    expect(fitted.width).toBe(layoutTimeline(long).width)
  })

  it('refits when the container changes', () => {
    const narrow = layoutTimeline(short, defaultTimelineOptions, 700)
    const wide = layoutTimeline(short, defaultTimelineOptions, 1400)

    expect(narrow.width).toBe(700)
    expect(wide.width).toBe(1400)
    expect(wide.pxPerMs / narrow.pxPerMs).toBeCloseTo((1400 - pads) / (700 - pads), 3)
  })

  it('keeps ticks spaced and lanes clear after stretching', () => {
    const times = [0, 1, 2, 5, 6, 30, 31, 32, 90].map((d) => base + d * hourMs * 6)
    const layout = layoutTimeline(times, defaultTimelineOptions, 3000)
    const gaps = layout.ticks.slice(1).map((tick, i) => tick.x - layout.ticks[i]!.x)

    expect(layout.width).toBe(3000)
    expect(Math.min(...gaps)).toBeGreaterThanOrEqual(defaultTimelineOptions.minTickSpacing * 0.5)

    for (const a of layout.markers) {
      for (const b of layout.markers) {
        if (a !== b && a.lane === b.lane) {
          expect(Math.abs(a.x - b.x)).toBeGreaterThanOrEqual(defaultTimelineOptions.labelWidth)
        }
      }
    }
  })

  it('fills a container narrower than the unmeasured minimum width', () => {
    const tiny = [base, base + 2 * hourMs]

    expect(layoutTimeline(tiny).width).toBe(defaultTimelineOptions.minWidth)
    expect(layoutTimeline(tiny, defaultTimelineOptions, 360).width).toBe(360)
  })

  it('ignores an unmeasured container', () => {
    expect(layoutTimeline(short, defaultTimelineOptions, 0).width).toBe(layoutTimeline(short).width)
  })
})

describe('scrollDelta', () => {
  it('is zero when the item is inside the view', () => {
    expect(scrollDelta(120, 240, 100, 500)).toBe(0)
  })

  it('moves back or forward by the overhang', () => {
    expect(scrollDelta(40, 160, 100, 500)).toBe(-60)
    expect(scrollDelta(450, 600, 100, 500)).toBe(100)
  })

  it('aligns the start when the item is wider than the view', () => {
    expect(scrollDelta(80, 700, 100, 500)).toBe(-20)
  })
})
