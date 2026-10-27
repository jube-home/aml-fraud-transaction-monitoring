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
import ActivationWatcher from '../../components/ActivationWatcher.vue'

const { t } = useI18n()

const embedded = ref(false)
const key = ref('')
const keyValue = ref('')
const applied = ref({ key: '', keyValue: '' })

function apply(): void {
  applied.value = { key: key.value, keyValue: keyValue.value }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <form class="flex shrink-0 flex-wrap items-end gap-3" @submit.prevent="apply">
      <label class="flex items-center gap-2 text-sm">
        <input
          v-model="embedded"
          type="checkbox"
          class="toggle toggle-sm toggle-primary"
          data-testid="watcher-embedded"
        />
        <span>{{ t('watcher.bench.embedded') }}</span>
      </label>
      <template v-if="embedded">
        <label class="flex flex-col gap-1 text-sm">
          <span class="label">{{ t('watcher.bench.key') }}</span>
          <input v-model="key" type="text" class="input input-sm" data-testid="watcher-host-key" />
        </label>
        <label class="flex flex-col gap-1 text-sm">
          <span class="label">{{ t('watcher.bench.keyValue') }}</span>
          <input
            v-model="keyValue"
            type="text"
            class="input input-sm"
            data-testid="watcher-host-value"
          />
        </label>
        <button type="submit" class="btn btn-sm" data-testid="watcher-host-apply">
          {{ t('override.bench.apply') }}
        </button>
      </template>
    </form>

    <ActivationWatcher
      v-if="embedded"
      :key="`${applied.key}|${applied.keyValue}`"
      :key-filter="applied.key || undefined"
      :key-value-filter="applied.keyValue || undefined"
      :show-controls="false"
      :show-feed="false"
      mode="all"
    />
    <ActivationWatcher v-else key="full" />
  </div>
</template>
