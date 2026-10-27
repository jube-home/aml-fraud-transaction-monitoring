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
import type {
  CardParameter,
  CardRequest,
  ChartKind,
  DatasourceCard,
  Introspection,
  MappingEntry,
} from './types'

interface IntrospectionResponse {
  valid?: boolean
  columns?: Introspection['columns'] | null
  error?: string | null
  errorLine?: number | null
  errorColumn?: number | null
  kinds?: Introspection['kinds'] | null
  resolvedMapping?: Introspection['resolvedMapping'] | null
  mappingError?: string | null
  templateError?: string | null
}

export interface IntrospectBody {
  visualisationRegistryId: number
  command: string
  displayKindId: number
  mapping: MappingEntry[]
  templateText: string
}

export interface PreviewBody {
  visualisationRegistryId: number
  command: string
  displayKindId: number
  mapping: MappingEntry[]
  templateText: string
  includeGrid: boolean
  card: CardRequest
}

export function cardRequest(parameters: CardParameter[], knownVersion: string | null): CardRequest {
  return {
    parameters,
    themeId: 1,
    styleModeId: 1,
    knownStylesheetVersion: knownVersion,
    width: 0,
    height: 0,
  }
}

export const datasourceApi = {
  chartKinds: () => useApi<ChartKind[]>('/api/VisualisationRegistryDatasource/ChartKinds'),

  introspect: async (body: IntrospectBody, signal?: AbortSignal): Promise<Introspection> => {
    const response = await useApi<IntrospectionResponse>(
      '/api/VisualisationRegistryDatasource/Introspect',
      { method: 'POST', body, signal },
    )

    return {
      valid: response.valid ?? false,
      columns: response.columns ?? [],
      error: response.error ?? null,
      errorLine: response.errorLine ?? null,
      errorColumn: response.errorColumn ?? null,
      kinds: response.kinds ?? [],
      resolvedMapping: response.resolvedMapping ?? [],
      mappingError: response.mappingError ?? null,
      templateError: response.templateError ?? null,
    }
  },

  preview: (body: PreviewBody, signal?: AbortSignal) =>
    useApi<DatasourceCard>('/api/VisualisationRegistryDatasource/Preview', {
      method: 'POST',
      body,
      signal,
    }),

  card: (id: number, request: CardRequest, signal?: AbortSignal) =>
    useApi<DatasourceCard>(`/api/VisualisationRegistryDatasource/${id}/Card`, {
      method: 'POST',
      body: request,
      signal,
    }),
}
