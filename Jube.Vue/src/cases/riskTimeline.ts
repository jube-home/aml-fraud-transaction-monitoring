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

export type TimelineUnit = 'hour' | 'day' | 'week' | 'month'

export interface TimelineTick {
  at: number
  x: number
}

export interface TimelineMarker {
  index: number
  at: number
  x: number
  lane: number
}

export interface TimelineLayout {
  unit: TimelineUnit
  width: number
  pxPerMs: number
  ticks: TimelineTick[]
  markers: TimelineMarker[]
  lanes: number
}

export interface TimelineOptions {
  minWidth: number
  maxWidth: number
  startPad: number
  endPad: number
  labelWidth: number
  gap: number
  minTickSpacing: number
}

export const defaultTimelineOptions: TimelineOptions = {
  minWidth: 480,
  maxWidth: 4800,
  startPad: 16,
  endPad: 136,
  labelWidth: 120,
  gap: 8,
  minTickSpacing: 56,
}

export const hourMs = 3_600_000
export const dayMs = 24 * hourMs
export const weekMs = 7 * dayMs

export const unitMs: Record<Exclude<TimelineUnit, 'month'>, number> = {
  hour: hourMs,
  day: dayMs,
  week: weekMs,
}

export const unitPx: Record<TimelineUnit, number> = {
  hour: 48,
  day: 64,
  week: 80,
  month: 96,
}

const monthApproxMs = 30 * dayMs
const maxUnitsPerSpan = 40

export const tickFormat: Record<TimelineUnit, Intl.DateTimeFormatOptions> = {
  hour: { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'UTC' },
  day: { day: 'numeric', month: 'short', timeZone: 'UTC' },
  week: { day: 'numeric', month: 'short', timeZone: 'UTC' },
  month: { month: 'short', year: 'numeric', timeZone: 'UTC' },
}

export function chooseUnit(spanMs: number): TimelineUnit {
  if (spanMs / hourMs <= maxUnitsPerSpan) return 'hour'
  if (spanMs / dayMs <= maxUnitsPerSpan) return 'day'
  if (spanMs / weekMs <= maxUnitsPerSpan) return 'week'

  return 'month'
}

function nominalMs(unit: TimelineUnit): number {
  return unit === 'month' ? monthApproxMs : unitMs[unit]
}

export function floorToUnit(at: number, unit: TimelineUnit): number {
  if (unit === 'hour') return Math.floor(at / hourMs) * hourMs
  if (unit === 'day') return Math.floor(at / dayMs) * dayMs
  if (unit === 'week') {
    const day = Math.floor(at / dayMs) * dayMs
    const weekday = (new Date(day).getUTCDay() + 6) % 7

    return day - weekday * dayMs
  }

  const d = new Date(at)

  return Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), 1)
}

export function nextTick(at: number, unit: TimelineUnit): number {
  if (unit !== 'month') return at + unitMs[unit]

  const d = new Date(at)

  return Date.UTC(d.getUTCFullYear(), d.getUTCMonth() + 1, 1)
}

export function tickTimes(start: number, end: number, unit: TimelineUnit): number[] {
  const times: number[] = []
  let at = floorToUnit(start, unit)

  if (at < start) at = nextTick(at, unit)

  while (at <= end) {
    times.push(at)
    at = nextTick(at, unit)
  }

  return times
}

export function assignLanes(xs: number[], labelWidth: number, gap: number): number[] {
  const laneEnds: number[] = []

  return xs.map((x) => {
    let lane = laneEnds.findIndex((end) => end <= x)

    if (lane === -1) {
      lane = laneEnds.length
      laneEnds.push(0)
    }

    laneEnds[lane] = x + labelWidth + gap

    return lane
  })
}

export function layoutTimeline(
  times: number[],
  options: TimelineOptions = defaultTimelineOptions,
  containerWidth = 0,
): TimelineLayout {
  const order = times
    .map((at, index) => ({ at, index }))
    .sort((a, b) => a.at - b.at || a.index - b.index)
  const start = order.length === 0 ? 0 : order[0]!.at
  const end = order.length === 0 ? 0 : order[order.length - 1]!.at
  const span = Math.max(end - start, hourMs)
  const unit = chooseUnit(span)
  const pads = options.startPad + options.endPad
  const natural = (span / nominalMs(unit)) * unitPx[unit]
  const floor = containerWidth > 0 ? 0 : options.minWidth - pads
  const clamped = Math.min(Math.max(natural, floor), options.maxWidth - pads)
  const fits = containerWidth > clamped + pads
  const inner = fits ? containerWidth - pads : clamped
  const pxPerMs = inner / span
  const width = fits ? Math.round(containerWidth) : Math.round(inner + pads)
  const xOf = (at: number) => Math.round(options.startPad + (at - start) * pxPerMs)

  const xs = order.map((entry) => xOf(entry.at))
  const lanes = assignLanes(xs, options.labelWidth, options.gap)

  const all = tickTimes(start, start + span, unit)
  const spacing = all.length > 1 ? xOf(all[1]!) - xOf(all[0]!) : options.minTickSpacing
  const stride = Math.max(1, Math.ceil(options.minTickSpacing / Math.max(spacing, 1)))

  return {
    unit,
    width,
    pxPerMs,
    ticks: all.filter((_, i) => i % stride === 0).map((at) => ({ at, x: xOf(at) })),
    markers: order.map((entry, i) => ({
      index: entry.index,
      at: entry.at,
      x: xs[i]!,
      lane: lanes[i]!,
    })),
    lanes: lanes.length === 0 ? 0 : Math.max(...lanes) + 1,
  }
}

export function scrollDelta(
  itemStart: number,
  itemEnd: number,
  viewStart: number,
  viewEnd: number,
): number {
  if (itemEnd - itemStart >= viewEnd - viewStart) return itemStart - viewStart
  if (itemStart < viewStart) return itemStart - viewStart
  if (itemEnd > viewEnd) return itemEnd - viewEnd

  return 0
}
