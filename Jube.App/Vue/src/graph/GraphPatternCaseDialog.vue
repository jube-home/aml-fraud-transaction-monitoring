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
import {ref, useTemplateRef, watch} from 'vue'
import {useI18n} from 'vue-i18n'

const props = defineProps<{ open: boolean; patternName: string }>()

const emit = defineEmits<{ confirm: [caseId: number]; cancel: [] }>()

const {t} = useI18n()
const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const caseText = ref('')

watch(
  () => props.open,
  (open) => {
    if (open) {
      caseText.value = ''
      dialog.value?.showModal?.()
    } else dialog.value?.close?.()
  },
)

function confirm(): void {
  const caseId = Number(caseText.value)

  if (Number.isSafeInteger(caseId) && caseId > 0) emit('confirm', caseId)
}
</script>

<template>
  <dialog
    ref="dialog"
    class="modal"
    data-testid="graph-pattern-case-dialog"
    @cancel.prevent="emit('cancel')"
  >
    <form method="dialog" class="modal-box flex flex-col gap-3" @submit.prevent="confirm">
      <h3 class="text-lg font-semibold">{{ t('graph.rail.caseAsk.title') }}</h3>
      <p class="text-sm">{{ t('graph.rail.caseAsk.body', {name: patternName}) }}</p>
      <label class="input input-sm w-full">
        <span class="label">{{ t('graph.rail.caseAsk.label') }}</span>
        <input
          v-model="caseText"
          type="number"
          min="1"
          step="1"
          class="font-mono tabular-nums"
          inputmode="numeric"
          data-testid="graph-pattern-case-id"
        />
      </label>
      <div class="modal-action">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="graph-pattern-case-cancel"
          @click="emit('cancel')"
        >
          {{ t('graph.rail.caseAsk.cancel') }}
        </button>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="!(Number(caseText) > 0)"
          data-testid="graph-pattern-case-confirm"
        >
          {{ t('graph.rail.caseAsk.confirm') }}
        </button>
      </div>
    </form>
  </dialog>
</template>
