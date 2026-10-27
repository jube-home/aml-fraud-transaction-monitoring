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

export interface OverrideAddress {
  key: string
  value: string
  model: string
}

export type OverrideLevel = 'keys' | 'values' | 'models'

export const forceOverridePermission = 62
export const overridePermission = 2

function text(query: Record<string, unknown>, name: string): string {
  const raw = query[name]
  const first = Array.isArray(raw) ? raw[0] : raw

  return typeof first === 'string' ? first.trim() : ''
}

export function normalise(address: OverrideAddress): OverrideAddress {
  const key = address.key.trim()
  const value = key ? address.value.trim() : ''
  const model = value ? address.model.trim() : ''

  return { key, value, model }
}

export function readAddress(query: Record<string, unknown>): OverrideAddress {
  return normalise({
    key: text(query, 'key'),
    value: text(query, 'value'),
    model: text(query, 'model'),
  })
}

export function writeAddress(address: OverrideAddress): Record<string, string | undefined> {
  const clean = normalise(address)

  return {
    key: clean.key || undefined,
    value: clean.value || undefined,
    model: clean.model || undefined,
  }
}

export function sameAddress(a: OverrideAddress, b: OverrideAddress): boolean {
  return a.key === b.key && a.value === b.value && a.model === b.model
}

export function levelOf(address: OverrideAddress): OverrideLevel {
  if (!address.key) return 'keys'

  return address.value ? 'models' : 'values'
}

export function levelsOf(address: OverrideAddress): OverrideLevel[] {
  const level = levelOf(address)

  if (level === 'keys') return ['keys']

  return level === 'values' ? ['keys', 'values'] : ['keys', 'values', 'models']
}

export function reopen(address: OverrideAddress, index: number): OverrideAddress {
  if (index <= 0) return { key: '', value: '', model: '' }

  if (index === 1) return { key: address.key, value: '', model: '' }

  return address
}
