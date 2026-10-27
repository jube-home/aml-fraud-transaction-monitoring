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

export interface SanctionParams {
  multiPartString: string
  distance: number
  maxDistanceRatio: number
  maxCoverageRatio: number
}

export interface SanctionEntry {
  id: number
  distance: number
  value: string | null
  sanctionEntrySourceId: number
  source: string | null
  reference: string | null
}

export interface SanctionAggregation {
  sum: number | null
  average: number | null
  count: number | null
  max: number | null
  min: number | null
  first: number | null
  last: number | null
  confidence: number | null
}

export interface SanctionSourceAggregation extends SanctionAggregation {
  sourceId: number
  sourceName: string | null
}

export interface SanctionResult {
  entries: SanctionEntry[]
  total: SanctionAggregation | null
  bySource: SanctionSourceAggregation[]
}

export const sanctionDefaults = {
  distance: 2,
  maxDistanceRatio: 0.3,
  maxCoverageRatio: 2,
} as const

export const sanctionLimits = {
  distance: { min: 0, max: 5, step: 1 },
  maxDistanceRatio: { min: 0, max: 1, step: 0.1 },
  maxCoverageRatio: { min: 0, max: 5, step: 1 },
} as const
