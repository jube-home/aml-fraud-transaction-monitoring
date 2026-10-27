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
import { useApi } from '../api/useApi'
import type { Completion } from '../ruleBuilder/types'
import type { CaseStatus } from './filters'

export type CaseWorkflowRef = { kind: 'guid'; value: string } | { kind: 'id'; value: number }

export type ExhaustiveSource =
  { kind: 'model'; modelId: number } | { kind: 'dataset'; datasetKind: 'Csv' | 'Json'; id: number }

export function caseWorkflowKey(ref: CaseWorkflowRef): string {
  return `${ref.kind}:${ref.value}`
}

export function exhaustiveSourceKey(source: ExhaustiveSource): string {
  return source.kind === 'model'
    ? `model:${source.modelId}`
    : `dataset:${source.datasetKind}:${source.id}`
}

export const filterBuilderApi = {
  caseCompletions: (ref: CaseWorkflowRef) =>
    ref.kind === 'guid'
      ? useApi<Completion[]>('/api/Completions/ByCaseWorkflowGuidIncludingDeleted', {
          query: { caseWorkflowGuid: ref.value },
        })
      : useApi<Completion[]>('/api/Completions/ByCaseWorkflowIdIncludingDeleted', {
          query: { caseWorkflowId: ref.value },
        }),
  caseStatuses: (ref: CaseWorkflowRef) =>
    useApi<CaseStatus[]>(
      ref.kind === 'guid'
        ? `/api/CaseWorkflowStatus/ByCaseWorkflowGuid/${ref.value}`
        : `/api/CaseWorkflowStatus/ByCaseWorkflowId/${ref.value}`,
    ),
  exhaustiveCompletions: (source: ExhaustiveSource) =>
    source.kind === 'model'
      ? useApi<Completion[]>('/api/Completions/ByEntityAnalysisModelId', {
          query: { entityAnalysisModelId: source.modelId },
        })
      : useApi<Completion[]>('/api/Completions/ByExhaustiveInstanceDataset', {
          query: { kind: source.datasetKind, id: source.id },
        }),
}
