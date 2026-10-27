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
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { overrideApi } from '../overrides/api'
import { expiryCell, switchCell } from '../overrides/cells'
import { kindKey } from '../overrides/state'
import { useOverrideActions } from '../overrides/useOverrideActions'
import type { OverrideKey, OverrideModel, OverrideScope, OverrideValue } from '../overrides/types'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridColumn, GridExpandEvent } from './dataGridTypes'
import OverrideRuleGrid from './OverrideRuleGrid.vue'
import PageTrail from './PageTrail.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(
  defineProps<{
    overrideKey?: string
    overrideKeyValue?: string
    showSelector?: boolean
    showTrail?: boolean
    canForce?: boolean
    expandedModel?: string
  }>(),
  { showSelector: true, showTrail: true, canForce: true, expandedModel: '' },
)

const emit = defineEmits<{
  change: [scope: { overrideKey: string; overrideKeyValue: string }]
  updated: []
  expand: [entityAnalysisModelGuid: string]
}>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const pagerLabels = usePagerLabels()

const direction = computed(() => directionFor(locale.value))

const key = ref(props.overrideKey ?? '')
const value = ref(props.overrideKeyValue ?? '')
const draftKey = ref(key.value)
const draftValue = ref(value.value)

watch(
  () => [props.overrideKey, props.overrideKeyValue],
  () => {
    key.value = props.overrideKey ?? ''
    value.value = props.overrideKeyValue ?? ''
    draftKey.value = key.value
    draftValue.value = value.value
  },
)

function go(nextKey: string, nextValue: string): void {
  key.value = nextKey
  value.value = nextValue
  draftKey.value = nextKey
  draftValue.value = nextValue
  emit('change', { overrideKey: nextKey, overrideKeyValue: nextValue })
}

const crumbs = computed(() => [
  { id: 'keys', label: t('override.keys'), testid: 'override-crumb-keys' },
  ...(key.value ? [{ id: 'key', label: key.value, testid: 'override-crumb-key' }] : []),
  ...(value.value ? [{ id: 'value', label: value.value, testid: 'override-crumb-value' }] : []),
])

function onCrumb(id: string | number): void {
  go(id === 'keys' ? '' : key.value, '')
}

function fetchSelection(): void {
  go(draftKey.value, draftKey.value ? draftValue.value.trim() : '')
}

const view = computed<'keys' | 'values' | 'models'>(() => {
  if (!key.value) return 'keys'

  return value.value ? 'models' : 'values'
})

const scope = computed<OverrideScope>(() => ({
  overrideKey: key.value,
  overrideKeyValue: value.value,
}))

const actions = useOverrideActions(
  () => scope.value,
  () => emit('updated'),
)

const keyNames = useQuery({
  queryKey: queryKeys.overrideKeyNames(),
  queryFn: overrideApi.keyNames,
  enabled: computed(() => props.showSelector),
  retry: false,
})

const keyRows = useQuery({
  queryKey: queryKeys.overrideKeys(),
  queryFn: overrideApi.keys,
  enabled: computed(() => view.value === 'keys'),
  retry: false,
})

const valueRows = useQuery({
  queryKey: computed(() => queryKeys.overrideValues(key.value)),
  queryFn: () => overrideApi.values(key.value),
  enabled: computed(() => view.value === 'values'),
  retry: false,
})

const modelRows = useQuery({
  queryKey: computed(() => queryKeys.overrideModels(scope.value)),
  queryFn: () => overrideApi.models(scope.value),
  enabled: computed(() => view.value === 'models'),
  retry: false,
})

const choices = computed(() => {
  const names = (keyNames.data.value ?? []).map((entry) => entry.name)

  return names.includes(draftKey.value) || !draftKey.value ? names : [draftKey.value, ...names]
})

