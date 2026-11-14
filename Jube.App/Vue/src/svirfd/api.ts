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

import {useApi} from '../api/useApi'
import {journalFrom, lookupFrom} from './convert'
import type {
  JournalSvirfd,
  JournalSvirfdDto,
  SvirfdLookup,
  SvirfdLookupDto,
  SvirfdStatus,
} from './types'

export const svirfdApi = {
  lookup: async (ip: string): Promise<SvirfdLookup> =>
    lookupFrom(await useApi<SvirfdLookupDto>(`/api/Svirfd/Lookup/${encodeURIComponent(ip)}`)),
  status: (): Promise<SvirfdStatus> => useApi<SvirfdStatus>('/api/Svirfd/Status'),
  journal: async (workflow: string, id: number, field: string): Promise<JournalSvirfd> =>
    journalFrom(
      await useApi<JournalSvirfdDto>('/api/CaseKeyJournal/Svirfd', {
        method: 'POST',
        body: {caseWorkflowGuid: workflow, id, field},
      }),
    ),
}
