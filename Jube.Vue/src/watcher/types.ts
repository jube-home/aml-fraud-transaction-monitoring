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

export interface GeoPoint {
  latitude: number
  longitude: number
  createdAt: number
}

export interface Activation {
  id: string
  key: string
  keyValue: string
  entity: string
  longitude: number
  latitude: number
  located: boolean
  previous: GeoPoint | null
  ruleSummary: string
  responseElevation: number
  responseElevationContent: string
  responseElevationName: string
  backColor: string | null
  foreColor: string | null
  createdAt: number
  createdDate: string
  dim?: boolean
}

export type MapMode = 'latest' | 'all'

export interface WatcherOptions {
  mode: MapMode
  maxRows: number
  keyFilter?: string
  keyValueFilter?: string
}

export interface RuleSummary {
  name: string
  frequency: number
  aggregate: number
}

export interface WatcherState {
  history: Activation[]
  rows: Activation[]
  rules: Record<string, RuleSummary>
  ingested: number
}

export interface TrailSegment {
  entity: string
  from: GeoPoint
  to: Activation
  kilometres: number
  hours: number
  speedKmh: number
  impossible: boolean
}
