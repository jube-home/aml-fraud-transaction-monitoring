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
  BatchResult,
  Control,
  Prescription,
  PrescriptionRow,
  SliderVariable,
  VarianceRow,
} from './types'

export const refreshDelayMs = 5000
export const resultsFromStatus = 14

export type StatusPhase = 'running' | 'finished' | 'stopping' | 'stopped' | 'empty' | 'unknown'

export function isTerminal(statusId: number | null | undefined): boolean {
  return (
    statusId === undefined ||
    statusId === null ||
    statusId === 16 ||
    statusId === 17 ||
    statusId === 19 ||
    statusId === 20 ||
    statusId > 20
  )
}

export function phaseOf(statusId: number): StatusPhase {
  if (statusId === 16 || statusId === 17) return 'finished'
  if (statusId === 18) return 'stopping'
  if (statusId === 19) return 'stopped'
  if (statusId === 20) return 'empty'
  if (statusId < 0 || statusId > 20) return 'unknown'

  return 'running'
}

export function canStop(statusId: number): boolean {
  return phaseOf(statusId) === 'running'
}

export function hasResults(statusId: number): boolean {
  return statusId >= resultsFromStatus
}

export function refreshInterval(statusId: number | undefined): number | false {
  return isTerminal(statusId) ? false : refreshDelayMs
}

export function generationOf(instance: { score: number; statusId: number } | undefined): string {
  return instance ? `${instance.score}:${instance.statusId}` : ''
}

