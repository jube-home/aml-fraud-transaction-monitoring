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

export const parameterType = {
  text: 1,
  integer: 2,
  decimal: 3,
  dateTime: 4,
  flag: 5,
} as const

export interface VisualisationRegistry {
  id: number
  guid: string
  name: string | null
  columns: number
  columnWidth: number
  rowHeight: number
}

export interface DirectoryEntry {
  id: number
  guid: string
  name: string | null
  contextKindId: number
}

export interface ParameterDefinition {
  id: number
  name: string | null
  dataTypeId: number
  defaultValue: string | null
  required: boolean
}

export interface DirectorySummary {
  registryId: number
  datasourceCount: number
  parameterCount: number
  displayKindIds: number[]
  executionsInWindow: number
  failuresInWindow: number
  averageResponseMilliseconds: number | null
  lastExecutionDate: string | null
  lastExecutionUser: string | null
  windowDays: number
}

export interface TileDefinition {
  id: number
  name: string | null
  displayKindId?: number
  columnSpan: number
  rowSpan: number
  priority: number
}

export type ParameterValues = Record<number, string>
