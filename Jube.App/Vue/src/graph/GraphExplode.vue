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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import DataGrid from '../components/DataGrid.vue'
import type {GridActionEvent, GridColumn} from '../components/dataGridTypes'
import {useGridExpandLabels} from '../composables/useGridExpandLabels'
import {useFormat} from '../composables/useFormat'
import {usePagerLabels} from '../composables/usePagerLabels'
import {directionFor} from '../i18n'
import GraphExplodeLane from './GraphExplodeLane.vue'
import {byInterest, nearestTagOf} from './state'
import type {GraphExplode, GraphExplodeLane as Lane, GraphWindow} from './types'

const props = withDefaults(
  defineProps<{
    data?: GraphExplode
    pending?: boolean
    failed?: boolean
    selected: string | null
    window: GraphWindow | null
    workflow?: string
    byInterest?: boolean
  }>(),
  {pending: false, failed: false, workflow: '', byInterest: false},
)

const emit = defineEmits<{
  select: [nodeId: string]
  pivot: [target: { key: string; value: string }]
}>()

const {t, locale} = useI18n()
const {number, fixed, dateTime} = useFormat()
const pagerLabels = usePagerLabels()
const expandLabels = useGridExpandLabels()

const direction = computed(() => directionFor(locale.value))

interface Row {
  id: string
  counterpart: string
  key: string
  edges: string
  count: number
  total: number
  first: string
  last: string
  hub: boolean
  interest: number | null
  nearestTag: string
  lane: Lane
}

const rows = computed<Row[]>(() =>
  (props.byInterest ? byInterest(props.data?.lanes ?? []) : (props.data?.lanes ?? [])).map(
    (lane) => ({
      id: String(lane.node.id),
      counterpart: lane.node.value ?? '',
      key: lane.node.key ?? '',
      edges: lane.definitions.join(', '),
      count: lane.count,
      total: lane.total,
      first: lane.first,
      last: lane.last,
      hub: lane.node.hub,
      interest: lane.node.interest,
      nearestTag: nearestTagText(lane.node),
      lane,
    }),
  ),
)

function nearestTagText(node: Lane['node']): string {
  const nearest = nearestTagOf(node)

  if (nearest === null) return ''

  return nearest.distance === 0
    ? t('graph.panel.tagHere', {tag: nearest.tag})
    : t('graph.panel.tagHops', {tag: nearest.tag, count: nearest.distance}, nearest.distance)
}

const hasInterest = computed(() => rows.value.some((row) => row.interest !== null))

const hasTags = computed(() => rows.value.some((row) => row.nearestTag !== ''))

function laneOf(id: unknown): Lane | undefined {
  return rows.value.find((row) => row.id === id)?.lane
}

const columns = computed<GridColumn[]>(() => [
  {field: 'counterpart', title: t('graph.explode.col.counterpart'), mono: true, minWidth: 180},
  {
    field: 'key',
    title: t('graph.explode.col.kind'),
    width: 130,
    badge: (value) => ({label: String(value ?? ''), tone: 'neutral'}),
  },
  {field: 'edges', title: t('graph.explode.col.edges'), minWidth: 160},
  {
    field: 'count',
    title: t('graph.explode.col.count'),
    type: 'number',
    mono: true,
    align: 'end',
    width: 110,
    format: (value) => (typeof value === 'number' ? number(value) : ''),
  },
  {
    field: 'total',
    title: t('graph.explode.col.total'),
    type: 'number',
    mono: true,
    align: 'end',
    width: 140,
    format: (value) => (typeof value === 'number' ? fixed(value) : ''),
  },
  {
    field: 'first',
    title: t('graph.explode.col.first'),
    type: 'date',
    mono: true,
    width: 170,
    format: (value) => (typeof value === 'string' && value !== '' ? dateTime(value) : ''),
  },
  {
    field: 'last',
    title: t('graph.explode.col.last'),
    type: 'date',
    mono: true,
    width: 170,
    format: (value) => (typeof value === 'string' && value !== '' ? dateTime(value) : ''),
  },
  ...(hasInterest.value
    ? [
      {
        field: 'interest',
        title: t('graph.explode.col.interest'),
        type: 'number' as const,
        mono: true,
        align: 'end' as const,
        width: 110,
        format: (value: unknown) => (typeof value === 'number' ? fixed(value) : ''),
      },
    ]
    : []),
  ...(hasTags.value
    ? [{field: 'nearestTag', title: t('graph.explode.col.nearestTag'), minWidth: 150}]
    : []),
  {
    field: 'hub',
    title: t('graph.explode.col.hub'),
    width: 90,
    badge: (value) =>
      value === true
        ? {label: t('graph.explode.hubYes'), tone: 'warning'}
        : {label: t('graph.explode.hubNo'), tone: 'neutral'},
  },
  {
    field: '',
    title: t('crud.actions'),
    width: 150,
    pinned: true,
    actions: () => [
      {id: 'select', label: t('graph.explode.select')},
      {id: 'pivot', label: t('graph.explode.pivot')},
    ],
  },
])

function onAction(event: GridActionEvent): void {
  const row = event.row as Row

  if (event.id === 'select') emit('select', row.id)
  else if (event.id === 'pivot') emit('pivot', {key: row.key, value: row.counterpart})
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="graph-explode">
    <h3 class="text-sm font-semibold">
      {{ t('graph.explode.title') }}
      <span
        v-if="data?.node"
        key="around"
        dir="ltr"
        class="badge badge-ghost badge-sm font-mono"
        data-testid="graph-explode-node"
      >{{ data.node.value }}</span
      >
    </h3>

    <div
      v-if="pending && !data"
      key="loading"
      class="skeleton h-40 w-full"
      role="status"
      :aria-label="t('graph.explode.loading')"
    ></div>
    <p v-else-if="failed && !data" key="failed" role="alert" class="text-sm">
      {{ t('graph.explode.failed') }}
    </p>
    <p v-else-if="rows.length === 0" key="empty" class="text-sm text-secondary">
      {{ t('graph.explode.empty') }}
    </p>
    <div v-else key="grid">
      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :page-size="25"
        :pager-labels="pagerLabels"
        :empty-text="t('graph.explode.empty')"
        :label="t('graph.explode.label')"
        row-key="id"
        :expand-labels="expandLabels"
        :mark="{ field: 'id', values: selected === null ? [] : [selected] }"
        @action="onAction"
      >
        <template #summary="{ id: id }">
          <GraphExplodeLane
            v-if="laneOf(id)"
            :lane="laneOf(id)!"
            :window="window"
            :workflow="workflow"
            :seed-key="data?.node?.key ?? ''"
            :seed-value="data?.node?.value ?? ''"
          />
        </template>
      </DataGrid>
    </div>
  </section>
</template>
