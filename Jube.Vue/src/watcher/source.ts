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
import { openWatcherStream, type WatcherStreamStatus } from './stream'

export interface ReplayQuery {
  from: Date | null
  to: Date | null
  key?: string
  keyValue?: string
}

export interface WatcherSource {
  replay: (query: ReplayQuery) => Promise<unknown[]>
  stream?: (
    onEvent: (raw: unknown) => void,
    onStatus: (status: WatcherStreamStatus) => void,
  ) => () => void
}

export const activationWatcherSource: WatcherSource = {
  replay: async (query) => {
    const rows = await useApi<unknown[]>('/api/ActivationWatcher/Replay', {
      query: {
        dateFrom: query.from?.toISOString(),
        dateTo: query.to?.toISOString(),
        key: query.key,
        keyValue: query.keyValue,
      },
    })

    return Array.isArray(rows) ? rows : []
  },
  stream: (onEvent, onStatus) => openWatcherStream({ onActivation: onEvent, onStatus }),
}
