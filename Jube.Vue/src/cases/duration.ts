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

export type Translate = (key: string, params?: Record<string, unknown>) => string

export function durationText(
  t: Translate,
  minutes: number,
  number: (value: number) => string,
): string {
  const absolute = Math.abs(minutes)

  if (absolute < 60) return t('cases.time.minutes', { n: number(Math.round(absolute)) })
  if (absolute < 1440) return t('cases.time.hours', { n: number(Math.round(absolute / 6) / 10) })

  return t('cases.time.days', { n: number(Math.round(absolute / 144) / 10) })
}

export function relativeText(
  t: Translate,
  dueIso: string,
  nowMs: number,
  number: (value: number) => string,
): string {
  const minutes = (new Date(dueIso).getTime() - nowMs) / 60_000

  return minutes >= 0
    ? t('cases.time.in', { duration: durationText(t, minutes, number) })
    : t('cases.time.overdue', { duration: durationText(t, minutes, number) })
}