const openAction = computed(() => [{ id: 'open', label: t('override.open') }])
const when = (cell: unknown) => (cell ? dateTime(new Date(cell as string)) : '')
const kindText = (cell: unknown) =>
  kindKey(cell as number) === 'force' ? t('override.kind.force') : t('override.kind.suppress')
const count = (cell: unknown) => String(cell ?? 0)

const keyColumns = computed<GridColumn[]>(() => [
  { field: 'overrideKey', title: t('override.col.key'), minWidth: 160 },
  {
    field: 'values',
    title: t('override.col.values'),
    align: 'end',
    mono: true,
    width: 100,
    format: count,
  },
  {
    field: 'overrides',
    title: t('override.col.overrides'),
    align: 'end',
    mono: true,
    width: 110,
    format: count,
  },
  {
    field: 'forced',
    title: t('override.col.forced'),
    align: 'end',
    mono: true,
    width: 100,
    format: count,
  },
  {
    field: 'activationRules',
    title: t('override.col.rules'),
    align: 'end',
    mono: true,
    width: 130,
    format: count,
  },
  {
    field: 'allActivationRules',
    title: t('override.col.allRules'),
    align: 'end',
    mono: true,
    width: 110,
    format: count,
  },
  {
    field: 'enabledOnModels',
    title: t('override.col.models'),
    align: 'end',
    mono: true,
    width: 100,
    format: count,
  },
  {
    field: 'lastCreatedDate',
    title: t('override.col.lastCreated'),
    mono: true,
    width: 180,
    format: when,
  },
  {
    field: 'nextExpiryDate',
    title: t('override.col.nextExpiry'),
    mono: true,
    width: 180,
    format: when,
  },
  {
    field: 'open',
    title: '',
    hiddenTitle: t('override.col.actions'),
    width: 90,
    actions: () => openAction.value,
  },
])

const valueColumns = computed<GridColumn[]>(() => [
  { field: 'overrideKeyValue', title: t('override.col.value'), minWidth: 160 },
  { field: 'name', title: t('override.col.model'), minWidth: 160 },
  { field: 'overrideKind', title: t('override.col.kind'), width: 120, format: kindText },
  { field: 'createdUser', title: t('override.col.createdUser'), width: 160 },
  {
    field: 'createdDate',
    title: t('override.col.createdDate'),
    mono: true,
    width: 180,
    format: when,
  },
  {
    field: 'deleteExpiryDate',
    title: t('override.col.expiry'),
    mono: true,
    width: 180,
    format: when,
  },
  {
    field: 'open',
    title: '',
    hiddenTitle: t('override.col.actions'),
    width: 90,
    actions: () => openAction.value,
  },
])

const modelColumns = computed<GridColumn[]>(() => [
  {
    field: 'hasOverride',
    title: t('override.col.override'),
    width: 110,
    draw: (row) =>
      switchCell((row as OverrideModel).hasOverride, t('override.col.override'), (active) =>
        actions.setModel((row as OverrideModel).entityAnalysisModelGuid, active),
      ),
  },
  {
    field: 'overrideKind',
    title: t('override.col.kind'),
    width: 130,
    format: kindText,
  },
  {
    field: 'deleteExpiryDate',
    title: t('override.col.expiry'),
    width: 230,
    draw: (row) =>
      expiryCell(
        (row as OverrideModel).deleteExpiryDate,
        !(row as OverrideModel).hasOverride,
        t('override.col.expiry'),
        actions.reportTooEarly,
        (expiry) => actions.setModelExpiry((row as OverrideModel).entityAnalysisModelGuid, expiry),
      ),
  },
  { field: 'name', title: t('override.col.model'), minWidth: 200 },
])

const models = computed(() =>
  (modelRows.data.value ?? []).map((model) => ({ ...model, id: model.entityAnalysisModelGuid })),
)

const hosts = ref(new Map<unknown, HTMLElement>())
const lastOpened = ref(props.expandedModel)

