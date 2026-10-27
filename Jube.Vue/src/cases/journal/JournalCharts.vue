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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ServerChart from '../../components/ServerChart.vue'
import FieldHelp from '../../components/FieldHelp.vue'
import { useUiStore } from '../../stores/ui'
import { chartUrl, type JournalChartKind, type JournalQuery } from './api'
import JournalChartCard from './JournalChartCard.vue'
import type { JournalField } from './types'

const props = defineProps<{ query: JournalQuery; fields: JournalField[] }>()

const { t } = useI18n()
const ui = useUiStore()
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const measures = computed(() => props.fields.filter((field) => field.kind === 'measure'))
const counterparties = computed(() => props.fields.filter((field) => field.kind === 'counterparty'))
const chosen = ref('')
const emptied = ref<string[]>([])

const field = computed(() =>
  measures.value.some((item) => item.name === chosen.value)
    ? chosen.value
    : (measures.value[0]?.name ?? ''),
)

function url(kind: JournalChartKind, name?: string): string {
  return chartUrl(kind, props.query, name, dark.value)
}

function noted(address: string, empty: boolean): void {
  const without = emptied.value.filter((item) => item !== address)

  emptied.value = empty ? [...without, address] : without
}

function choose(event: Event): void {
  chosen.value = (event.target as HTMLSelectElement).value
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="journal-charts">
    <label v-if="measures.length > 0" key="field" class="flex w-fit items-center gap-2">
      <span class="text-xs">{{ t('journal.charts.field') }}</span>
      <FieldHelp :label="t('journal.charts.field')" :text="t('journal.charts.help.field')" />
      <select
        class="select select-sm max-w-xs"
        :value="field"
        data-testid="journal-chart-field"
        @change="choose"
      >
        <option v-for="item in measures" :key="item.name" :value="item.name">
          {{ item.title }}
        </option>
      </select>
    </label>

    <div class="grid min-w-0 gap-3 xl:grid-cols-2">
      <JournalChartCard
        v-if="field !== ''"
        key="progression"
        :title="t('journal.charts.progression')"
        :help="t('journal.charts.help.progression')"
      >
        <ServerChart
          :url="url('Progression', field)"
          :label="t('journal.charts.progression')"
          :stale-seconds="60"
          empty-on-refusal
        />
      </JournalChartCard>
      <JournalChartCard
        v-if="field !== ''"
        key="distribution"
        :title="t('journal.charts.distribution')"
        :help="t('journal.charts.help.distribution')"
      >
        <ServerChart
          :url="url('Distribution', field)"
          :label="t('journal.charts.distribution')"
          :stale-seconds="60"
          empty-on-refusal
        />
      </JournalChartCard>
      <JournalChartCard
        :title="t('journal.charts.velocity')"
        :help="t('journal.charts.help.velocity')"
      >
        <ServerChart
          :url="url('Velocity')"
          :label="t('journal.charts.velocity')"
          :stale-seconds="60"
          empty-on-refusal
        />
      </JournalChartCard>
      <JournalChartCard :title="t('journal.charts.gaps')" :help="t('journal.charts.help.gaps')">
        <ServerChart
          :url="url('Gaps')"
          :label="t('journal.charts.gaps')"
          :stale-seconds="60"
          empty-on-refusal
        />
      </JournalChartCard>
    </div>

    <div class="grid min-w-0 gap-3 xl:grid-cols-2">
      <div v-show="!emptied.includes(url('Correlation'))" class="min-w-0">
        <JournalChartCard
          :title="t('journal.charts.correlation')"
          :help="t('journal.charts.help.correlation')"
        >
          <ServerChart
            :url="url('Correlation')"
            :label="t('journal.charts.correlation')"
            :stale-seconds="60"
            empty-on-refusal
            @empty="noted(url('Correlation'), $event)"
          />
        </JournalChartCard>
      </div>
      <div v-show="!emptied.includes(url('Drift'))" class="min-w-0">
        <JournalChartCard :title="t('journal.charts.drift')" :help="t('journal.charts.help.drift')">
          <ServerChart
            :url="url('Drift')"
            :label="t('journal.charts.drift')"
            :stale-seconds="60"
            empty-on-refusal
            @empty="noted(url('Drift'), $event)"
          />
        </JournalChartCard>
      </div>
      <JournalChartCard
        v-for="item in counterparties"
        :key="item.name"
        :title="`${t('journal.charts.counterparties')}: ${item.title}`"
        :help="t('journal.charts.help.counterparties')"
      >
        <ServerChart
          :url="url('Counterparties', item.name)"
          :label="t('journal.charts.counterparties')"
          :stale-seconds="60"
          empty-on-refusal
        />
      </JournalChartCard>
    </div>
  </section>
</template>
