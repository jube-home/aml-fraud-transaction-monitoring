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
import { onBeforeUnmount, onMounted, ref, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { TriangleAlert } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { exhaustiveSourceKey, filterBuilderApi, type ExhaustiveSource } from '../filterBuilder/api'
import {
  exhaustiveFilters,
  exhaustiveOperators,
  type FilterIslandApi,
  type SqlFilter,
} from '../filterBuilder/filters'
import { emptyGroup, prepareInitialRules } from '../filterBuilder/initial'
import { builderProblem, type Problem } from '../ruleBuilder/problems'
import type { BuilderGroup, CompileState } from '../ruleBuilder/types'
import BuilderStatus from './BuilderStatus.vue'
import FilterBuilderIsland from './FilterBuilderIsland.vue'

export interface ExhaustiveFilterValue {
  filterSql: string
  filterJson: string
  filterTokens: string
  valid: boolean
}

const props = defineProps<{
  source: ExhaustiveSource
  initialValue?: { filterJson?: string | BuilderGroup }
}>()

const emit = defineEmits<{
  change: [value: ExhaustiveFilterValue]
  loadFailed: []
}>()

const settleDelayMs = 600

const { t } = useI18n()
const queryClient = useQueryClient()

const loaded = ref(false)
const loadFailed = ref(false)
const filters = shallowRef<SqlFilter[]>([])
const initialRules = ref<BuilderGroup>()
const dropped = ref<string[]>([])
const restoreFailed = ref(false)
const state = ref<CompileState>('incomplete')
const problems = ref<Problem[]>([])
const island = ref<FilterIslandApi>()

let settleTimer: ReturnType<typeof setTimeout> | undefined

function currentValue(): ExhaustiveFilterValue {
  const where = island.value

  if (!where)
    return {
      filterSql: '',
      filterJson: JSON.stringify(emptyGroup),
      filterTokens: '[]',
      valid: false,
    }

  const has = where.hasRules()
  const valid = has ? where.validate() : true
  const found: Problem[] = []

  if (!valid) {
    for (const message of where.validationMessages()) found.push(builderProblem(message))
  }

  const sql = valid && has ? where.getSql() : null

  problems.value = found
  state.value = found.length > 0 ? 'errors' : has ? 'valid' : 'incomplete'

  return {
    filterSql: sql?.sql ?? '',
    filterJson: JSON.stringify(where.getRules() ?? emptyGroup, null, 2),
    filterTokens: JSON.stringify(sql?.params ?? [], null, 2),
    valid,
  }
}

function onChanged(): void {
  clearTimeout(settleTimer)

  settleTimer = setTimeout(() => {
    if (loaded.value) emit('change', currentValue())
  }, settleDelayMs)
}

function onReady(): void {
  emit('change', currentValue())
}

function showProblem(): void {
  island.value?.focusFirstError()
}

onMounted(async () => {
  try {
    const source = props.source
    const completions = await queryClient.fetchQuery({
      queryKey: queryKeys.exhaustiveFilterCompletions(exhaustiveSourceKey(source)),
      queryFn: () => filterBuilderApi.exhaustiveCompletions(source),
    })

    filters.value = exhaustiveFilters(completions)

    const restored = prepareInitialRules(
      props.initialValue?.filterJson,
      new Set(filters.value.map((filter) => filter.id)),
    )

    initialRules.value = restored.group
    dropped.value = restored.dropped
    restoreFailed.value = restored.failed

    loaded.value = true
  } catch {
    loadFailed.value = true
    emit('loadFailed')
  }
})

onBeforeUnmount(() => clearTimeout(settleTimer))

function getValue(): ExhaustiveFilterValue {
  return currentValue()
}

defineExpose({ getValue })
</script>

<template>
  <section class="flex flex-col gap-3" data-testid="exhaustive-filter-builder">
    <div
      v-if="loadFailed"
      role="alert"
      class="alert alert-error"
      data-testid="filter-builder-load-failed"
    >
      <TriangleAlert aria-hidden="true" class="size-4" />
      <span>{{ t('filterBuilder.loadFailed') }}</span>
    </div>

    <p v-else-if="!loaded" class="flex items-center gap-2 text-sm" role="status">
      <span class="loading loading-spinner loading-sm" aria-hidden="true"></span>
      {{ t('filterBuilder.loading') }}
    </p>

    <p v-else-if="filters.length === 0" class="text-sm" data-testid="filter-builder-no-columns">
      {{ t('filterBuilder.noColumns') }}
    </p>

    <template v-else>
      <section class="flex min-w-0 flex-col gap-2">
        <h3 class="text-sm font-semibold">{{ t('filterBuilder.where') }}</h3>
        <div class="min-w-0 overflow-auto">
          <FilterBuilderIsland
            ref="island"
            test-id="where-builder"
            :filters="filters"
            :operators="exhaustiveOperators"
            :initial-rules="initialRules"
            @ready="onReady"
            @change="onChanged"
            @restore-failed="restoreFailed = true"
            @load-failed="loadFailed = true"
          />
        </div>
      </section>

      <p
        v-if="dropped.length > 0 || restoreFailed"
        role="status"
        class="alert text-sm"
        data-testid="filter-builder-warning"
      >
        <TriangleAlert aria-hidden="true" class="size-4" />
        <span>{{
          dropped.length > 0
            ? t('filterBuilder.droppedFields', { fields: dropped.join(', ') })
            : t('filterBuilder.restoreFailed')
        }}</span>
      </p>
      <BuilderStatus :state="state" :problems="problems" @select="showProblem" />
    </template>
  </section>
</template>
