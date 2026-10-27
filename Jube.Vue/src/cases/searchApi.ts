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

export interface CaseSavedSearch {
  guid: string
  caseWorkflowGuid: string
  name: string | null
  filterJson: string | null
  selectJson: string | null
  sharedRoleRegistryGuid: string | null
  mine: boolean
  version: number
}

export interface CaseSavedSearchInput {
  guid?: string
  caseWorkflowGuid: string
  name: string
  filterJson: string
  selectJson: string
  sharedRoleRegistryGuid: string | null
  version?: number
}

export const searchApi = {
  get: (guid: string) => useApi<CaseSavedSearch>(`/api/CaseSearchFilter/ByGuid/${guid}`),
  create: (body: CaseSavedSearchInput) =>
    useApi<CaseSavedSearch>('/api/CaseSearchFilter', { method: 'POST', body }),
  update: (body: CaseSavedSearchInput) =>
    useApi<CaseSavedSearch>('/api/CaseSearchFilter', { method: 'PUT', body }),
  remove: (guid: string) => useApi(`/api/CaseSearchFilter/${guid}`, { method: 'DELETE' }),
}
