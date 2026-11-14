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

import {safeAddress} from './noteGroups'

export const maximumSources = 10
export const maximumTitleLength = 300
export const maximumAddressLength = 2000

export interface SourceRow {
  key: number
  title: string
  url: string
  retrieved: string
}

export interface Citation {
  title: string | null
  url: string
  retrievedDate: string
}

let counter = 0

export function today(now: Date = new Date()): string {
  return now.toISOString().slice(0, 10)
}

export function emptySource(retrieved: string = today()): SourceRow {
  counter += 1

  return {key: counter, title: '', url: '', retrieved}
}

export function isBlank(row: SourceRow): boolean {
  return row.title.trim() === '' && row.url.trim() === ''
}

export function isValidAddress(url: string): boolean {
  const trimmed = url.trim()

  return (
    trimmed.length > 0 &&
    trimmed.length <= maximumAddressLength &&
    !/[\s<>"'`\\]/u.test(trimmed) &&
    safeAddress(trimmed) !== null &&
    !/^[a-z][a-z0-9+.-]*:\/\/[^/]*@/iu.test(trimmed)
  )
}

export function isValidSource(row: SourceRow): boolean {
  return isValidAddress(row.url) && row.title.trim().length <= maximumTitleLength
}

export function sourcesValid(rows: SourceRow[]): boolean {
  return rows.filter((row) => !isBlank(row)).every(isValidSource)
}

export function toCitations(rows: SourceRow[]): Citation[] {
  return rows
    .filter((row) => !isBlank(row))
    .map((row) => ({
      title: row.title.trim() === '' ? null : row.title.trim(),
      url: row.url.trim(),
      retrievedDate: new Date(`${row.retrieved || today()}T00:00:00Z`).toISOString(),
    }))
}

export function researchHost(url: string): string {
  try {
    return new URL(url).host
  } catch {
    return ''
  }
}
