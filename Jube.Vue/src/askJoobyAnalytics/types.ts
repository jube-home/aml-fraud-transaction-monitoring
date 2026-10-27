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
export interface AnalyticsInstance {
  id: number
  guid: string
  name: string | null
  entityAnalysisModelId: number | null
  responsePayload: boolean
  reportTable: boolean
  active: boolean
  createdUser: string | null
  createdDate: string | null
  deletedUser: string | null
  version: number
}

export interface TrainedModel {
  id: number
  askJoobyAnalyticsInstanceId: number
  name: string | null
  kind: number
  source: number
  active: boolean
  activeVersionId: number | null
  activeVersionNumber: number | null
  createdDate: string | null
}

export interface ModelVersion {
  id: number
  trainedModelId: number
  versionNumber: number
  isActive: boolean
  createdDate: string | null
  testRows: number | null
  testRocAuc: number | null
  testAveragePrecision: number | null
  testLogLoss: number | null
  testBrierScore: number | null
  testExpectedCalibrationError: number | null
  warningCodes: string[]
  transformedVariableCount: string | null
  evidence?: unknown
}

export interface PromotionWarning {
  code: string
  message: string
}

export interface PromotionEvidence {
  versionId: number
  versionNumber: number
  trainedModelId: number
  modelName: string | null
  isActiveVersion: boolean
  instanceAllocated: boolean
  allocatedEntityAnalysisModelId: number | null
  kind: string
  seed: number
  warnings: PromotionWarning[]
}

export interface MetricComparison {
  name: string
  valueA: number | null
  valueB: number | null
  difference: number | null
}

export interface VersionComparison {
  versionA: ModelVersion
  versionB: ModelVersion
  sameSeed: boolean
  sameSplitSizes: boolean
  sameVariables: boolean
  metrics: MetricComparison[]
  warningsOnlyInA: string[]
  warningsOnlyInB: string[]
}

export interface AnalyticsNarrative {
  holistic?: string[]
  groups?: Record<string, string[]>
}

export interface ModelName {
  id: number
  name: string
}
