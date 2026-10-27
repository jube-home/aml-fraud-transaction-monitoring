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
import OverrideManager from '../../components/OverrideManager.vue'

const { t } = useI18n()

const embedded = ref(false)
const key = ref('')
const value = ref('')
const applied = ref({ key: '', value: '' })

function apply(): void {
  applied.value = { key: key.value, value: value.value }
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
          data-testid="override-embedded"
        />
        <span>{{ t('override.bench.embedded') }}</span>
      </label>
      <template v-if="embedded">
        <label class="flex flex-col gap-1 text-sm">
          <span class="label">{{ t('override.bench.hostKey') }}</span>
          <input v-model="key" type="text" class="input input-sm" data-testid="override-host-key" />
        </label>
        <label class="flex flex-col gap-1 text-sm">
          <span class="label">{{ t('override.bench.hostValue') }}</span>
          <input
            v-model="value"
            type="text"
            class="input input-sm"
            data-testid="override-host-value"
          />
        </label>
        <button type="submit" class="btn btn-sm" data-testid="override-host-apply">
          {{ t('override.bench.apply') }}
        </button>
      </template>
    </form>

    <OverrideManager
      v-if="embedded"
      key="embedded"
      :override-key="applied.key"
      :override-key-value="applied.value"
      :show-selector="false"
    />
    <OverrideManager v-else key="full" />
  </div>
</template>
