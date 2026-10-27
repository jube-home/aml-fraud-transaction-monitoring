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
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useToasts } from '../stores/toasts'
import { snapshotApi } from './api'

const { t } = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()

const name = ref('')
const busy = ref(false)

async function take(): Promise<void> {
  busy.value = true

  try {
    await snapshotApi.take({
      name: name.value.trim() === '' ? null : name.value.trim(),
    })
    name.value = ''
    await queryClient.invalidateQueries({ queryKey: queryKeys.preservationSnapshots() })
    toasts.success(t('preservation.take.done'))
  } catch (error) {
    toasts.apiError(error)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="preservation-take">
    <p class="text-sm text-secondary">{{ t('preservation.take.intro') }}</p>
    <div class="flex flex-wrap items-end gap-4">
      <label class="flex flex-col gap-1">
        <span class="label text-xs">{{ t('preservation.take.name') }}</span>
        <input
          v-model="name"
          type="text"
          maxlength="200"
          class="input input-sm w-72 max-w-full"
          data-testid="take-name"
        />
      </label>
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="busy"
        :aria-busy="busy"
        data-testid="take-run"
        @click="take"
      >
        <span
          v-if="busy"
          key="busy"
          class="loading loading-spinner loading-xs"
          aria-hidden="true"
        ></span>
        {{ t('preservation.take.run') }}
      </button>
    </div>
  </div>
</template>
