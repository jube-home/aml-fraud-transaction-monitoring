<!-- Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed, reactive, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQueryClient} from '@tanstack/vue-query'
import {ListChecks} from 'lucide-vue-next'
import {useToasts} from '../../stores/toasts'
import {checklistApi, checklistKeys} from '../checklist/api'
import ChecklistItemRow from '../checklist/ChecklistItemRow.vue'
import {serverMessage} from '../checklist/refusal'
import {itemState, type ChecklistItem} from '../checklist/types'
import {useCaseChecklist} from '../checklist/useCaseChecklist'
import DecideDialog from './DecideDialog.vue'
import {closedStatus} from './operationsApi'
import type {CaseDetail} from './types'

const props = defineProps<{
  open: boolean
  detail: CaseDetail
  userName: string
}>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const refusals = reactive<Record<number, string>>({})
const pendingId = ref<number | null>(null)

const caseId = computed(() => props.detail.id)
const checklist = useCaseChecklist(caseId)

watch(
  () => props.open,
  (open) => {
    if (open) {
      for (const key of Object.keys(refusals)) delete refusals[Number(key)]
    }
  },
)

const caseOpen = computed(() => props.detail.closedStatusId !== closedStatus.closed)
const mine = computed(() => props.detail.locked && props.detail.lockedUser === props.userName)
const editable = computed(() => caseOpen.value && mine.value)

const readOnlyReason = computed(() => {
  if (editable.value) return ''
  if (!caseOpen.value) return t('caseDetail.checklist.readOnly.closed')
  if (!props.detail.locked) return t('caseDetail.checklist.readOnly.free')

  return t('caseDetail.checklist.readOnly.other', {user: props.detail.lockedUser ?? ''})
})

const groups = computed(() =>
  (checklist.data.value?.checklists ?? []).filter((group) => group.items.length > 0),
)

function doneIn(items: ChecklistItem[]): number {
  return items.filter((item) => item.stateId === itemState.done).length
}

const tick = useMutation({
  mutationFn: (variables: { item: ChecklistItem; checked: boolean }) =>
    variables.checked
      ? checklistApi.check(caseId.value, variables.item.id)
      : checklistApi.uncheck(caseId.value, variables.item.id),
  onMutate: (variables) => {
    pendingId.value = variables.item.id
    delete refusals[variables.item.id]
  },
  onSuccess: (view) => {
    client.setQueryData(checklistKeys.byCase(caseId.value), view)
    void client.invalidateQueries({queryKey: ['service', 'Case']})
  },
  onError: (error, variables) => {
    const message = serverMessage(error)

    if (message !== null) {
      refusals[variables.item.id] = message
    } else {
      refusals[variables.item.id] = t('caseDetail.checklist.refused')
      toasts.apiError(error)
    }

    void client.invalidateQueries({queryKey: checklistKeys.byCase(caseId.value)})
  },
  onSettled: () => {
    pendingId.value = null
    void client.invalidateQueries({queryKey: checklistKeys.history(caseId.value)})
  },
})

function toggle(item: ChecklistItem, checked: boolean): void {
  tick.mutate({item, checked})
}
</script>

<template>
  <DecideDialog
    :open="open"
    :title="t('caseDetail.checklist.title')"
    :icon="ListChecks"
    testid="checklist-dialog"
    @close="emit('close')"
  >
    <div class="flex flex-col gap-4" data-testid="checklist-items">
      <p
        v-if="readOnlyReason"
        key="readonly"
        class="text-sm text-base-content/70"
        data-testid="checklist-readonly"
      >
        {{ readOnlyReason }}
      </p>

      <p
        v-if="groups.length === 0"
        key="empty"
        class="text-sm text-base-content/70"
        data-testid="checklist-empty"
      >
        {{ t('caseDetail.checklist.empty') }}
      </p>

      <section
        v-for="group in groups"
        :key="group.caseWorkflowChecklistGuid"
        class="flex flex-col gap-2"
        :data-testid="`checklist-group-${group.caseWorkflowChecklistGuid}`"
      >
        <div class="flex items-baseline justify-between gap-3">
          <h4 class="text-sm font-semibold">{{ group.name }}</h4>
          <span class="text-xs text-base-content/70">
            {{
              t('caseDetail.checklist.group', {
                done: doneIn(group.items),
                required: group.items.length,
              })
            }}
          </span>
        </div>
        <ul class="flex flex-col gap-2">
          <ChecklistItemRow
            v-for="item in group.items"
            :key="item.id"
            :item="item"
            :editable="editable"
            :pending="pendingId === item.id"
            :refusal="refusals[item.id] ?? null"
            @toggle="toggle(item, $event)"
          />
        </ul>
      </section>
    </div>
  </DecideDialog>
</template>
