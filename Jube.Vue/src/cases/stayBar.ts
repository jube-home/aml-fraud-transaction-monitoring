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

export interface StayInput {
  caseWorkflowStatusGuid: string
  enteredDate: string
  exitedDate: string | null
  minutes: number
  maxMinutes: number | null
}

export interface StaySegment {
  minutes: number
  percent: number
  current: boolean
  breach: boolean
  overMinutes: number
  used: number | null
  visits: number
  totalMinutes: number
}

export const defaultMinPercent = 5

export function orderStays<T extends { enteredDate: string }>(stays: T[]): T[] {
  return stays
    .map((stay, index) => ({ stay, index }))
    .sort(
      (a, b) =>
        Date.parse(a.stay.enteredDate) - Date.parse(b.stay.enteredDate) || a.index - b.index,
    )
    .map((entry) => entry.stay)
}

export function sharePercents(minutes: number[], minPercent: number = defaultMinPercent): number[] {
  const count = minutes.length

  if (count === 0) return []

  const weights = minutes.map((m) => Math.max(0, Number.isFinite(m) ? m : 0))
  const total = weights.reduce((sum, w) => sum + w, 0)

  if (total === 0 || count * minPercent >= 100) return weights.map(() => 100 / count)

  const fixed = new Set<number>()
  let result = weights.map(() => 0)

  for (;;) {
    const free = weights.map((_, i) => i).filter((i) => !fixed.has(i))
    const freeTotal = free.reduce((sum, i) => sum + weights[i]!, 0)
    const room = 100 - fixed.size * minPercent
    const scale = freeTotal === 0 ? 0 : room / freeTotal

    result = weights.map((w, i) => (fixed.has(i) ? minPercent : w * scale))

    const under = free.filter((i) => result[i]! < minPercent)

    if (under.length === 0) return result

    under.forEach((i) => fixed.add(i))
  }
}

export function layoutStays(
  stays: StayInput[],
  minPercent: number = defaultMinPercent,
): StaySegment[] {
  const percents = sharePercents(
    stays.map((stay) => stay.minutes),
    minPercent,
  )

  let currentAt = -1

  stays.forEach((stay, index) => {
    if (stay.exitedDate === null) currentAt = index
  })

  const totals = new Map<string, { minutes: number; visits: number }>()

  for (const stay of stays) {
    const entry = totals.get(stay.caseWorkflowStatusGuid) ?? { minutes: 0, visits: 0 }

    entry.minutes += stay.minutes
    entry.visits += 1
    totals.set(stay.caseWorkflowStatusGuid, entry)
  }

  return stays.map((stay, index) => {
    const limit = stay.maxMinutes
    const overMinutes = limit !== null && stay.minutes > limit ? stay.minutes - limit : 0
    const total = totals.get(stay.caseWorkflowStatusGuid)!

    return {
      minutes: stay.minutes,
      percent: percents[index]!,
      current: index === currentAt,
      breach: overMinutes > 0,
      overMinutes,
      used: limit !== null && limit > 0 ? Math.min(stay.minutes, limit) : null,
      visits: total.visits,
      totalMinutes: total.minutes,
    }
  })
}
