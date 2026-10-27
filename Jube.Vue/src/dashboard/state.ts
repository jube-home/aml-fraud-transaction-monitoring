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

import {
  parameterType,
  type ParameterDefinition,
  type ParameterValues,
  type TileDefinition,
  type VisualisationRegistry,
} from './types'
import type { CardParameter } from '../datasource/types'

const dayMilliseconds = 24 * 60 * 60 * 1000

export function localInput(date: Date): string {
  const pad = (value: number): string => String(value).padStart(2, '0')

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function initialValue(definition: ParameterDefinition, now: Date): string {
  const text = definition.defaultValue ?? ''

  switch (definition.dataTypeId) {
    case parameterType.dateTime: {
      const days = Number.parseInt(text, 10)

      return localInput(
        new Date(now.getTime() - (Number.isFinite(days) ? days : 0) * dayMilliseconds),
      )
    }
    case parameterType.flag:
      return text === '1' || text.toLowerCase() === 'true' ? '1' : '0'
    default:
      return text
  }
}

export function initialValues(
  definitions: ParameterDefinition[],
  now: Date,
  overrides: Record<string, string> = {},
): ParameterValues {
  const values: ParameterValues = {}

  for (const definition of definitions) {
    values[definition.id] = overrides[definition.name ?? ''] ?? initialValue(definition, now)
  }

  return values
}

export function missingRequired(
  definitions: ParameterDefinition[],
  values: ParameterValues,
): number[] {
  return definitions
    .filter(
      (definition) =>
        definition.required &&
        definition.dataTypeId !== parameterType.flag &&
        (values[definition.id] ?? '').trim() === '',
    )
    .map((definition) => definition.id)
}

export function wireValue(definition: ParameterDefinition, value: string): string {
  if (definition.dataTypeId === parameterType.dateTime && value !== '') {
    const parsed = new Date(value)

    return Number.isNaN(parsed.getTime()) ? value : parsed.toISOString()
  }

  return value
}

export function cardParameters(
  definitions: ParameterDefinition[],
  values: ParameterValues,
): CardParameter[] {
  return definitions.map((definition) => ({
    id: definition.id,
    value: wireValue(definition, values[definition.id] ?? ''),
  }))
}

export function orderedTiles(tiles: TileDefinition[]): TileDefinition[] {
  return [...tiles].sort((a, b) => a.priority - b.priority || a.id - b.id)
}

export function gridStyle(registry: VisualisationRegistry): Record<string, string> {
  const columns = Math.max(registry.columns, 1)
  const width = Math.max(registry.columnWidth, 120)

  return {
    '--dash-columns': `repeat(${columns}, minmax(0, ${width}px))`,
    '--dash-rows': `${Math.max(registry.rowHeight, 80)}px`,
  }
}

export function tileStyle(
  registry: VisualisationRegistry,
  tile: TileDefinition,
): Record<string, string> {
  return {
    '--tile-columns': String(Math.min(Math.max(tile.columnSpan, 1), Math.max(registry.columns, 1))),
    '--tile-rows': String(Math.max(tile.rowSpan, 1)),
    '--tile-height': `${tileSize(registry, tile).height}px`,
  }
}

export function tileSize(
  registry: VisualisationRegistry,
  tile: TileDefinition,
): { width: number; height: number } {
  return {
    width: Math.max(tile.columnSpan, 1) * Math.max(registry.columnWidth, 120),
    height: Math.max(tile.rowSpan, 1) * Math.max(registry.rowHeight, 80),
  }
}
