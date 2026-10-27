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
import { queryKeys } from '../api/queryKeys'
import { useFormat } from '../composables/useFormat'
import { chartBody, figures } from '../ruleTools/model'
import { ruleToolsApi } from '../ruleTools/api'
import type { BacktestRequest } from '../ruleTools/types'
import { useUiStore } from '../stores/ui'
import BacktestExampleGroup from './BacktestExampleGroup.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ instanceId: number; request: BacktestRequest }>()

const { t } = useI18n()
const { dateTime } = useFormat()
const ui = useUiStore()

const result = useQuery({
  queryKey: computed(() => queryKeys.backtestResult(props.instanceId)),
  queryFn: () => ruleToolsApi.result(props.instanceId),
  retry: false,
  staleTime: Infinity,
})

const data = computed(() => result.data.value)
const body = computed(() =>
  data.value ? chartBody(data.value, ui.resolvedTheme === 'jube-dark') : undefined,
)
const rates = computed(() => (data.value ? figures(data.value) : undefined))
const base = '/api/Chart/Backtest'

const groups = computed(() => {
  const value = data.value

  return value
    ? [
        { title: t('ruleTools.examples.truePositives'), samples: value.truePositiveSamples },
        { title: t('ruleTools.examples.falsePositives'), samples: value.falsePositiveSamples },
        { title: t('ruleTools.examples.falseNegatives'), samples: value.falseNegativeSamples },
        { title: t('ruleTools.examples.errors'), samples: value.errorSamples },
      ].filter((group) => group.samples.length > 0)
    : []
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="backtest-result">
    <p v-if="result.isPending.value" key="loading" class="text-sm" role="status">
      {{ t('ruleTools.loading') }}
    </p>
    <ProblemAlert
      v-else-if="result.isError.value"
      key="failed"
      :reasons="[t('ruleTools.result.failed')]"
    />
    <template v-else-if="data && body">
      <p class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-secondary" data-testid="backtest-range">
        <span
          v-if="data.earliestReferenceDate && data.latestReferenceDate"
          class="font-mono tabular-nums"
        >
          {{ dateTime(new Date(data.earliestReferenceDate)) }}
          {{ '–' }}
          {{ dateTime(new Date(data.latestReferenceDate)) }}
        </span>
        <span v-if="data.limitReached">{{ t('ruleTools.result.limitReached') }}</span>
        <span v-if="data.aborted">{{
          t('ruleTools.result.aborted', { reason: data.abortReason ?? '' })
        }}</span>
        <span v-if="data.runtimeErrors > 0" class="text-error">{{
          t('ruleTools.result.runtimeErrors', { count: data.runtimeErrors })
        }}</span>
        <span v-if="data.filterErrors > 0" class="text-error">{{
          t('ruleTools.result.filterErrors', { count: data.filterErrors })
        }}</span>
      </p>

      <p
        v-if="data.evaluated === 0"
        key="nothing"
        class="text-sm"
        role="status"
        data-testid="backtest-nothing"
      >
        {{ t('ruleTools.result.nothingEvaluated') }}
      </p>

      <div class="grid min-w-0 gap-3 @container @3xl:grid-cols-2" data-testid="backtest-charts">
        <PanelCard :title="t('ruleTools.result.funnel')" compact>
          <ServerChart
            :url="`${base}/Funnel`"
            :body="body"
            :label="t('ruleTools.result.funnel')"
            :stale-seconds="3600"
          />
        </PanelCard>
        <PanelCard
          v-if="data.classDefined && data.evaluated > 0"
          key="confusion"
          :title="t('ruleTools.result.confusion')"
          compact
        >
          <ServerChart
            :url="`${base}/Confusion`"
            :body="body"
            :label="t('ruleTools.result.confusion')"
            :stale-seconds="3600"
          />
        </PanelCard>
        <PanelCard
          v-if="data.evaluated > 0"
          key="rates"
          :title="t('ruleTools.result.rates')"
          compact
        >
          <ServerChart
            :url="`${base}/Rates`"
            :body="body"
            :label="t('ruleTools.result.rates')"
            :stale-seconds="3600"
          />
          <dl
            v-if="rates"
            class="grid grid-cols-2 gap-x-4 text-xs text-secondary"
            data-testid="backtest-figures"
          >
            <dt>{{ t('ruleTools.col.precision') }}</dt>
            <dd class="font-mono">
              {{
                rates.precision === null
                  ? t('ruleTools.result.notAvailable')
                  : `${(rates.precision * 100).toFixed(1)}%`
              }}
            </dd>
            <dt>{{ t('ruleTools.col.recall') }}</dt>
            <dd class="font-mono">
              {{
                rates.recall === null
                  ? t('ruleTools.result.notAvailable')
                  : `${(rates.recall * 100).toFixed(1)}%`
              }}
            </dd>
          </dl>
        </PanelCard>
        <PanelCard
          v-if="data.tagsInSample.length > 0"
          key="tags"
          :title="t('ruleTools.result.tags')"
          compact
        >
          <ServerChart
            :url="`${base}/Tags`"
            :body="body"
            :label="t('ruleTools.result.tags')"
            :stale-seconds="3600"
          />
        </PanelCard>
      </div>

      <div class="flex flex-col gap-2">
        <BacktestExampleGroup
          v-for="group in groups"
          :key="group.title"
          :title="group.title"
          :samples="group.samples"
          :request="request"
        />
      </div>
    </template>
  </div>
</template>
