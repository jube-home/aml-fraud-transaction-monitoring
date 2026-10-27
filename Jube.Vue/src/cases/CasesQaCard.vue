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
import { useFormat } from '../composables/useFormat'
import { casesApi } from './api'

const props = defineProps<{ workflow: string; from: string }>()

const { t } = useI18n()
const { number, percent } = useFormat()

const report = useQuery({
  queryKey: computed(() => ['service', 'Case', 'qaSample', props.workflow, props.from] as const),
  queryFn: () => casesApi.qaSample(props.workflow, props.from),
  staleTime: 60_000,
  retry: false,
})

const data = computed(() => report.data.value?.qaSample)

function rate(value: number | null | undefined): string {
  return value === null || value === undefined ? t('cases.headline.none') : percent(value)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="cases-qa-card">
    <div v-if="report.isPending.value" key="loading" class="skeleton h-24 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>

    <div
      v-else-if="report.isError.value || !data"
      key="failed"
      role="alert"
      class="alert alert-error"
    >
      <span>{{ t('cases.supervision.failed') }}</span>
    </div>

    <template v-else>
      <p v-if="data.picked === 0" key="none" class="text-sm" data-testid="cases-qa-none">
        {{ t('cases.qa.none') }}
      </p>

      <dl v-else key="figures" class="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div class="flex flex-col gap-1">
          <dt class="text-xs">{{ t('cases.qa.picked') }}</dt>
          <dd class="font-mono text-xl tabular-nums" data-testid="cases-qa-picked">
            {{ number(data.picked) }}
          </dd>
        </div>
        <div class="flex flex-col gap-1">
          <dt class="text-xs">{{ t('cases.qa.open') }}</dt>
          <dd class="font-mono text-xl tabular-nums">{{ number(data.open) }}</dd>
        </div>
        <div class="flex flex-col gap-1">
          <dt class="text-xs">{{ t('cases.qa.decided') }}</dt>
          <dd class="font-mono text-xl tabular-nums">{{ number(data.decided) }}</dd>
        </div>
        <div class="flex flex-col gap-1">
          <dt class="text-xs">{{ t('cases.qa.rate') }}</dt>
          <dd class="font-mono text-xl tabular-nums" data-testid="cases-qa-rate">
            {{ rate(data.confirmedRate) }}
          </dd>
        </div>
      </dl>

      <p v-if="data.picked > 0" key="compare" class="text-sm">
        {{ t('cases.qa.compare', { overall: rate(data.overallConfirmedRate) }) }}
      </p>
      <p
        v-if="data.picked > 0 && !data.enough"
        key="few"
        class="text-sm"
        data-testid="cases-qa-few"
      >
        {{ t('cases.qa.few') }}
      </p>
    </template>
  </div>
</template>
