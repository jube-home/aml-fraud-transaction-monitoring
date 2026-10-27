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
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { meQueryOptions, hasPermission } from '../auth/me'
import { useToasts } from '../stores/toasts'
import PreservationPreview from './PreservationPreview.vue'
import { snapshotApi } from './api'
import { canImport } from './state'
import type { ImportPreview } from './types'

const props = defineProps<{ id: number; name: string }>()

const emit = defineEmits<{ close: []; restored: [] }>()

const { t } = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()
const confirmId = useId()

const me = useQuery(meQueryOptions)

const preview = ref<ImportPreview>()
const confirmed = ref(false)
const loading = ref(true)
const busy = ref(false)
const problem = ref<'preview' | 'restore' | null>(null)

const permitted = computed(() => hasPermission(me.data.value, 63))
const ready = computed(() => permitted.value && canImport(preview.value, confirmed.value))

async function load(): Promise<void> {
  loading.value = true
  problem.value = null
  preview.value = undefined
  confirmed.value = false

  try {
    preview.value = await snapshotApi.previewRestore(props.id)
  } catch {
    problem.value = 'preview'
  } finally {
    loading.value = false
  }
}

async function restore(): Promise<void> {
  if (!ready.value) return

  busy.value = true
  problem.value = null

  try {
    await snapshotApi.restore(props.id)
    await queryClient.invalidateQueries({ queryKey: ['preservation'] })
    toasts.success(t('preservation.restore.done'))
    emit('restored')
  } catch {
    problem.value = 'restore'
  } finally {
    busy.value = false
  }
}

watch(() => props.id, load, { immediate: true })
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-restore">
    <div class="flex flex-wrap items-center gap-3">
      <p class="text-sm">
        {{ t('preservation.restore.target') }}
        <span class="badge badge-sm font-mono" data-testid="restore-target">{{ name }}</span>
      </p>
      <button
        type="button"
        class="btn btn-sm btn-ghost"
        data-testid="restore-close"
        @click="emit('close')"
      >
        {{ t('preservation.restore.close') }}
      </button>
    </div>

    <div
      v-if="loading"
      key="loading"
      class="skeleton h-32 w-full"
      data-testid="restore-loading"
    ></div>

    <p
      v-if="problem"
      key="problem"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="restore-problem"
    >
      {{
        problem === 'preview'
          ? t('preservation.restore.previewFailed')
          : t('preservation.restore.failed')
      }}
    </p>

    <template v-if="preview">
      <PreservationPreview :preview="preview" />

      <div class="flex flex-col gap-3 rounded-box border border-base-300 p-4">
        <p class="text-sm font-medium">{{ t('preservation.restore.replaces') }}</p>
        <p
          v-if="!permitted"
          key="permission"
          role="status"
          class="alert alert-warning text-sm"
          data-testid="restore-not-permitted"
        >
          {{ t('preservation.restore.notPermitted') }}
        </p>
        <label class="flex items-center gap-3 text-sm" :for="confirmId">
          <input
            :id="confirmId"
            v-model="confirmed"
            type="checkbox"
            class="checkbox checkbox-sm"
            :disabled="!permitted || preview.blockers.length > 0"
            data-testid="restore-confirm"
          />
          <span>{{ t('preservation.restore.confirm') }}</span>
        </label>
        <div>
          <button
            type="button"
            class="btn btn-sm btn-error"
            :disabled="!ready || busy"
            :aria-busy="busy"
            data-testid="restore-run"
            @click="restore"
          >
            <span
              v-if="busy"
              key="busy"
              class="loading loading-spinner loading-xs"
              aria-hidden="true"
            ></span>
            {{ t('preservation.restore.run') }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>
