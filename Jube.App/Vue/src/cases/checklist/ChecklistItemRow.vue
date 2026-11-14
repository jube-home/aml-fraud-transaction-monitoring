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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useFormat} from '../../composables/useFormat'
import ChecklistSlaChip from './ChecklistSlaChip.vue'
import {itemState, type ChecklistItem} from './types'

const props = defineProps<{
  item: ChecklistItem
  editable: boolean
  pending: boolean
  refusal: string | null
}>()

const emit = defineEmits<{ toggle: [checked: boolean] }>()

const {t} = useI18n()
const {dateTime} = useFormat()

const done = computed(() => props.item.stateId === itemState.done)

function change(event: Event): void {
  const box = event.target as HTMLInputElement
  const wanted = box.checked

  box.checked = done.value
  emit('toggle', wanted)
}
</script>

<template>
  <li
    class="flex flex-col gap-1 rounded-box border border-base-300 px-3 py-2"
    :data-testid="`checklist-item-${item.id}`"
  >
    <div class="flex items-start gap-3">
      <label class="flex min-w-0 flex-1 cursor-pointer items-start gap-3">
        <input
          type="checkbox"
          class="checkbox checkbox-sm mt-0.5"
          :checked="done"
          :disabled="!editable || pending"
          :data-testid="`checklist-tick-${item.id}`"
          @change="change($event)"
        />
        <span class="min-w-0 text-sm font-medium">{{ item.name }}</span>
      </label>
      <ChecklistSlaChip :item="item"/>
    </div>
    <span
      v-if="done && item.completedUser"
      key="by"
      class="ps-8 text-xs text-base-content/70"
      :data-testid="`checklist-by-${item.id}`"
    >
      {{
        t('caseDetail.checklist.completedBy', {
          user: item.completedUser,
          when: item.completedDate ? dateTime(item.completedDate) : '',
        })
      }}
    </span>
    <p
      v-if="refusal"
      key="refusal"
      role="alert"
      class="ps-8 text-xs text-error"
      :data-testid="`checklist-refusal-${item.id}`"
    >
      {{ refusal }}
    </p>
  </li>
</template>
