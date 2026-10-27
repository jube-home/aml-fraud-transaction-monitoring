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

import { computed, type Ref } from 'vue'
import { useQueries } from '@tanstack/vue-query'
import { approvalApi } from '../approval/api'
import type { ApprovalStatus } from '../approval/model'
import { queryKeys } from '../api/queryKeys'

export interface ApprovalTarget {
  kind: number
  id: number
}

export function approvalKey(kind: number, id: number): string {
  return `${kind}:${id}`
}

export function useRecordApprovals(targets: Ref<ApprovalTarget[]>) {
  const results = useQueries({
    queries: computed(() =>
      targets.value.map((target) => ({
        queryKey: queryKeys.approvalStatus(target.kind, target.id),
        queryFn: () =>
          approvalApi.status({
            endpoint: '',
            kind: { kind: target.kind, historyRoute: '' },
            entityId: target.id,
          }),
        retry: false,
        staleTime: 30_000,
      })),
    ),
  })

  return computed(
    () =>
      new Map<string, ApprovalStatus | undefined>(
        targets.value.map((target, index) => [
          approvalKey(target.kind, target.id),
          results.value[index]?.data,
        ]),
      ),
  )
}
