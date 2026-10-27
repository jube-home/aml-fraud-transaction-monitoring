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

import { areaById, familyById, familyOfArea } from './families'

export interface PerformanceAddress {
  family: string
  area: string
}

export const emptyAddress: PerformanceAddress = { family: '', area: '' }

export function normalise(address: PerformanceAddress): PerformanceAddress {
  const area = areaById(address.area) ? address.area : ''
  const family = area
    ? (familyOfArea(area)?.id ?? '')
    : familyById(address.family)
      ? address.family
      : ''

  return { family, area }
}

export function encodeAddress(address: PerformanceAddress): string {
  const clean = normalise(address)

  return [
    ...(clean.family ? [`family:${clean.family}`] : []),
    ...(clean.area ? [`area:${clean.area}`] : []),
  ].join(',')
}

export function decodeAddress(text: string | undefined): PerformanceAddress {
  const parts = Object.fromEntries(
    (text ?? '').split(',').flatMap((part): [string, string][] => {
      const index = part.indexOf(':')

      return index > 0 ? [[part.slice(0, index), part.slice(index + 1)]] : []
    }),
  )

  return normalise({ family: parts.family ?? '', area: parts.area ?? '' })
}

export function readAddress(query: Record<string, unknown>): PerformanceAddress {
  const raw = query.path
  const first = Array.isArray(raw) ? raw[0] : raw

  return decodeAddress(typeof first === 'string' ? first : undefined)
}

export function sameAddress(a: PerformanceAddress, b: PerformanceAddress): boolean {
  return a.family === b.family && a.area === b.area
}

export type PerformanceLevel = 'families' | 'areas' | 'grid'

export function levelOf(address: PerformanceAddress): PerformanceLevel {
  if (address.area) return 'grid'

  return address.family ? 'areas' : 'families'
}

export function levelsOf(address: PerformanceAddress): PerformanceLevel[] {
  const level = levelOf(address)

  if (level === 'families') return ['families']

  return level === 'areas' ? ['families', 'areas'] : ['families', 'areas', 'grid']
}

export function reopen(address: PerformanceAddress, index: number): PerformanceAddress {
  return index <= 0 ? emptyAddress : { family: address.family, area: '' }
}
