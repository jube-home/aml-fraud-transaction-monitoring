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

export interface InventoryRow {
  kindId: number
  modelGuid: string
  name: string
  recorded: boolean
  ownerUser: string | null
  riskTierId: number | null
  nextReviewDate: string | null
  reviewStatusId: number
}

export interface GovernanceRecord {
  kindId: number
  modelGuid: string
  version: number
  ownerUser: string
  intendedPurpose: string
  riskTierId: number
  humanOversight: string
  nextReviewDate: string | null
  createdUser: string
  createdDate: string
}

export interface GovernanceHistory {
  current: GovernanceRecord | null
  history: GovernanceRecord[]
}

export const governanceKey = ['service', 'ModelGovernance', 'inventory'] as const
export const maximumOwner = 200
export const maximumText = 2000

const base = '/api/ModelGovernance'

export const governanceApi = {
  inventory: () => useApi<InventoryRow[]>(base),
  get: (kindId: number, modelGuid: string) =>
    useApi<GovernanceHistory>(`${base}/ByModel`, {query: {kindId, modelGuid}}),
  set: (record: Omit<GovernanceRecord, 'version' | 'createdUser' | 'createdDate'>) =>
    useApi<GovernanceRecord>(`${base}/Set`, {method: 'POST', body: record}),
  cardUrl: (kindId: number, modelGuid: string) =>
    `${base}/Card?kindId=${kindId}&modelGuid=${encodeURIComponent(modelGuid)}`,
}

export function statementReady(
  owner: string,
  purpose: string,
  oversight: string,
  tier: number,
): boolean {
  return (
    owner.trim().length > 0 &&
    owner.trim().length <= maximumOwner &&
    purpose.trim().length > 0 &&
    purpose.trim().length <= maximumText &&
    oversight.trim().length > 0 &&
    oversight.trim().length <= maximumText &&
    tier >= 1 &&
    tier <= 3
  )
}
