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

export interface FilterField {
  name: string
  dataType: string
}

export interface BacktestFields {
  filterFields: FilterField[]
  classFields: FilterField[]
}

export interface ValidationError {
  propertyName: string
  errorCode: string
  message: string
  line: number | null
}

export interface BacktestRequest {
  entityAnalysisModelId: number
  ruleType: string
  ruleId: number | null
  ruleText: string | null
  filterJson: string | null
  filterRuleText?: string | null
  classJson: string | null
  from: string | null
  to: string | null
  limit: number
  sampleSize: number
}

export interface BacktestSubmit {
  request: BacktestRequest
  windowInterval: string | null
  windowValue: number | null
}

export interface BacktestInstance {
  id: number
  ruleType: string
  ruleId: number | null
  status: string
  progress: number
  scanned: number
  evaluated: number
  createdDate: string
  createdUser?: string | null
  startedDate: string | null
  error: string | null
  request: BacktestRequest
  fired: number | null
  positives: number | null
  truePositives: number | null
  falsePositives: number | null
  falseNegatives: number | null
  trueNegatives: number | null
}

export interface BacktestSubmitResult {
  valid: boolean
  instance: BacktestInstance | null
  errors: ValidationError[]
}

export interface BacktestSample {
  entityAnalysisModelInstanceEntryGuid: string
  entryKeyValue: string | null
  referenceDate: string | null
  fired: boolean
  positive: boolean
  error: string | null
}

export interface BacktestTagCount {
  name: string
  count: number
}

export interface BacktestResult {
  valid: boolean
  scanned: number
  filteredOut: number
  filterErrors: number
  evaluated: number
  limitReached: boolean
  earliestReferenceDate: string | null
  latestReferenceDate: string | null
  fired: number
  notFired: number
  runtimeErrors: number
  aborted: boolean
  abortReason: string | null
  classDefined: boolean
  positives: number
  falsePositives: number
  falseNegatives: number
  trueNegatives: number
  truePositives: number
  durationMicroseconds: number
  tagsInSample: BacktestTagCount[]
  truePositiveSamples: BacktestSample[]
  falsePositiveSamples: BacktestSample[]
  falseNegativeSamples: BacktestSample[]
  errorSamples: BacktestSample[]
  errors: ValidationError[]
}

export interface BacktestValue {
  name: string
  value: string | null
}

export interface BacktestExplanation {
  valid: boolean
  passedFilter: boolean
  filterError: string | null
  fired: boolean
  ruleError: string | null
  positive: boolean | null
  tags: string[]
  values: BacktestValue[]
  errors: ValidationError[]
}

export interface ModelEntity {
  kind: string
  name: string
  active: boolean
}

export interface ModelDependency {
  dependent: ModelEntity
  target: ModelEntity | null
  dependencyKind: string
  name: string
  line: number | null
  depth: number
  via: string | null
}

export interface DependencyList {
  entity: ModelEntity | null
  dependencies: ModelDependency[]
}

export interface DependencyImpact {
  entity: ModelEntity | null
  dependents: ModelDependency[]
}
