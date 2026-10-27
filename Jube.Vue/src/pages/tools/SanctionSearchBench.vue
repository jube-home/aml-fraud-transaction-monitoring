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
import SanctionSearch from '../../components/SanctionSearch.vue'

const { t } = useI18n()

const embedded = ref(false)
const text = ref('')
const applied = ref('')

function apply(): void {
  applied.value = text.value
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
          data-testid="sanction-embedded"
        />
        <span>{{ t('sanction.bench.embedded') }}</span>
      </label>
      <label v-if="embedded" key="host" class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('sanction.bench.hostText') }}</span>
        <input
          v-model="text"
          type="search"
          class="input input-sm"
          data-testid="sanction-host-text"
        />
      </label>
      <button
        v-if="embedded"
        key="apply"
        type="submit"
        class="btn btn-sm"
        data-testid="sanction-host-apply"
      >
        {{ t('sanction.bench.apply') }}
      </button>
    </form>

    <SanctionSearch
      v-if="embedded"
      key="embedded"
      :multi-part-string="applied"
      :show-form="false"
      auto-run
    />
    <SanctionSearch v-else key="full" />
  </div>
</template>
