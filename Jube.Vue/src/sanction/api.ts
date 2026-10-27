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
import { sanctionQuery } from './state'
import type { SanctionParams, SanctionResult } from './types'

interface SanctionResponse {
  entries?: SanctionResult['entries'] | null
  aggregations?: {
    total?: SanctionResult['total']
    bySource?: SanctionResult['bySource'] | null
  } | null
}

export const sanctionApi = {
  search: async (params: SanctionParams): Promise<SanctionResult> => {
    const response = await useApi<SanctionResponse>('/api/Invoke/Sanction', {
      query: sanctionQuery(params),
    })

    return {
      entries: response.entries ?? [],
      total: response.aggregations?.total ?? null,
      bySource: response.aggregations?.bySource ?? [],
    }
  },
}
