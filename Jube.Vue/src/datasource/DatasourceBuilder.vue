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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { keepPreviousData, useMutation, useQuery } from '@tanstack/vue-query'
import { Play } from 'lucide-vue-next'
import PanelCard from '../components/PanelCard.vue'
import { queryKeys } from '../api/queryKeys'
import { useDebounced } from '../composables/useDebounced'
import DatasourceCardView from './DatasourceCardView.vue'
import DatasourceColumnStrip from './DatasourceColumnStrip.vue'
import DatasourceDisplayPicker from './DatasourceDisplayPicker.vue'
import DatasourceSqlEditor from './DatasourceSqlEditor.vue'
import DatasourceSqlStatus from './DatasourceSqlStatus.vue'
import VisualisationParameters from '../dashboard/VisualisationParameters.vue'
import { dashboardApi } from '../dashboard/api'
import { cardParameters, initialValues, missingRequired } from '../dashboard/state'
import type { ParameterValues } from '../dashboard/types'
import { cardRequest, datasourceApi } from './api'
import { knownStylesheetVersion } from './chartStylesheet'
import {
  columnRoles,
  dropMissingColumns,
  isValid,
  normaliseMapping,
  sameMapping,
  sqlState,
} from './state'
import type { CardParameter, DatasourceValue, MappingEntry } from './types'

const props = defineProps<{
  registryId: number
  initialValue?: DatasourceValue
  locked?: boolean
  parameters?: CardParameter[]
}>()

const emit = defineEmits<{ change: [value: DatasourceValue] }>()

const { t } = useI18n()

const command = ref(props.initialValue?.command ?? '')
const displayKindId = ref(props.initialValue?.displayKindId ?? 0)
const includeGrid = ref(props.initialValue?.includeGrid ?? false)
const mapping = ref<MappingEntry[]>(normaliseMapping(props.initialValue?.mapping ?? []))
const templateText = ref(props.initialValue?.templateText ?? '')

const definitionsQuery = useQuery({
  queryKey: computed(() => queryKeys.visualisationParameters(props.registryId)),
  queryFn: () => dashboardApi.parameters(props.registryId),
})

const definitions = computed(() => definitionsQuery.data.value ?? [])
const parameterValues = ref<ParameterValues>({})
const missingParameters = ref<number[]>([])
const appliedParameters = ref<CardParameter[]>()

const previewParameters = computed(() => appliedParameters.value ?? props.parameters ?? [])

watch(
  definitions,
  (loaded) => {
    parameterValues.value = initialValues(loaded, new Date())
    appliedParameters.value = undefined
  },
  { immediate: true },
)

function changeParameter(id: number, value: string): void {
  parameterValues.value = { ...parameterValues.value, [id]: value }
}

function runParameters(): void {
  const gaps = missingRequired(definitions.value, parameterValues.value)

  missingParameters.value = gaps

  if (gaps.length === 0)
    appliedParameters.value = cardParameters(definitions.value, parameterValues.value)
}

const settled = useDebounced(command, 600)
const settledTemplate = useDebounced(templateText, 600)

const kindsQuery = useQuery({
  queryKey: queryKeys.datasourceChartKinds(),
  queryFn: () => datasourceApi.chartKinds(),
  staleTime: Infinity,
})

const kinds = computed(() => kindsQuery.data.value ?? [])

const introspectQuery = useQuery({
  queryKey: computed(() =>
    queryKeys.datasourceIntrospect(
      props.registryId,
      settled.value,
      displayKindId.value,
      mapping.value,
      settledTemplate.value,
    ),
  ),
  queryFn: ({ signal }) =>
    datasourceApi.introspect(
      {
        visualisationRegistryId: props.registryId,
        command: settled.value,
        displayKindId: displayKindId.value,
        mapping: mapping.value,
        templateText: settledTemplate.value,
      },
      signal,
    ),
  enabled: computed(() => settled.value.trim().length > 0),
  placeholderData: keepPreviousData,
  retry: false,
})

const introspection = computed(() => introspectQuery.data.value)

const columns = computed(() => introspection.value?.columns ?? [])

const state = computed(() =>
  sqlState({
    command: command.value,
    settled: settled.value,
    fetching: introspectQuery.isFetching.value,
    failed: introspectQuery.isError.value,
    introspection: introspection.value,
  }),
)

const valid = computed(() =>
  isValid({
    state: state.value,
    introspection: introspection.value,
    displayKindId: displayKindId.value,
    includeGrid: includeGrid.value,
    templateText: templateText.value,
  }),
)

const problem = computed(() => {
  const found = introspection.value

  return state.value === 'errors' && found?.errorLine && found.error
    ? { line: found.errorLine, column: found.errorColumn ?? 1, message: found.error }
    : null
})

const kind = computed(() =>
  kinds.value.find((candidate) => candidate.kindId === displayKindId.value),
)

const roles = computed(() => columnRoles(kind.value, introspection.value?.resolvedMapping ?? []))

