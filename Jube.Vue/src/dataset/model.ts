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
export type DatasetKind = 'Csv' | 'Json'

export interface DatasetDistinctValue {
  value: string | null
  count: number
}

export interface DatasetColumn {
  index: number
  header: string | null
  kind: string | null
  missingCount: number
  min: number | null
  max: number | null
  isCategorical: boolean
  include: boolean
  name: string | null
  processingTypeId: number
  distinctValues: DatasetDistinctValue[]
}

export interface Dataset {
  id: number
  name: string | null
  kind: DatasetKind
  fileName: string | null
  rowCount: number
  sizeBytes: number
  columnCount: number
  statusId: number
  delimiter: string | null
  columns: DatasetColumn[]
  warnings: string[]
}

export interface DatasetSummary {
  id: number
  name: string | null
  kind: DatasetKind
  statusId: number
}

export interface ColumnMapping {
  index: number
  include: boolean
  name: string
  processingTypeId: number
}

export interface DatasetMapping {
  id: number
  columns: ColumnMapping[]
}

export type MappingProblem =
  { code: 'none' } | { code: 'name'; index: number } | { code: 'duplicate'; index: number }

export const datasetConfirmed = 1

export const processingTypes = [
  { id: 1, key: 'payload' },
  { id: 2, key: 'dictionary' },
  { id: 3, key: 'ttlCounter' },
  { id: 4, key: 'sanction' },
  { id: 5, key: 'abstraction' },
  { id: 6, key: 'abstractionCalculation' },
] as const

const variableName = /^[A-Za-z][A-Za-z0-9_]*$/

export function reference(dataset: { kind: DatasetKind; id: number }): string {
  return `${dataset.kind}:${dataset.id}`
}

export function parseReference(value: string): { kind: DatasetKind; id: number } | null {
  const [kind, id] = value.split(':')
  const number = Number(id)

  return (kind === 'Csv' || kind === 'Json') && Number.isInteger(number) && number > 0
    ? { kind, id: number }
    : null
}

export function kindOfFile(name: string): DatasetKind {
  return /\.json$/i.test(name) ? 'Json' : 'Csv'
}

export function canInclude(column: Pick<DatasetColumn, 'kind' | 'isCategorical'>): boolean {
  if (column.kind === 'Numeric') return true

  return (column.kind === 'Text' || column.kind === 'Mixed') && column.isCategorical
}

export function initialMapping(columns: DatasetColumn[]): ColumnMapping[] {
  return columns.map((column) => ({
    index: column.index,
    include: column.include,
    name: column.name ?? '',
    processingTypeId: column.processingTypeId || 1,
  }))
}

export function mappingProblems(mapping: ColumnMapping[]): MappingProblem[] {
  const included = mapping.filter((column) => column.include)
  const problems: MappingProblem[] = []

  if (included.length === 0) problems.push({ code: 'none' })

  const seen = new Set<string>()

  for (const column of included) {
    if (!variableName.test(column.name)) {
      problems.push({ code: 'name', index: column.index })
      continue
    }

    const key = column.name.toLowerCase()

    if (seen.has(key)) problems.push({ code: 'duplicate', index: column.index })

    seen.add(key)
  }

  return problems
}

export function confirmBody(
  dataset: Pick<Dataset, 'id'>,
  mapping: ColumnMapping[],
): DatasetMapping {
  return {
    id: dataset.id,
    columns: mapping.map((column) => ({
      index: column.index,
      include: column.include,
      name: column.name,
      processingTypeId: Number(column.processingTypeId),
    })),
  }
}

export function pickable(datasets: DatasetSummary[]): DatasetSummary[] {
  return datasets.filter((dataset) => dataset.statusId === datasetConfirmed)
}
