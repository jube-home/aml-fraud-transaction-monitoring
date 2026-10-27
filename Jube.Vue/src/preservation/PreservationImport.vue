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
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { useToasts } from '../stores/toasts'
import PreservationPreview from './PreservationPreview.vue'
import { preservationApi } from './api'
import { canImport } from './state'
import type { ImportPreview } from './types'

const props = defineProps<{ password: string; acceptable: boolean }>()

const emit = defineEmits<{ imported: [] }>()

const { t } = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()
const confirmId = useId()

const file = ref<File | null>(null)
const preview = ref<ImportPreview>()
const confirmed = ref(false)
const phase = ref<'idle' | 'previewing' | 'importing' | 'done'>('idle')
const problem = ref<'invalid' | 'forbidden' | 'failed' | 'none-chosen' | null>(null)
const input = ref<HTMLInputElement | null>(null)

const ready = computed(() => canImport(preview.value, confirmed.value))

function reset(): void {
  preview.value = undefined
  confirmed.value = false
  problem.value = null

  if (phase.value !== 'previewing' && phase.value !== 'importing') phase.value = 'idle'
}

function choose(event: Event): void {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
  reset()
}

watch(
  () => props.password,
  () => reset(),
)

function classify(error: unknown): 'invalid' | 'forbidden' | 'failed' {
  if (error instanceof ApiError && error.kind === 'forbidden') return 'forbidden'
  if (error instanceof ApiError && error.status === 400) return 'invalid'

  return 'failed'
}

async function runPreview(): Promise<void> {
  if (!file.value) {
    problem.value = 'none-chosen'

    return
  }

  phase.value = 'previewing'
  problem.value = null
  confirmed.value = false

  try {
    preview.value = await preservationApi.previewPackage(file.value, props.password)
    phase.value = 'idle'
  } catch (error) {
    preview.value = undefined
    problem.value = classify(error)
    phase.value = 'idle'
  }
}

async function runImport(): Promise<void> {
  if (!file.value || !ready.value) return

  phase.value = 'importing'
  problem.value = null

  try {
    await preservationApi.importPackage(file.value, props.password)
    phase.value = 'done'
    preview.value = undefined
    confirmed.value = false
    file.value = null

    if (input.value) input.value.value = ''

    await queryClient.invalidateQueries({ queryKey: ['preservation'] })
    toasts.success(t('preservation.import.done'))
    emit('imported')
  } catch (error) {
    problem.value = classify(error)
    phase.value = 'idle'
  }
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-import">
    <p class="text-sm text-secondary">{{ t('preservation.import.intro') }}</p>

    <div class="flex flex-wrap items-end gap-3">
      <label class="flex min-w-0 flex-col gap-1">
        <span class="label text-xs">{{ t('preservation.import.file') }}</span>
        <input
          ref="input"
          type="file"
          accept=".jemp"
          class="file-input file-input-sm w-full max-w-sm"
          :disabled="phase === 'previewing' || phase === 'importing'"
          data-testid="import-file"
          @change="choose"
        />
      </label>
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!file || !acceptable || phase === 'previewing' || phase === 'importing'"
        :aria-busy="phase === 'previewing'"
        data-testid="import-preview"
        @click="runPreview"
      >
        <span
          v-if="phase === 'previewing'"
          key="busy"
          class="loading loading-spinner loading-xs"
          aria-hidden="true"
        ></span>
        {{ t('preservation.import.preview') }}
      </button>
    </div>

    <p
      v-if="problem"
      key="problem"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="import-problem"
    >
      <template v-if="problem === 'invalid'">{{ t('preservation.import.invalid') }}</template>
      <template v-else-if="problem === 'forbidden'">{{
        t('preservation.import.forbidden')
      }}</template>
      <template v-else-if="problem === 'none-chosen'">{{
        t('preservation.import.noneChosen')
      }}</template>
      <template v-else>{{ t('preservation.import.failed') }}</template>
    </p>

    <p
      v-if="phase === 'done'"
      key="done"
      role="status"
      class="alert alert-success text-sm"
      data-testid="import-done"
    >
      {{ t('preservation.import.doneDetail') }}
    </p>

    <template v-if="preview">
      <PreservationPreview :preview="preview" />

      <div class="flex flex-col gap-3 rounded-box border border-base-300 p-4">
        <p class="text-sm font-medium" data-testid="import-replaces">
          {{ t('preservation.import.replaces') }}
        </p>
        <label class="flex items-center gap-3 text-sm" :for="confirmId">
          <input
            :id="confirmId"
            v-model="confirmed"
            type="checkbox"
            class="checkbox checkbox-sm"
            :disabled="preview.blockers.length > 0"
            data-testid="import-confirm"
          />
          <span>{{ t('preservation.import.confirm') }}</span>
        </label>
        <div>
          <button
            type="button"
            class="btn btn-sm btn-error"
            :disabled="!ready || phase === 'importing'"
            :aria-busy="phase === 'importing'"
            data-testid="import-run"
            @click="runImport"
          >
            <span
              v-if="phase === 'importing'"
              key="busy"
              class="loading loading-spinner loading-xs"
              aria-hidden="true"
            ></span>
            {{ t('preservation.import.run') }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>
