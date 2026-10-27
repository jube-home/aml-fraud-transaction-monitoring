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

import type {
  BacktestInstance,
  BacktestResult,
  BacktestSubmit,
  ModelDependency,
  ValidationError,
} from './types'
import type { BuilderGroup } from '../ruleBuilder/types'

export interface BacktestWindow {
  key: 'week' | 'month' | 'quarter' | 'year' | 'all'
  interval: string | null
  amount: number | null
}

export const backtestWindows: BacktestWindow[] = [
  { key: 'week', interval: 'd', amount: 7 },
  { key: 'month', interval: 'd', amount: 30 },
  { key: 'quarter', interval: 'd', amount: 90 },
  { key: 'year', interval: 'm', amount: 12 },
  { key: 'all', interval: null, amount: null },
]

export const defaultBacktest = { window: 'month', limit: 100000, sampleSize: 10 } as const

export function ratio(numerator: number, denominator: number): number | null {
  return denominator > 0 ? numerator / denominator : null
}

export function builderJson(group: BuilderGroup | null): string | null {
  return group && group.rules.length > 0 ? JSON.stringify(group) : null
}

export interface SubmitInput {
  modelId: number
  ruleType: string
  ruleId: number | null
  ruleText: string
  filter: BuilderGroup | null
  positive: BuilderGroup | null
  window: BacktestWindow
  limit: number | null
  sampleSize: number | null
}

export function submitBody(input: SubmitInput): BacktestSubmit {
  return {
    request: {
      entityAnalysisModelId: input.modelId,
      ruleType: input.ruleType,
      ruleId: input.ruleId,
      ruleText: input.ruleText,
      filterJson: builderJson(input.filter),
      classJson: builderJson(input.positive),
      from: null,
      to: null,
      limit: input.limit && input.limit > 0 ? input.limit : defaultBacktest.limit,
      sampleSize: input.sampleSize && input.sampleSize > 0 ? input.sampleSize : 0,
    },
    windowInterval: input.window.interval,
    windowValue: input.window.amount,
  }
}

export function validationText(errors: ValidationError[]): string {
  return errors
    .map(
      (error) =>
        `${error.propertyName}: ${error.message}${error.line === null ? '' : ` (line ${error.line})`}`,
    )
    .join(' ')
}

export type BacktestStatus =
  'Pending' | 'Running' | 'Cancelling' | 'Succeeded' | 'Failed' | 'Cancelled' | string

export function isActive(status: BacktestStatus): boolean {
  return status === 'Pending' || status === 'Running' || status === 'Cancelling'
}

export function isStoppable(status: BacktestStatus): boolean {
  return status === 'Pending' || status === 'Running'
}

export function anyActive(instances: BacktestInstance[]): boolean {
  return instances.some((instance) => isActive(instance.status))
}

export interface InstanceRow extends BacktestInstance {
  classed: boolean
  precision: number | null
  recall: number | null
  edited: boolean
  stoppable: boolean
}

export function instanceRows(
  instances: BacktestInstance[],
  currentRuleText: string,
): InstanceRow[] {
  return instances.map((instance) => {
    const classed = instance.truePositives !== null

    return {
      ...instance,
      classed,
      precision: classed
        ? ratio(
            instance.truePositives ?? 0,
            (instance.truePositives ?? 0) + (instance.falsePositives ?? 0),
          )
        : null,
      recall: classed
        ? ratio(
            instance.truePositives ?? 0,
            (instance.truePositives ?? 0) + (instance.falseNegatives ?? 0),
          )
        : null,
      edited: instance.request.ruleText !== currentRuleText,
      stoppable: isStoppable(instance.status),
    }
  })
}

export interface Figures {
  precision: number | null
  recall: number | null
  f1: number | null
  specificity: number | null
  firedRate: number | null
  positiveRate: number | null
}

export function figures(result: BacktestResult): Figures {
  const precision = ratio(result.truePositives, result.truePositives + result.falsePositives)
  const recall = ratio(result.truePositives, result.truePositives + result.falseNegatives)

  return {
    precision,
    recall,
    f1:
      precision !== null && recall !== null && precision + recall > 0
        ? (2 * precision * recall) / (precision + recall)
        : null,
    specificity: ratio(result.trueNegatives, result.trueNegatives + result.falsePositives),
    firedRate: ratio(result.fired, result.evaluated),
    positiveRate: ratio(result.positives, result.evaluated),
  }
}

export interface ChartBody {
  dark: boolean
  scanned: number
  evaluated: number
  fired: number
  positives: number
  truePositives: number
  falsePositives: number
  falseNegatives: number
  trueNegatives: number
  classDefined: boolean
  tags: { name: string; count: number }[]
}

export function chartBody(result: BacktestResult, dark: boolean): ChartBody {
  return {
    dark,
    scanned: result.scanned,
    evaluated: result.evaluated,
    fired: result.fired,
    positives: result.positives,
    truePositives: result.truePositives,
    falsePositives: result.falsePositives,
    falseNegatives: result.falseNegatives,
    trueNegatives: result.trueNegatives,
    classDefined: result.classDefined,
    tags: result.tagsInSample.map((tag) => ({ name: tag.name, count: tag.count })),
  }
}

export interface DependencyUseRow {
  id: string
  name: string
  entity: string
  missing: boolean
  how: string
  line: number | null
}

export function useRows(dependencies: ModelDependency[]): DependencyUseRow[] {
  return dependencies.map((use, index) => ({
    id: `${use.name}|${index}`,
    name: use.name,
    entity: use.target
      ? `${use.target.kind} ${use.target.name}${use.target.active ? '' : ' (inactive)'}`
      : '',
    missing: use.target === null,
    how: use.dependencyKind,
    line: use.line === null ? null : use.line + 1,
  }))
}

export interface DependentRow {
  id: string
  entity: string
  through: string
  depth: number
}

export function dependentRows(dependents: ModelDependency[]): DependentRow[] {
  return dependents.map((use, index) => ({
    id: `${use.dependent.kind}|${use.dependent.name}|${index}`,
    entity: `${use.dependent.kind} ${use.dependent.name}`,
    through: use.via || use.name,
    depth: use.depth,
  }))
}