const previewQuery = useQuery({
  queryKey: computed(() =>
    queryKeys.datasourcePreview(
      props.registryId,
      settled.value,
      displayKindId.value,
      mapping.value,
      settledTemplate.value,
      includeGrid.value,
      previewParameters.value,
    ),
  ),
  queryFn: ({ signal }) =>
    datasourceApi.preview(
      {
        visualisationRegistryId: props.registryId,
        command: settled.value,
        displayKindId: displayKindId.value,
        mapping: mapping.value,
        templateText: settledTemplate.value,
        includeGrid: includeGrid.value,
        card: cardRequest(previewParameters.value, knownStylesheetVersion()),
      },
      signal,
    ),
  enabled: computed(
    () =>
      valid.value &&
      command.value === settled.value &&
      templateText.value === settledTemplate.value,
  ),
  placeholderData: keepPreviousData,
  retry: false,
})

const testMutation = useMutation({
  mutationFn: (parameters: CardParameter[]) =>
    datasourceApi.preview({
      visualisationRegistryId: props.registryId,
      command: command.value,
      displayKindId: 0,
      mapping: [],
      templateText: '',
      includeGrid: true,
      card: cardRequest(parameters, knownStylesheetVersion()),
    }),
})

const testFailed = computed(() => testMutation.isError.value)

function runSql(): void {
  const gaps = missingRequired(definitions.value, parameterValues.value)

  missingParameters.value = gaps

  if (gaps.length === 0)
    testMutation.mutate(cardParameters(definitions.value, parameterValues.value))
}

const value = computed<DatasourceValue>(() => ({
  command: command.value,
  displayKindId: displayKindId.value,
  includeGrid: includeGrid.value,
  mapping: mapping.value,
  templateText: templateText.value,
  valid: valid.value,
}))

watch(value, (next) => emit('change', next), { immediate: true })

watch(columns, (next) => {
  if (next.length === 0) return

  const kept = dropMissingColumns(
    mapping.value,
    next.map((column) => column.name),
  )

  if (!sameMapping(kept, mapping.value)) mapping.value = kept
})

function setMapping(next: MappingEntry[]): void {
  mapping.value = next
}

function setKind(next: number): void {
  displayKindId.value = next
  mapping.value = []
}

defineExpose({ getValue: () => value.value })
</script>

<template>
  <div class="flex flex-col gap-4" data-testid="datasource-builder">
    <PanelCard :title="t('datasource.sql.title')" :step="1">
      <DatasourceSqlEditor
        v-model="command"
        :label="t('datasource.sql.label')"
        :readonly="locked"
        :problem="problem"
      />
      <DatasourceSqlStatus
        :state="state"
        :error="introspection?.error ?? null"
        :line="introspection?.errorLine ?? null"
        :column="introspection?.errorColumn ?? null"
      />
      <div class="flex flex-wrap items-center gap-3">
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="state !== 'valid' || testMutation.isPending.value"
          data-testid="datasource-run-sql"
          @click="runSql"
        >
          <Play aria-hidden="true" class="size-4" />
          {{ t('datasource.test.run') }}
        </button>
        <p
          v-if="missingParameters.length > 0"
          key="missing"
          role="status"
          class="text-sm text-error"
          data-testid="datasource-test-missing"
        >
          {{ t('datasource.test.missing') }}
        </p>
      </div>
      <p
        v-if="testFailed"
        key="failed"
        role="alert"
        class="text-sm text-error"
        data-testid="datasource-test-failed"
      >
        {{ t('datasource.test.failed') }}
      </p>
      <DatasourceCardView
        v-if="testMutation.isPending.value || testMutation.data.value"
        key="test"
        :card="testMutation.data.value"
        :loading="testMutation.isPending.value"
      />
    </PanelCard>

    <PanelCard :title="t('datasource.columns.title')" :step="2">
      <DatasourceColumnStrip v-if="columns.length > 0" :columns="columns" :roles="roles" />
      <p v-else key="none" class="text-sm text-secondary" data-testid="datasource-columns-empty">
        {{ t('datasource.columns.empty') }}
      </p>
    </PanelCard>

    <PanelCard :title="t('datasource.show.title')" :step="3">
      <DatasourceDisplayPicker
        :kinds="kinds"
        :introspection="introspection"
        :display-kind-id="displayKindId"
        :include-grid="includeGrid"
        :mapping="mapping"
        :template-text="templateText"
        :columns="columns"
        :locked="locked"
        @update:display-kind-id="setKind"
        @update:include-grid="includeGrid = $event"
        @update:mapping="setMapping"
        @update:template-text="templateText = $event"
      />
      <p
        v-if="displayKindId === 0 && !includeGrid"
        key="needs"
        role="status"
        class="text-sm text-secondary"
        data-testid="datasource-needs-display"
      >
        {{ t('datasource.show.needsDisplay') }}
      </p>
    </PanelCard>

    <PanelCard :title="t('datasource.preview.title')" :step="4">
      <VisualisationParameters
        v-if="definitions.length > 0"
        :definitions="definitions"
        :values="parameterValues"
        :missing="missingParameters"
        @change="changeParameter"
        @run="runParameters"
      />
      <DatasourceCardView
        v-if="valid"
        key="card"
        :card="previewQuery.data.value"
        :loading="previewQuery.isFetching.value"
      />
      <p
        v-else
        key="waiting"
        class="text-sm text-secondary"
        data-testid="datasource-preview-waiting"
      >
        {{ t('datasource.preview.waiting') }}
      </p>
    </PanelCard>
  </div>
</template>
