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
import type { ApprovalKind, ApprovalRecord, ApprovalStatus, VersionRow } from './model'

export interface ApprovalTarget {
  endpoint: string
  kind: ApprovalKind
  entityId: number
}

export const approvalApi = {
  status: (target: ApprovalTarget) =>
    useApi<ApprovalStatus>('/api/EntityApproval/Status', {
      query: { kind: target.kind.kind, entityId: target.entityId },
    }),
  history: (target: ApprovalTarget) =>
    useApi<ApprovalRecord[]>('/api/EntityApproval/History', {
      query: { kind: target.kind.kind, entityId: target.entityId },
    }),
  versions: (target: ApprovalTarget) =>
    useApi<VersionRow[]>(`${target.kind.historyRoute}/ByParent/${target.entityId}`),
  compare: (target: ApprovalTarget, fromId: number, toId: number) =>
    useApi<VersionRow[]>(`${target.kind.historyRoute}/Compare`, {
      query: { fromId, toId },
    }),
  decide: (
    target: ApprovalTarget,
    action: 'Approve' | 'Reject',
    version: number,
    note: string | null,
  ) =>
    useApi(`/api/EntityApproval/${action}`, {
      method: 'POST',
      body: { kind: target.kind.kind, entityId: target.entityId, version, note },
    }),
  act: (target: ApprovalTarget, action: 'Resubmit' | 'Retract', note: string | null) =>
    useApi(`${target.endpoint}/${target.entityId}/${action}`, {
      method: 'POST',
      body: { note },
    }),
}
