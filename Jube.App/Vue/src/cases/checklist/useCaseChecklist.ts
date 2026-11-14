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

import {computed, toValue, type MaybeRefOrGetter} from 'vue'
import {useQuery} from '@tanstack/vue-query'
import {checklistApi, checklistKeys} from './api'
import {deriveGate} from './gate'
import type {ChecklistGateContext} from './types'

export function useCaseChecklist(caseId: MaybeRefOrGetter<number>) {
  return useQuery({
    queryKey: computed(() => checklistKeys.byCase(toValue(caseId))),
    queryFn: () => checklistApi.get(toValue(caseId)),
    staleTime: 15_000,
    refetchOnWindowFocus: true,
    retry: false,
  })
}

export function useCaseChecklistHistory(
  caseId: MaybeRefOrGetter<number>,
  enabled: MaybeRefOrGetter<boolean>,
) {
  return useQuery({
    queryKey: computed(() => checklistKeys.history(toValue(caseId))),
    queryFn: () => checklistApi.history(toValue(caseId)),
    enabled: computed(() => toValue(enabled)),
    staleTime: 0,
    retry: false,
  })
}

export function useChecklistGate(
  caseId: MaybeRefOrGetter<number>,
  context: MaybeRefOrGetter<ChecklistGateContext>,
) {
  const checklist = useCaseChecklist(caseId)
  const gate = computed(() => deriveGate(checklist.data.value, toValue(context)))

  return {checklist, gate}
}
