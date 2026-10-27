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
import { fromLocalInput, toLocalInput } from '../composables/localDateTime'

export interface SampleDraft {
  modelGuid: string
  from: string
  to: string
  sample: number
}

export type SampleProblem = 'model' | 'from' | 'to' | 'order' | 'sample'

export interface SampleModel {
  id: number
  guid: string
  name: string
}

export const defaultSampleRate = 0.02

export function defaultDraft(now = new Date()): SampleDraft {
  return {
    modelGuid: '',
    from: toLocalInput(new Date(now.getTime() - 86_400_000)),
    to: toLocalInput(now),
    sample: defaultSampleRate,
  }
}

export function sampleProblems(draft: SampleDraft): SampleProblem[] {
  const from = fromLocalInput(draft.from)
  const to = fromLocalInput(draft.to)
  const problems: SampleProblem[] = []

  if (!draft.modelGuid) problems.push('model')
  if (!from) problems.push('from')
  if (!to) problems.push('to')
  if (from && to && to.getTime() <= from.getTime()) problems.push('order')
  if (!(draft.sample >= 0 && draft.sample <= 1)) problems.push('sample')

  return problems
}

export function sampleBody(draft: SampleDraft) {
  return {
    entityAnalysisModelGuid: draft.modelGuid,
    sample: draft.sample,
    dateFrom: fromLocalInput(draft.from)?.toISOString() ?? null,
    dateTo: fromLocalInput(draft.to)?.toISOString() ?? null,
  }
}

export function percent(sample: number): string {
  return `${(sample * 100).toFixed(sample * 100 >= 10 ? 0 : 1)}%`
}
