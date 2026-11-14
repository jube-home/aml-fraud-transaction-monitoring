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
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import ConfirmDialog from '../components/ConfirmDialog.vue'

const props = defineProps<{ open: boolean; count: number; busy: boolean }>()

const emit = defineEmits<{ confirm: [toUser: string]; cancel: [] }>()

const {t} = useI18n()

const analyst = ref('')

const ready = computed(() => analyst.value.trim() !== '')

watch(
  () => props.open,
  (open) => {
    if (open) analyst.value = ''
  },
)
</script>

<template>
  <ConfirmDialog
    id="cases-bulk-reassign"
    :open="open"
    :title="t('cases.bulk.reassignTitle', { count })"
    :body="t('cases.bulk.reassignBody')"
    :confirm-label="t('cases.bulk.reassign')"
    :cancel-label="t('cases.bulk.cancel')"
    :confirm-disabled="busy || !ready"
    @confirm="emit('confirm', analyst.trim())"
    @cancel="emit('cancel')"
  >
    <label class="flex flex-col gap-1 pb-2">
      <span class="text-sm">{{ t('cases.bulk.analyst') }}</span>
      <input
        v-model="analyst"
        type="text"
        class="input input-sm w-full"
        autocomplete="off"
        data-testid="cases-bulk-reassign-analyst"
      />
    </label>
  </ConfirmDialog>
</template>
