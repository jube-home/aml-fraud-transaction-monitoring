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

export type CanvasName = 'queue' | 'quality'

export interface CanvasChart {
  kind: string
  labelKey: string
}

export interface CanvasSectionLayout {
  id: string
  titleKey: string
  hintKey: string
  charts: CanvasChart[]
  card?: 'qaSample'
}

export interface CanvasLayout {
  titleKey: string
  indexKey: string
  sections: CanvasSectionLayout[]
}

export const periods = [7, 30, 90] as const

export const defaultPeriod = 30

export const canvasLayouts: Record<CanvasName, CanvasLayout> = {
  queue: {
    titleKey: 'cases.analytics.queue.title',
    indexKey: 'cases.analytics.queue.index',
    sections: [
      {
        id: 'ageing',
        titleKey: 'cases.analytics.section.ageing',
        hintKey: 'cases.analytics.hint.ageing',
        charts: [{ kind: 'Aging', labelKey: 'cases.analytics.chart.aging' }],
      },
      {
        id: 'flow',
        titleKey: 'cases.analytics.section.flow',
        hintKey: 'cases.analytics.hint.flow',
        charts: [{ kind: 'Flow', labelKey: 'cases.analytics.chart.flow' }],
      },
      {
        id: 'bottlenecks',
        titleKey: 'cases.analytics.section.bottlenecks',
        hintKey: 'cases.analytics.hint.bottlenecks',
        charts: [{ kind: 'StatusDwell', labelKey: 'cases.analytics.chart.statusDwell' }],
      },
      {
        id: 'service',
        titleKey: 'cases.analytics.section.service',
        hintKey: 'cases.analytics.hint.service',
        charts: [
          { kind: 'FirstView', labelKey: 'cases.analytics.chart.firstView' },
          { kind: 'Resolution', labelKey: 'cases.analytics.chart.resolution' },
          { kind: 'ServiceLevelWeekly', labelKey: 'cases.analytics.chart.serviceLevelWeekly' },
        ],
      },
      {
        id: 'rhythm',
        titleKey: 'cases.analytics.section.rhythm',
        hintKey: 'cases.analytics.hint.rhythm',
        charts: [
          { kind: 'Arrivals', labelKey: 'cases.analytics.chart.arrivals' },
          { kind: 'FirstViewByHour', labelKey: 'cases.analytics.chart.firstViewByHour' },
          { kind: 'Handoffs', labelKey: 'cases.analytics.chart.handoffs' },
        ],
      },
    ],
  },
  quality: {
    titleKey: 'cases.analytics.quality.title',
    indexKey: 'cases.analytics.quality.index',
    sections: [
      {
        id: 'rules',
        titleKey: 'cases.analytics.section.rules',
        hintKey: 'cases.analytics.hint.rules',
        charts: [
          { kind: 'RuleEffort', labelKey: 'cases.analytics.chart.ruleEffort' },
          { kind: 'RulePrecision', labelKey: 'cases.analytics.chart.rulePrecision' },
        ],
      },
      {
        id: 'ranking',
        titleKey: 'cases.analytics.section.ranking',
        hintKey: 'cases.analytics.hint.ranking',
        charts: [{ kind: 'Gains', labelKey: 'cases.analytics.chart.gains' }],
      },
      {
        id: 'sample',
        titleKey: 'cases.analytics.section.sample',
        hintKey: 'cases.analytics.hint.sample',
        charts: [],
        card: 'qaSample',
      },
      {
        id: 'calibration',
        titleKey: 'cases.analytics.section.calibration',
        hintKey: 'cases.analytics.hint.calibration',
        charts: [{ kind: 'Calibration', labelKey: 'cases.analytics.chart.calibration' }],
      },
    ],
  },
}

export function periodFrom(days: number, now: Date): string {
  const start =
    Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()) - days * 86_400_000

  return new Date(start).toISOString()
}

export function chartPath(kind: string, workflow: string, days: number, now: Date): string {
  const query = new URLSearchParams({ caseWorkflowGuid: workflow, from: periodFrom(days, now) })

  return `/api/CaseAnalytics/Chart/${kind}?${query.toString()}`
}

export function periodOf(value: unknown): number {
  const single = Array.isArray(value) ? value[0] : value
  const parsed = Number(single)

  return (periods as readonly number[]).includes(parsed) ? parsed : defaultPeriod
}
