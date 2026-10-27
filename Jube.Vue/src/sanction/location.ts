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

import { sanctionDefaults, sanctionLimits, type SanctionParams } from './types'

export type SanctionLocationQuery = Record<string, unknown>

function first(value: unknown): string | undefined {
  const single = Array.isArray(value) ? value[0] : value

  return typeof single === 'string' ? single : undefined
}

function bounded(
  raw: string | undefined,
  limits: { min: number; max: number },
  fallback: number,
): number {
  if (raw === undefined || raw.trim() === '') return fallback

  const parsed = Number(raw)

  if (!Number.isFinite(parsed)) return fallback

  return Math.min(limits.max, Math.max(limits.min, parsed))
}

export function paramsFromLocation(query: SanctionLocationQuery): SanctionParams {
  return {
    multiPartString: first(query.q) ?? '',
    distance: bounded(first(query.d), sanctionLimits.distance, sanctionDefaults.distance),
    maxDistanceRatio: bounded(
      first(query.r),
      sanctionLimits.maxDistanceRatio,
      sanctionDefaults.maxDistanceRatio,
    ),
    maxCoverageRatio: bounded(
      first(query.c),
      sanctionLimits.maxCoverageRatio,
      sanctionDefaults.maxCoverageRatio,
    ),
  }
}

export function locationFromParams(params: SanctionParams): Record<string, string> {
  const text = params.multiPartString.trim()

  if (text === '') return {}

  const out: Record<string, string> = { q: text }

  if (params.distance !== sanctionDefaults.distance) out.d = String(params.distance)

  if (params.maxDistanceRatio !== sanctionDefaults.maxDistanceRatio) {
    out.r = String(params.maxDistanceRatio)
  }

  if (params.maxCoverageRatio !== sanctionDefaults.maxCoverageRatio) {
    out.c = String(params.maxCoverageRatio)
  }

  return out
}

export function locationKey(params: SanctionParams): string {
  return JSON.stringify(locationFromParams(params))
}