export function modelTypeLabel(kind: string | null | undefined): string {
  if (!kind || kind === 'ExhaustiveTopologyNetwork') return 'Neural Network (evolved topology)'

  return kind.replace(/Pipeline$/, '').replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function correlationTone(correlation: number): 'error' | 'warning' | 'success' {
  const strength = Math.abs(correlation)

  if (strength > 0.9) return 'error'

  return strength > 0.7 ? 'warning' : 'success'
}

export function variableLabel(variable: {
  name: string | null
  categoricalSourceName: string | null
  categoricalLevel: string | null
}): string {
  return variable.categoricalSourceName
    ? `${variable.categoricalSourceName} = ${variable.categoricalLevel ?? ''}`
    : (variable.name ?? '')
}

export function groupPrescriptions(rows: PrescriptionRow[]): Prescription[] {
  const byId = new Map<number, Prescription>()

  for (const row of rows) {
    let grouped = byId.get(row.id)

    if (!grouped) {
      grouped = {
        id: row.id,
        name: row.name ?? '',
        variableMean: row.variableMean,
        variableStandardDeviation: row.variableStandardDeviation,
        variableMaximum: row.variableMaximum,
        variableMinimum: row.variableMinimum,
        sensitivity: row.sensitivity,
        chartId: row.promotedTrialInstanceVariableId,
      }
      byId.set(row.id, grouped)
    } else if (row.positive === true) {
      grouped.chartId = row.promotedTrialInstanceVariableId
    }

    if (row.positive === false) {
      grouped.negativePrescriptionMean = row.prescriptionMean
      grouped.negativePrescriptionStandardDeviation = row.prescriptionStandardDeviation
    } else {
      grouped.positivePrescriptionMean = row.prescriptionMean
      grouped.positivePrescriptionStandardDeviation = row.prescriptionStandardDeviation
    }
  }

  return [...byId.values()]
}

export function sortedVariance(rows: VarianceRow[]): VarianceRow[] {
  return [...rows].sort((a, b) => a.correlationAbsRank - b.correlationAbsRank)
}

export function sliderStep(min: number, max: number): number {
  const span = max - min

  if (!(span > 0)) return 1

  const raw = span / 200
  const magnitude = 10 ** Math.floor(Math.log10(raw))
  const unit = raw / magnitude

  return (unit >= 5 ? 5 : unit >= 2 ? 2 : 1) * magnitude
}

export function controlsOf(variables: SliderVariable[]): Control[] {
  const controls: Control[] = []
  const groups = new Map<string, Extract<Control, { kind: 'category' }>>()

  for (const variable of variables) {
    if (variable.categoricalSourceName) {
      let group = groups.get(variable.categoricalSourceName)

      if (!group) {
        const reference = variable.categoricalReferenceLabel ?? ''

        group = {
          kind: 'category',
          name: variable.categoricalSourceName,
          reference,
          options: [{ text: reference, value: reference }],
        }
        groups.set(variable.categoricalSourceName, group)
        controls.push(group)
      }

      group.options.push({
        text: variable.categoricalLevel ?? '',
        value: variable.categoricalKind === 3 ? '' : (variable.categoricalLevel ?? ''),
      })
    } else if (!variable.emptyRange && variable.name) {
      controls.push({
        kind: 'number',
        name: variable.name,
        min: variable.minimum,
        max: variable.maximum,
        step: sliderStep(variable.minimum, variable.maximum),
        value: variable.mean,
      })
    }
  }

  return controls
}

export function initialParams(controls: Control[]): Record<string, string | number> {
  const params: Record<string, string | number> = {}

  for (const control of controls) {
    params[control.name] = control.kind === 'number' ? control.value : control.reference
  }

  return params
}

export function performanceCharts(
  variableId: number | null,
): { key: string; route: string; narrative: string | null; valueBased: boolean }[] {
  const all = [
    { key: 'measures', route: 'Measures', narrative: 'Performance', valueBased: false },
    { key: 'confusion', route: 'PerformanceConfusion', narrative: 'Confusion', valueBased: false },
    {
      key: 'scoreDistribution',
      route: 'ScoreDistribution',
      narrative: 'ScoreDistribution',
      valueBased: true,
    },
    { key: 'roc', route: 'PerformanceRoc', narrative: 'Discrimination', valueBased: true },
    {
      key: 'precisionRecall',
      route: 'PrecisionRecall',
      narrative: 'PrecisionRecall',
      valueBased: false,
    },
    { key: 'calibration', route: 'Calibration', narrative: 'Calibration', valueBased: false },
    { key: 'gains', route: 'Gains', narrative: 'Gains', valueBased: true },
    { key: 'lift', route: 'Lift', narrative: 'Lift', valueBased: true },
    { key: 'ks', route: 'Ks', narrative: 'Ks', valueBased: true },
    {
      key: 'thresholdSweep',
      route: 'ThresholdSweep',
      narrative: 'ThresholdSweep',
      valueBased: false,
    },
    { key: 'valueCaptured', route: 'ValueCaptured', narrative: null, valueBased: true },
  ]

  return all.filter((chart) => chart.key !== 'valueCaptured' || variableId !== null)
}

export function chartUrl(
  route: string,
  instanceId: number,
  valueBased: boolean,
  basis: { id: number; name: string } | null,
): string {
  const base = `/api/Chart/Exhaustive/${route}/${instanceId}`

  return valueBased && basis
    ? `${base}?variableId=${basis.id}&variableName=${encodeURIComponent(basis.name)}`
    : base
}

export function narrativeParagraphs(
  narrative: { holistic: string[]; groups: Record<string, string[]> } | undefined,
  group: string | null,
): string[] {
  if (!narrative) return []

  return (group === null ? narrative.holistic : narrative.groups?.[group]) ?? []
}

export function batchErrorText(body: unknown, fallback: string): string {
  if (typeof body === 'object' && body !== null) {
    const record = body as Record<string, unknown>

    if (Array.isArray(record.errors) && record.errors.length > 0) {
      return record.errors.map(String).join(' ')
    }

    if (typeof record.message === 'string' && record.message) return record.message
  }

  return fallback
}

export function batchNotes(result: BatchResult): string[] {
  const notes: string[] = []

  if (result.unmappedVariables.length > 0) notes.push(result.unmappedVariables.join(', '))

  return [...notes, ...result.warnings]
}
