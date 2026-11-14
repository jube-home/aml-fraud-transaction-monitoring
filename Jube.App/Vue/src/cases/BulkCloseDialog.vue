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
import {ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import ConfirmDialog from '../components/ConfirmDialog.vue'

const props = defineProps<{ open: boolean; count: number; busy: boolean }>()

const emit = defineEmits<{ confirm: [reasonId: number, note: string | null]; cancel: [] }>()

const {t} = useI18n()

const reasonIds = [1, 2, 3, 4, 5, 6, 7, 8]
const reasonId = ref(0)
const note = ref('')

watch(
  () => props.open,
  (open) => {
    if (open) {
      reasonId.value = 0
      note.value = ''
    }
  },
)

function confirm(): void {
  emit('confirm', reasonId.value, note.value.trim() === '' ? null : note.value.trim())
}
</script>

<template>
  <ConfirmDialog
    id="cases-bulk-close"
    :open="open"
    :title="t('cases.bulk.closeTitle', { count })"
    :body="t('cases.bulk.closeBody')"
    :confirm-label="t('cases.bulk.close')"
    :cancel-label="t('cases.bulk.cancel')"
    :confirm-disabled="busy"
    @confirm="confirm()"
    @cancel="emit('cancel')"
  >
    <div class="flex flex-col gap-2 pb-2">
      <label class="flex flex-col gap-1">
        <span class="text-sm">{{ t('caseDetail.decide.closeReason') }}</span>
        <select
          v-model="reasonId"
          class="select select-sm w-full"
          data-testid="cases-bulk-close-reason"
        >
          <option :value="0">{{ t('caseDetail.decide.closeReasonNone') }}</option>
          <option v-for="id in reasonIds" :key="id" :value="id">
            {{ t(`caseDetail.decide.closeReasons.${id}`) }}
          </option>
        </select>
      </label>
      <label class="flex flex-col gap-1">
        <span class="text-sm">{{ t('caseDetail.decide.closeNote') }}</span>
        <textarea
          v-model="note"
          class="textarea w-full"
          rows="2"
          maxlength="1000"
          data-testid="cases-bulk-close-note"
        ></textarea>
      </label>
    </div>
  </ConfirmDialog>
</template>
