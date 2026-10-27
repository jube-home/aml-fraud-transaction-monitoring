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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { RefreshCw } from 'lucide-vue-next'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { useFormat } from '../composables/useFormat'
import { integrityApi } from '../integrity/api'
import { verdictOf } from '../integrity/model'

const props = defineProps<{ modelId: number }>()

const emit = defineEmits<{ check: [] }>()

const { t } = useI18n()
const { dateTime } = useFormat()

const report = useQuery({
  queryKey: computed(() => queryKeys.integrityReport(props.modelId)),
  queryFn: () => integrityApi.report(props.modelId),
  retry: false,
})

const data = computed(() => report.data.value)
const verdict = computed(() => (data.value ? verdictOf(data.value) : undefined))
const forbidden = computed(
  () => report.error.value instanceof ApiError && report.error.value.kind === 'forbidden',
)

const label = computed(() => {
  switch (verdict.value) {
    case 'unsound':
      return t('integrity.verdict.unsound')
    case 'warnings':
      return t('integrity.verdict.warnings')
    default:
      return t('integrity.verdict.sound')
  }
})
</script>

<template>
  <div
    class="sticky top-0 z-20 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-box border border-base-300 border-t-2 border-t-accent bg-base-100 px-4 py-2"
    data-testid="integrity-band"
  >
    <div v-if="report.isPending.value" key="loading" class="skeleton h-8 w-72" role="status"></div>
    <p
      v-else-if="forbidden"
      key="forbidden"
      class="text-sm font-semibold"
      data-testid="integrity-forbidden"
    >
      {{ t('integrity.forbidden') }}
    </p>
    <p v-else-if="report.isError.value" key="failed" class="text-sm font-semibold text-error">
      {{ t('integrity.failed') }}
    </p>
    <template v-else-if="data">
      <p class="flex items-center gap-2 text-sm font-semibold" data-testid="integrity-verdict">
        <span
          :class="[
            'status',
            verdict === 'unsound'
              ? 'status-error'
              : verdict === 'warnings'
                ? 'status-warning'
                : 'status-success',
          ]"
          aria-hidden="true"
        ></span>
        {{ label }}
      </p>
      <p class="font-mono text-xs tabular-nums" data-testid="integrity-summary">
        {{
          t('integrity.summary', {
            errors: data.errors,
            warnings: data.warnings,
            infos: data.infos,
          })
        }}
      </p>
      <p class="font-mono text-xs tabular-nums text-secondary">
        {{ t('integrity.checked', { when: dateTime(new Date(data.checkedDate)) }) }}
      </p>
    </template>

    <button
      type="button"
      class="btn btn-sm ms-auto"
      :disabled="report.isFetching.value"
      data-testid="integrity-check"
      @click="emit('check')"
    >
      <RefreshCw aria-hidden="true" class="size-4" />
      {{ t('integrity.check') }}
    </button>
  </div>
</template>
