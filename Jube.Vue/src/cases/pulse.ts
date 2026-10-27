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

export interface SparkPaths {
  line: string
  area: string
}

export type DayState = 'none' | 'started' | 'toGo' | 'met'

export function sparkPaths(
  values: readonly number[],
  width = 100,
  height = 24,
  pad = 2,
): SparkPaths {
  const floor = height - pad

  if (values.length === 0) return { line: `M0 ${floor}H${width}`, area: '' }

  const top = Math.max(...values)
  const bottom = Math.min(...values)
  const span = top - bottom
  const step = values.length > 1 ? width / (values.length - 1) : 0

  const points = values.map((value, index) => {
    const x = values.length > 1 ? index * step : width / 2
    const y = span === 0 ? height / 2 : floor - ((value - bottom) / span) * (floor - pad)

    return `${round(x)} ${round(y)}`
  })

  const line = `M${points.join('L')}`
  const last = values.length > 1 ? width : width / 2
  const first = values.length > 1 ? 0 : width / 2

  return { line, area: `${line}L${round(last)} ${floor}L${round(first)} ${floor}Z` }
}

function round(value: number): number {
  return Math.round(value * 10) / 10
}

export function ringValue(closed: number, goal: number | null): number {
  if (goal === null || goal <= 0) return closed > 0 ? 100 : 0

  return Math.min(100, Math.round((closed / goal) * 100))
}

export function dayState(closed: number, goal: number | null): DayState {
  if (goal === null) return closed > 0 ? 'started' : 'none'

  return closed >= goal ? 'met' : closed > 0 ? 'toGo' : 'none'
}

export function againstTypical(closed: number, typical: number): number {
  return Math.round(closed - typical)
}
