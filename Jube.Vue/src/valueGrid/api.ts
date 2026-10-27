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
import { readPath, type ValueConfig, type ValueRecord, type ValueStatus } from './model'

export const valueGridApi = {
  values: (config: ValueConfig, parentId: number) =>
    useApi<ValueRecord[]>(readPath(config), {
      query: { [config.readParameter]: parentId, includeDeleted: true },
    }),
  statuses: (config: ValueConfig, parentId: number) =>
    useApi<ValueStatus[]>('/api/EntityApproval/ValueStatus', {
      method: 'POST',
      body: { kind: config.parentKind, entityId: parentId },
    }),
  create: (config: ValueConfig, body: Record<string, unknown>) =>
    useApi(config.endpoint, { method: 'POST', body }),
  update: (config: ValueConfig, body: Record<string, unknown>) =>
    useApi(config.endpoint, { method: 'PUT', body }),
  remove: (config: ValueConfig, id: number) =>
    useApi(config.endpoint, { method: 'DELETE', query: { id } }),
  decide: (status: ValueStatus, action: 'Approve' | 'Reject', note: string | null) =>
    useApi(`/api/EntityApproval/${action}`, {
      method: 'POST',
      body: {
        kind: status.kind,
        entityId: status.entityId,
        version: status.currentVersion,
        note,
      },
    }),
  act: (config: ValueConfig, id: number, action: 'Resubmit' | 'Retract', note: string | null) =>
    useApi(`${config.endpoint}/${id}/${action}`, { method: 'POST', body: { note } }),
  approveAll: (config: ValueConfig, parentId: number) =>
    useApi<{ approved: number; refusals: string[] }>('/api/EntityApproval/ApproveValues', {
      method: 'POST',
      body: { kind: config.parentKind, entityId: parentId },
    }),
}
