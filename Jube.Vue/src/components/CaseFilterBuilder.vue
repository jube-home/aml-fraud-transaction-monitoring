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
import { caseWorkflowKey, filterBuilderApi, type CaseWorkflowRef } from '../filterBuilder/api'
import {
  caseFilterSets,
  caseSelectOperators,
  caseWhereOperators,
  type CaseStatus,
  type FilterIslandApi,
  type FilterLabels,
  type SqlFilter,
} from '../filterBuilder/filters'
import { emptyGroup, prepareInitialRules } from '../filterBuilder/initial'
import { builderProblem, type Problem } from '../ruleBuilder/problems'
import type { BuilderGroup, CompileState } from '../ruleBuilder/types'
import BuilderStatus from './BuilderStatus.vue'
import FilterBuilderIsland from './FilterBuilderIsland.vue'

export interface CaseFilterValue {
  selectJson: string
  filterJson: string
  valid: boolean
}

const props = defineProps<{
  caseWorkflow: CaseWorkflowRef
  initialValue?: { selectJson?: string | BuilderGroup; filterJson?: string | BuilderGroup }
}>()

const emit = defineEmits<{
  change: [value: CaseFilterValue]
  loadFailed: []
}>()

const settleDelayMs = 600

const { t } = useI18n()
const queryClient = useQueryClient()

const labels: FilterLabels = {
  yes: t('filterBuilder.yes'),
  no: t('filterBuilder.no'),
  ascending: t('filterBuilder.ascending'),
  descending: t('filterBuilder.descending'),
  closedStatus: [
    t('filterBuilder.closedStatus0'),
    t('filterBuilder.closedStatus1'),
    t('filterBuilder.closedStatus2'),
    t('filterBuilder.closedStatus3'),
    t('filterBuilder.closedStatus4'),
  ],
  priority: [
    t('filterBuilder.priority1'),
    t('filterBuilder.priority2'),
    t('filterBuilder.priority3'),
    t('filterBuilder.priority4'),
    t('filterBuilder.priority5'),
  ],
  diaryType: [
    t('filterBuilder.diaryType1'),
    t('filterBuilder.diaryType2'),
    t('filterBuilder.diaryType3'),
    t('filterBuilder.diaryType4'),
  ],
}

const loaded = ref(false)
const loadFailed = ref(false)
const filters = shallowRef<SqlFilter[]>([])
const selects = shallowRef<SqlFilter[]>([])
const initialFilter = ref<BuilderGroup>()
const initialSelect = ref<BuilderGroup>()
const dropped = ref<string[]>([])
const restoreFailed = ref(false)
const state = ref<CompileState>('incomplete')
const problems = ref<Problem[]>([])
const selectIsland = ref<FilterIslandApi>()
const whereIsland = ref<FilterIslandApi>()

const targets = new WeakMap<Problem, 'select' | 'where'>()

let settleTimer: ReturnType<typeof setTimeout> | undefined

function evaluate(): boolean {
  const select = selectIsland.value
  const where = whereIsland.value

  if (!select || !where) return false

  const found: Problem[] = []
  const selectHas = select.hasRules()
  const whereHas = where.hasRules()

  const check = (
    island: FilterIslandApi,
    has: boolean,
    target: 'select' | 'where',
    title: string,
  ): void => {
    if (!has || island.validate()) return

    for (const message of island.validationMessages()) {
      const problem = builderProblem(`${title}: ${message}`)

      targets.set(problem, target)
      found.push(problem)
    }
  }

  check(select, selectHas, 'select', t('filterBuilder.select'))
  check(where, whereHas, 'where', t('filterBuilder.where'))

  problems.value = found
  state.value = found.length > 0 ? 'errors' : selectHas || whereHas ? 'valid' : 'incomplete'

  return found.length === 0
}

function currentValue(): CaseFilterValue {
  const valid = evaluate()

  return {
    selectJson: JSON.stringify(selectIsland.value?.getRules() ?? emptyGroup, null, 2),
    filterJson: JSON.stringify(whereIsland.value?.getRules() ?? emptyGroup, null, 2),
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
  if (!selectIsland.value || !whereIsland.value) return

  evaluate()
  emit('change', currentValue())
}

function showProblem(problem: Problem): void {
  const target = targets.get(problem)

  ;(target === 'select' ? selectIsland.value : whereIsland.value)?.focusFirstError()
}

async function loadStatuses(ref: CaseWorkflowRef): Promise<CaseStatus[]> {
  try {
    const statuses = await queryClient.fetchQuery({
      queryKey: queryKeys.caseFilterStatuses(caseWorkflowKey(ref)),
      queryFn: () => filterBuilderApi.caseStatuses(ref),
      retry: false,
    })

    return Array.isArray(statuses) ? statuses : []
  } catch {
    return []
  }
}

onMounted(async () => {
  try {
    const ref = props.caseWorkflow
    const [completions, statuses] = await Promise.all([
      queryClient.fetchQuery({
        queryKey: queryKeys.caseFilterCompletions(caseWorkflowKey(ref)),
        queryFn: () => filterBuilderApi.caseCompletions(ref),
      }),
      loadStatuses(ref),
    ])

    const sets = caseFilterSets(completions, statuses, labels)

    filters.value = sets.filters
    selects.value = sets.selects

    const restoredFilter = prepareInitialRules(
      props.initialValue?.filterJson,
      new Set(sets.filters.map((filter) => filter.id)),
    )
    const restoredSelect = prepareInitialRules(
      props.initialValue?.selectJson,
      new Set(sets.selects.map((select) => select.id)),
    )

    initialFilter.value = restoredFilter.group
    initialSelect.value = restoredSelect.group
    dropped.value = [...restoredSelect.dropped, ...restoredFilter.dropped]
    restoreFailed.value = restoredFilter.failed || restoredSelect.failed

    loaded.value = true
  } catch {
    loadFailed.value = true
    emit('loadFailed')
  }
})

onBeforeUnmount(() => clearTimeout(settleTimer))

function getValue(): CaseFilterValue {
  return currentValue()
}

defineExpose({ getValue })
</script>

<template>
  <section class="flex flex-col gap-3" data-testid="case-filter-builder">
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

    <template v-else>
      <section class="flex min-w-0 flex-col gap-2" data-testid="case-select-section">
        <h3 class="text-sm font-semibold">{{ t('filterBuilder.select') }}</h3>
        <div class="min-w-0 overflow-auto">
          <FilterBuilderIsland
            ref="selectIsland"
            test-id="select-builder"
            columns
            :add-label="t('filterBuilder.addColumn')"
            :filters="selects"
            :operators="caseSelectOperators"
            :initial-rules="initialSelect"
            @ready="onReady"
            @change="onChanged"
            @restore-failed="restoreFailed = true"
            @load-failed="loadFailed = true"
          />
        </div>
      </section>

      <section class="flex min-w-0 flex-col gap-2" data-testid="case-where-section">
        <h3 class="text-sm font-semibold">{{ t('filterBuilder.where') }}</h3>
        <div class="min-w-0 overflow-auto">
          <FilterBuilderIsland
            ref="whereIsland"
            test-id="where-builder"
            :filters="filters"
            :operators="caseWhereOperators"
            :initial-rules="initialFilter"
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
