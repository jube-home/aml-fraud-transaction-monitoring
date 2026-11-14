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

export const laneLetters = ['A', 'B', 'C', 'D'] as const

const laneClasses: Record<string, string> = {
  A: 'border-[oklch(0.62_0.07_250)] bg-[oklch(0.95_0.02_250)] text-[oklch(0.42_0.07_250)]',
  B: 'border-[oklch(0.62_0.07_160)] bg-[oklch(0.95_0.02_160)] text-[oklch(0.42_0.07_160)]',
  C: 'border-[oklch(0.62_0.07_70)] bg-[oklch(0.95_0.02_70)] text-[oklch(0.42_0.07_70)]',
  D: 'border-[oklch(0.62_0.07_320)] bg-[oklch(0.95_0.02_320)] text-[oklch(0.42_0.07_320)]',
}

export function laneClass(letter: string): string {
  return laneClasses[letter] ?? laneClasses.A!
}

export function lettersOf(value: unknown): string[] {
  return typeof value === 'string'
    ? [...value].filter((letter) => laneLetters.includes(letter as 'A'))
    : []
}

export const laneOperators = ['eq', 'gt', 'ge', 'lt', 'le', 'between'] as const

export type LaneOperator = (typeof laneOperators)[number]

export const rangeDataTypes = [2, 3, 4, 6, 7] as const

export function isRangeType(dataTypeId: number | undefined): boolean {
  return dataTypeId !== undefined && (rangeDataTypes as readonly number[]).includes(dataTypeId)
}

export function isLaneOperator(value: unknown): value is LaneOperator {
  return typeof value === 'string' && (laneOperators as readonly string[]).includes(value)
}

const operatorSymbols: Record<LaneOperator, string> = {
  eq: '=',
  gt: '>',
  ge: '\u2265',
  lt: '<',
  le: '\u2264',
  between: '\u2013',
}

export function laneValueText(lane: { value: string; op?: LaneOperator; to?: string }): string {
  const op = lane.op ?? 'eq'

  if (op === 'eq') return lane.value
  if (op === 'between') return `${lane.value} ${operatorSymbols.between} ${lane.to ?? ''}`

  return `${operatorSymbols[op]} ${lane.value}`
}

export function sameLane(
  a: { key: string; value: string; op?: LaneOperator; to?: string },
  b: { key: string; value: string; op?: LaneOperator; to?: string },
): boolean {
  return (
    a.key === b.key &&
    a.value === b.value &&
    (a.op ?? 'eq') === (b.op ?? 'eq') &&
    (a.to ?? '') === (b.to ?? '')
  )
}
