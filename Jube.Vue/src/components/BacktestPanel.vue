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
import { computed, ref, useId, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { describeApiError } from '../api/problems'
import { ruleBuilderApi } from '../ruleBuilder/api'
import { filtersFromCompletions } from '../ruleBuilder/filters'
import type { BuilderGroup, Completion, FieldDataType } from '../ruleBuilder/types'
import {
  anyActive,
  backtestWindows,
  defaultBacktest,
  instanceRows,
  submitBody,
  validationText,
} from '../ruleTools/model'
import { ruleToolsApi } from '../ruleTools/api'
import type { FilterField } from '../ruleTools/types'
import BacktestInstances from './BacktestInstances.vue'
import BacktestResult from './BacktestResult.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'
import RuleBuilderIsland from './RuleBuilderIsland.vue'

const props = defineProps<{
  modelId: number
  ruleType: string
  ruleId: number | null
  ruleText: string
}>()

const { t } = useI18n()
const queryClient = useQueryClient()
const id = useId()

interface BuilderApi {
  getRules: (lenient?: boolean) => BuilderGroup | null
  validate: () => boolean
}

const filterBuilder = useTemplateRef<BuilderApi>('filterBuilder')
const classBuilder = useTemplateRef<BuilderApi>('classBuilder')

const windowKey = ref<string>(defaultBacktest.window)
const limit = ref<number | null>(defaultBacktest.limit)
const sampleSize = ref<number | null>(defaultBacktest.sampleSize)
const problems = ref<string[]>([])
const note = ref('')
const selectedId = ref<number | null>(null)

const fields = useQuery({
  queryKey: computed(() => queryKeys.backtestFields(props.modelId)),
  queryFn: () => ruleToolsApi.fields(props.modelId),
  retry: false,
})

const operators = useQuery({
  queryKey: queryKeys.ruleOperators(),
  queryFn: ruleBuilderApi.operators,
  retry: false,
})

function completions(items: FilterField[]): Completion[] {
  return items.map((field) => ({
    name: field.name,
    value: field.name,
    meta: '',
    score: 0,
    dataType: field.dataType.toLowerCase() as FieldDataType,
    group: field.name.split('.')[0] ?? field.name,
  }))
}

const ready = computed(() => fields.data.value !== undefined && operators.data.value !== undefined)
const filterSet = computed(() =>
  ready.value
    ? filtersFromCompletions(completions(fields.data.value!.filterFields), operators.data.value!)
    : undefined,
)
const classSet = computed(() =>
  ready.value
    ? filtersFromCompletions(completions(fields.data.value!.classFields), operators.data.value!)
    : undefined,
)
const initialClass = computed<BuilderGroup | undefined>(() => {
  const tag = fields.data.value?.classFields.find((field) => field.name.startsWith('Tag.'))

  return tag
    ? { condition: 'AND', rules: [{ id: tag.name, operator: 'equal', value: 'True' }] }
    : undefined
})

const instances = useQuery({
  queryKey: computed(() =>
    queryKeys.backtestInstances(props.modelId, props.ruleType, props.ruleId),
  ),
  queryFn: () => ruleToolsApi.instances(props.modelId, props.ruleType, props.ruleId),
  retry: false,
  refetchInterval: (query) => (anyActive(query.state.data ?? []) ? 3000 : false),
})

const rows = computed(() => instanceRows(instances.data.value ?? [], props.ruleText))
const selected = computed(() =>
  (instances.data.value ?? []).find((item) => item.id === selectedId.value),
)

function read(builder: BuilderApi | null | undefined): { group: BuilderGroup | null; ok: boolean } {
  if (!builder) return { group: null, ok: true }

  const group = builder.getRules(true)

  return { group, ok: !group || group.rules.length === 0 || builder.validate() }
}

const submit = useMutation({
  mutationFn: () => {
    const filter = read(filterBuilder.value)
    const positive = read(classBuilder.value)

    if (!filter.ok || !positive.ok) throw new Error('incomplete')

    const chosen =
      backtestWindows.find((window) => window.key === windowKey.value) ?? backtestWindows[1]!

    return ruleToolsApi.submit(
      submitBody({
        modelId: props.modelId,
        ruleType: props.ruleType,
        ruleId: props.ruleId,
        ruleText: props.ruleText,
        filter: filter.group,
        positive: positive.group,
        window: chosen,
        limit: limit.value,
        sampleSize: sampleSize.value,
      }),
    )
  },
  onSuccess: async (result) => {
    if (!result.valid || !result.instance) {
      problems.value = [validationText(result.errors) || t('ruleTools.submitFailed')]

      return
    }

    problems.value = []
    note.value = t('ruleTools.submitted')
    selectedId.value = result.instance.id
    await queryClient.invalidateQueries({ queryKey: ['ruleTools', 'instances'] })
  },
  onError: (error) => {
    note.value = ''
    problems.value =
      error instanceof Error && !(error instanceof ApiError)
        ? [t('ruleTools.incomplete')]
        : describeApiError(error, {
            failed: t('ruleTools.submitFailed'),
            refused: t('ruleTools.noPermission'),
          })
  },
})

async function stop(instanceId: number): Promise<void> {
  try {
    await ruleToolsApi.stop(instanceId)
  } finally {
    await queryClient.invalidateQueries({ queryKey: ['ruleTools', 'instances'] })
  }
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="backtest-panel">
    <p class="text-sm text-secondary">{{ t('ruleTools.backtestHint') }}</p>

    <ProblemAlert
      v-if="fields.isError.value || operators.isError.value"
      key="loadFailed"
      :reasons="[t('ruleTools.fieldsFailed')]"
    />
    <p v-else-if="!ready" key="loading" class="text-sm" role="status">
      {{ t('ruleTools.loading') }}
    </p>
    <template v-else-if="filterSet && classSet">
      <section class="flex min-w-0 flex-col gap-1">
        <h3 class="text-sm font-semibold">{{ t('ruleTools.whichTransactions') }}</h3>
        <div class="min-w-0 overflow-auto">
          <RuleBuilderIsland
            ref="filterBuilder"
            :filters="filterSet.filters"
            :operators="operators.data.value!"
            :context="filterSet.context"
          />
        </div>
      </section>
      <section class="flex min-w-0 flex-col gap-1">
        <h3 class="text-sm font-semibold">{{ t('ruleTools.whatIsPositive') }}</h3>
        <div class="min-w-0 overflow-auto">
          <RuleBuilderIsland
            ref="classBuilder"
            :filters="classSet.filters"
            :operators="operators.data.value!"
            :context="classSet.context"
            :initial-rules="initialClass"
          />
        </div>
      </section>

      <form class="flex flex-wrap items-end gap-x-4 gap-y-3" @submit.prevent="submit.mutate()">
        <label class="flex flex-col gap-1" :for="`${id}-window`">
          <span class="label text-xs">{{ t('ruleTools.window.label') }}</span>
          <select
            :id="`${id}-window`"
            v-model="windowKey"
            class="select select-sm"
            data-testid="backtest-window"
          >
            <option value="week">{{ t('ruleTools.window.week') }}</option>
            <option value="month">{{ t('ruleTools.window.month') }}</option>
            <option value="quarter">{{ t('ruleTools.window.quarter') }}</option>
            <option value="year">{{ t('ruleTools.window.year') }}</option>
            <option value="all">{{ t('ruleTools.window.all') }}</option>
          </select>
        </label>
        <label class="flex flex-col gap-1" :for="`${id}-limit`">
          <span class="label text-xs">{{ t('ruleTools.limit') }}</span>
          <input
            :id="`${id}-limit`"
            v-model.number="limit"
            type="number"
            min="1"
            class="input input-sm w-36 font-mono"
            data-testid="backtest-limit"
          />
        </label>
        <label class="flex flex-col gap-1" :for="`${id}-sample`">
          <span class="label text-xs">{{ t('ruleTools.sampleSize') }}</span>
          <input
            :id="`${id}-sample`"
            v-model.number="sampleSize"
            type="number"
            min="0"
            max="50"
            class="input input-sm w-24 font-mono"
            data-testid="backtest-sample"
          />
        </label>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="submit.isPending.value"
          data-testid="backtest-run"
        >
          {{ t('ruleTools.run') }}
        </button>
      </form>
    </template>

    <p v-if="note" key="note" class="text-sm" role="status" data-testid="backtest-note">
      {{ note }}
    </p>
    <ProblemAlert
      v-if="problems.length > 0"
      key="problems"
      :reasons="problems"
      data-testid="backtest-problems"
    />

    <BacktestInstances :rows="rows" @show="selectedId = $event" @stop="stop" />

    <PanelCard
      v-if="selected"
      key="result"
      :title="t('ruleTools.resultTitle', { id: selected.id })"
      compact
    >
      <p
        v-if="selected.status !== 'Succeeded'"
        key="notDone"
        class="text-sm"
        data-testid="backtest-notdone"
      >
        {{ t('ruleTools.notSucceeded', { status: selected.status })
        }}{{ selected.error ? `: ${selected.error}` : '' }}
      </p>
      <BacktestResult
        v-else
        :key="selected.id"
        :instance-id="selected.id"
        :request="selected.request"
      />
    </PanelCard>
  </div>
</template>
