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
import { stateOf, type ValueState, type ValueStatus } from '../valueGrid/model'
import type {
  AnalyticsInstance,
  MetricComparison,
  ModelName,
  ModelVersion,
  VersionComparison,
} from './types'

export type MetricKey =
  | 'testRocAuc'
  | 'testAveragePrecision'
  | 'testLogLoss'
  | 'testBrierScore'
  | 'testExpectedCalibrationError'

export const versionMetrics: { key: MetricKey; better: 'higher' | 'lower' }[] = [
  { key: 'testRocAuc', better: 'higher' },
  { key: 'testAveragePrecision', better: 'higher' },
  { key: 'testLogLoss', better: 'lower' },
  { key: 'testBrierScore', better: 'lower' },
  { key: 'testExpectedCalibrationError', better: 'lower' },
]

const kindKeys: Record<number, string> = {
  1: 'classifier',
  2: 'regressor',
  3: 'anomalyDetector',
  4: 'clusterer',
  5: 'forecaster',
  6: 'sequenceModel',
  7: 'dimensionReducer',
  8: 'densityEstimator',
}

const sourceKeys: Record<number, string> = { 1: 'agent', 2: 'upload' }

export function kindKey(kind: number): string | undefined {
  return kindKeys[kind]
}

export function sourceKey(source: number): string | undefined {
  return sourceKeys[source]
}

export function bestVersionIds(versions: ModelVersion[]): Record<MetricKey, number[]> {
  const best = {} as Record<MetricKey, number[]>

  for (const metric of versionMetrics) {
    const scored = versions.filter((version) => typeof version[metric.key] === 'number')

    if (scored.length < 2) {
      best[metric.key] = []

      continue
    }

    const target =
      metric.better === 'higher'
        ? Math.max(...scored.map((version) => version[metric.key] as number))
        : Math.min(...scored.map((version) => version[metric.key] as number))

    best[metric.key] = scored
      .filter((version) => version[metric.key] === target)
      .map((version) => version.id)
  }

  return best
}

export interface LiveStanding {
  liveId: number | null
  best: number
  of: number
  allBest: boolean
}

export function liveStanding(versions: ModelVersion[]): LiveStanding {
  const live = versions.find((version) => version.isActive)

  if (!live) return { liveId: null, best: 0, of: 0, allBest: false }

  const bests = bestVersionIds(versions)
  const compared = versionMetrics.filter((metric) => bests[metric.key].length > 0)
  const won = compared.filter((metric) => bests[metric.key].includes(live.id))

  return {
    liveId: live.id,
    best: won.length,
    of: compared.length,
    allBest: compared.length > 0 && won.length === compared.length,
  }
}

export function defaultViewedVersion(versions: ModelVersion[]): number | null {
  const live = versions.find((version) => version.isActive)

  if (live) return live.id

  return [...versions].sort((a, b) => b.versionNumber - a.versionNumber)[0]?.id ?? null
}

const lowerIsBetter = new Set(['TestLogLoss', 'TestBrierScore', 'TestExpectedCalibrationError'])
const higherIsBetter = new Set([
  'TestRocAuc',
  'TestAveragePrecision',
  'TestMatthewsCorrelation',
  'TestPrecision',
  'TestRecall',
])

export type Movement = 'better' | 'worse' | 'same' | 'unknown'

export function movementOf(metric: MetricComparison): Movement {
  if (metric.difference === null) return 'unknown'

  if (metric.difference === 0) return 'same'

  if (metric.name === 'TestCalibrationSlope') {
    const before = metric.valueA === null ? null : Math.abs(metric.valueA - 1)
    const after = metric.valueB === null ? null : Math.abs(metric.valueB - 1)

    if (before === null || after === null) return 'unknown'

    if (Math.abs(after - before) < 1e-9) return 'same'

    return after < before ? 'better' : 'worse'
  }

  if (higherIsBetter.has(metric.name)) return metric.difference > 0 ? 'better' : 'worse'

  if (lowerIsBetter.has(metric.name)) return metric.difference < 0 ? 'better' : 'worse'

  return 'unknown'
}

export function comparable(
  comparison: Pick<VersionComparison, 'sameSeed' | 'sameSplitSizes' | 'sameVariables'>,
): boolean {
  return comparison.sameSeed && comparison.sameSplitSizes && comparison.sameVariables
}

export function differences(
  comparison: Pick<VersionComparison, 'sameSeed' | 'sameSplitSizes' | 'sameVariables'>,
): ('seed' | 'splits' | 'variables')[] {
  const found: ('seed' | 'splits' | 'variables')[] = []

  if (!comparison.sameSeed) found.push('seed')
  if (!comparison.sameSplitSizes) found.push('splits')
  if (!comparison.sameVariables) found.push('variables')

  return found
}

export interface InstanceRow {
  id: number
  name: string
  active: boolean
  allocated: string
  responsePayload: boolean
  reportTable: boolean
  state: ValueState
  progress: string
  maker: string
  note: string
  returnNote: string
  pending: boolean
  rejected: boolean
  canApprove: boolean
  hasBaseline: boolean
  deleted: boolean
  entityAnalysisModelId: number | null
  createdDate: string | null
}

export function instanceVisible(
  instance: Pick<AnalyticsInstance, 'deletedUser'>,
  status: ValueStatus | undefined,
): boolean {
  if (!instance.deletedUser) return true

  return Boolean(status && (status.pending || status.rejected))
}

export function instanceRows(
  instances: AnalyticsInstance[],
  statuses: ValueStatus[],
  models: ModelName[],
  unavailable: boolean,
  notAllocated: string,
): InstanceRow[] {
  const byInstance = new Map(statuses.map((status) => [status.entityId, status]))
  const names = new Map(models.map((model) => [model.id, model.name]))

  return instances
    .filter((instance) => instanceVisible(instance, byInstance.get(instance.id)))
    .map((instance) => {
      const status = byInstance.get(instance.id)

      return {
        id: instance.id,
        name: instance.name ?? '',
        active: instance.active,
        allocated: instance.entityAnalysisModelId
          ? (names.get(instance.entityAnalysisModelId) ?? String(instance.entityAnalysisModelId))
          : notAllocated,
        responsePayload: instance.responsePayload,
        reportTable: instance.reportTable,
        state: stateOf(instance, status, unavailable),
        progress: status?.pending
          ? `${status.approvalsRecorded} of ${status.approvalsRequired}`
          : '',
        maker: status?.makerUser ?? '',
        note: status?.note ?? '',
        returnNote: status?.returnedNote ?? '',
        pending: Boolean(status?.pending),
        rejected: Boolean(status?.rejected),
        canApprove: Boolean(status?.canApprove),
        hasBaseline: status?.effectiveVersion !== null && status?.effectiveVersion !== undefined,
        deleted: Boolean(instance.deletedUser),
        entityAnalysisModelId: instance.entityAnalysisModelId,
        createdDate: instance.createdDate,
      }
    })
}

export function allocatedModelIds(
  instances: Pick<AnalyticsInstance, 'entityAnalysisModelId'>[],
): number[] {
  return [
    ...new Set(
      instances.flatMap((instance) =>
        instance.entityAnalysisModelId ? [instance.entityAnalysisModelId] : [],
      ),
    ),
  ]
}

export const analyticsPermission = {
  view: [45, 46, 47, 48],
  setActive: 47,
  delete: 48,
} as const
