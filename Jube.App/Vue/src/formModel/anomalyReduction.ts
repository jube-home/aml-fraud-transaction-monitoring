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

export type AnomalyReduction = 'default' | 'none' | 'standard' | 'components'

export const anomalyReductions: readonly AnomalyReduction[] = [
  'default',
  'none',
  'standard',
  'components',
]

const presets = {
  none: {mode: 'None', scaling: 'None'},
  standard: {mode: 'Standard', scaling: 'Robust'},
  components: {mode: 'Standard', scaling: 'Robust', projection: 'ParallelAnalysis'},
} as const

function parsed(json: unknown): Record<string, unknown> {
  if (typeof json !== 'string' || json.trim() === '') return {}

  try {
    const value: unknown = JSON.parse(json)

    return typeof value === 'object' && value !== null && !Array.isArray(value)
      ? (value as Record<string, unknown>)
      : {}
  } catch {
    return {}
  }
}

export function reductionOf(json: unknown): AnomalyReduction {
  const reduction = parsed(json).reduction as
    { mode?: unknown; projection?: unknown } | null | undefined

  if (!reduction || typeof reduction !== 'object') return 'default'
  if (typeof reduction.projection === 'string' && reduction.projection !== 'None') {
    return 'components'
  }

  return reduction.mode === 'Standard' ? 'standard' : 'none'
}

export function withReduction(json: unknown, choice: AnomalyReduction): string {
  const next = {...parsed(json)}

  if (choice === 'default') delete next.reduction
  else next.reduction = presets[choice]

  return Object.keys(next).length === 0 ? '' : JSON.stringify(next)
}
