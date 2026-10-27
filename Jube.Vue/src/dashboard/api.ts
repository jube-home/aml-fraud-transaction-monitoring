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
import { cardRequest, datasourceApi } from '../datasource/api'
import { knownStylesheetVersion } from '../datasource/chartStylesheet'
import type { CardParameter } from '../datasource/types'
import type {
  DirectoryEntry,
  DirectorySummary,
  ParameterDefinition,
  TileDefinition,
  VisualisationRegistry,
} from './types'

export const dashboardApi = {
  directory: () => useApi<DirectoryEntry[]>('/api/VisualisationRegistry/GetByShowInDirectory'),

  registry: (guid: string) =>
    useApi<VisualisationRegistry>(
      `/api/VisualisationRegistry/GetByGuidActiveOnly/${encodeURIComponent(guid)}`,
    ),

  summary: () => useApi<DirectorySummary[]>('/api/VisualisationRegistry/Summary'),

  parameters: (registryId: number) =>
    useApi<ParameterDefinition[]>(
      `/api/VisualisationRegistryParameter/ByVisualisationRegistryIdActiveOnly/${registryId}`,
    ),

  tiles: (registryId: number) =>
    useApi<TileDefinition[]>(
      `/api/VisualisationRegistryDatasource/ByVisualisationRegistryIdActiveOnly/${registryId}`,
    ),

  card: (
    id: number,
    parameters: CardParameter[],
    size: { width: number; height: number },
    signal?: AbortSignal,
    contextId?: number,
  ) =>
    datasourceApi.card(
      id,
      {
        ...cardRequest(parameters, knownStylesheetVersion()),
        ...size,
        ...(contextId === undefined ? {} : { contextId }),
      },
      signal,
    ),
}
