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
export interface ExhaustiveInstance {
  id: number
  guid: string
  name: string | null
  statusId: number
  models: number
  modelsSinceBest: number
  score: number
  topologyComplexity: number
  anomalySummary: string | null
  completedDate: string | null
  updatedDate: string | null
  createdDate: string | null
  createdUser: string | null
  dataSourceId: number | null
  filter: boolean
  anomaly: boolean
  bootstrap: boolean
}

export interface NarrativeResult {
  holistic: string[]
  groups: Record<string, string[]>
}

export interface TrainingVariable {
  id: number
  name: string | null
  mean: number
  standardDeviation: number
  maximum: number
  minimum: number
  kurtosis: number
  skewness: number
  iqr: number
  normalisationType: string | null
  distinctValues: number
  correlation: number
  correlationAbsRank: number
  categoricalSourceName: string | null
  categoricalLevel: string | null
}

export interface VarianceRow {
  name: string | null
  correlation: number
  correlationAbsRank: number
}

export interface PromotedModel {
  id: number
  exhaustiveSearchInstanceTrialInstanceId: number
  active: boolean
  score: number
  topologyComplexity: number
  kind: string | null
  createdDate: string
  calibrationMethod: string | null
}

export interface PrescriptionRow {
  id: number
  name: string | null
  variableMean: number
  variableStandardDeviation: number
  variableMaximum: number
  variableMinimum: number
  prescriptionMean: number
  prescriptionStandardDeviation: number
  prescriptionMaximum: number
  prescriptionMinimum: number
  sensitivity: number
  positive: boolean | null
  promotedTrialInstanceVariableId: number
}

export interface Prescription {
  id: number
  name: string
  variableMean: number
  variableStandardDeviation: number
  variableMaximum: number
  variableMinimum: number
  sensitivity: number
  chartId: number
  positivePrescriptionMean?: number
  positivePrescriptionStandardDeviation?: number
  negativePrescriptionMean?: number
  negativePrescriptionStandardDeviation?: number
}

export interface Trial {
  id: number
  createdDate: string
  kind: string | null
  score: number | null
  promoted: boolean
}

export interface TrialDetail {
  id: number
  kind: string | null
  score: number | null
  hasDiagram: boolean
  evidence: unknown
}

export interface SliderVariable {
  id: number
  name: string | null
  mean: number
  maximum: number
  minimum: number
  emptyRange: boolean
  categoricalSourceName: string | null
  categoricalKind: number | null
  categoricalLevel: string | null
  categoricalReferenceLabel: string | null
}

export interface NumericControl {
  kind: 'number'
  name: string
  min: number
  max: number
  step: number
  value: number
}

export interface CategoricalControl {
  kind: 'category'
  name: string
  reference: string
  options: { text: string; value: string }[]
}

export type Control = NumericControl | CategoricalControl

export interface RecallResult {
  score: number
  baseValue: number
  nodeRelevance: number[][]
}

export interface BatchRow {
  rowNumber: number
  score: number | null
  baseValue: number | null
  error: string | null
}

export interface BatchResult {
  rowCount: number
  scoredCount: number
  unmappedVariables: string[]
  warnings: string[]
  rows: BatchRow[]
}