function onExpand(event: GridExpandEvent): void {
  const next = new Map(hosts.value)

  if (event.host) {
    next.set(event.id, event.host)
    lastOpened.value = String(event.id)
    emit('expand', lastOpened.value)
  } else {
    next.delete(event.id)

    if (lastOpened.value === String(event.id)) {
      lastOpened.value = ''
      emit('expand', '')
    }
  }

  hosts.value = next
}

function onKeyAction(event: GridActionEvent): void {
  go((event.row as OverrideKey).overrideKey, '')
}

function onValueAction(event: GridActionEvent): void {
  go(key.value, (event.row as OverrideValue).overrideKeyValue)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="override-manager">
    <form
      v-if="showSelector"
      key="selector"
      class="flex flex-wrap items-end gap-3"
      @submit.prevent="fetchSelection"
    >
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('override.key') }}</span>
        <select v-model="draftKey" class="select select-sm min-w-56" data-testid="override-key">
          <option value="">{{ t('override.allKeys') }}</option>
          <option v-for="name in choices" :key="name" :value="name">{{ name }}</option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('override.keyValue') }}</span>
        <input
          v-model="draftValue"
          type="text"
          class="input input-sm min-w-56"
          :disabled="!draftKey"
          data-testid="override-value"
        />
      </label>
      <button type="submit" class="btn btn-sm" data-testid="override-fetch">
        {{ t('override.fetch') }}
      </button>
    </form>

    <PageTrail
      v-if="showTrail"
      key="trail"
      :crumbs="crumbs"
      :label="t('override.trail')"
      data-testid="override-trail"
      @select="onCrumb"
    />

    <ProblemAlert
      v-if="actions.problems.value && actions.problems.value.length > 0"
      key="problems"
      :reasons="actions.problems.value"
      data-testid="override-problems"
    />

    <PanelCard v-if="view === 'keys'" key="keysView" :title="t('override.keys')" compact>
      <p v-if="keyRows.isError.value" role="alert" class="text-sm text-error">
        {{ t('override.failed') }}
      </p>
      <DataGrid
        v-else
        :columns="keyColumns"
        :rows="(keyRows.data.value ?? []).map((row) => ({ ...row, id: row.overrideKey }))"
        :direction="direction"
        :empty-text="t('override.noKeys')"
        :label="t('override.keys')"
        :page-size="20"
        :pager-labels="pagerLabels"
        auto-height
        @action="onKeyAction"
      />
    </PanelCard>

    <PanelCard v-else-if="view === 'values'" key="valuesView" :title="key" compact>
      <p v-if="valueRows.isError.value" role="alert" class="text-sm text-error">
        {{ t('override.failed') }}
      </p>
      <DataGrid
        v-else
        :columns="valueColumns"
        :rows="
          (valueRows.data.value ?? []).map((row) => ({
            ...row,
            id: `${row.overrideKeyValue}|${row.entityAnalysisModelGuid}`,
          }))
        "
        :direction="direction"
        :empty-text="t('override.noValues')"
        :label="t('override.values')"
        :page-size="20"
        :pager-labels="pagerLabels"
        auto-height
        @action="onValueAction"
      />
    </PanelCard>

    <PanelCard v-else key="modelsView" :title="t('override.models')" compact>
      <p v-if="modelRows.isError.value" role="alert" class="text-sm text-error">
        {{ t('override.failed') }}
      </p>
      <DataGrid
        v-else
        :columns="modelColumns"
        :rows="models"
        :direction="direction"
        :empty-text="t('override.noModels')"
        :label="t('override.models')"
        expandable
        :expanded-keys="expandedModel ? [expandedModel] : []"
        auto-height
        :expand-labels="{ expand: t('override.showRules'), collapse: t('override.hideRules') }"
        @expand="onExpand"
      />
    </PanelCard>

    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <OverrideRuleGrid
          :scope="scope"
          :entity-analysis-model-guid="String(id)"
          :actions="actions"
          :can-force="canForce"
        />
      </Teleport>
    </template>
  </div>
</template>
