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
import { computed, onBeforeUnmount, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { directionFor } from '../i18n'
import { cssColourHex } from '../composables/themeColour'
import { fromLocalInput, toLocalInput } from '../composables/localDateTime'
import { useFormat } from '../composables/useFormat'
import { useUiStore } from '../stores/ui'
import { impossibleKilometres, impossibleKmh } from '../watcher/geo'
import type { WatcherView } from '../watcher/pageState'
import { latestPerEntity, locatedOnly, ruleList, trailsOf, withinWindow } from '../watcher/state'
import type { Activation, MapMode } from '../watcher/types'
import { activationWatcherSource, type WatcherSource } from '../watcher/source'
import { useWatcher } from '../watcher/useWatcher'
import ActivationMap from './ActivationMap.vue'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridColumn } from './dataGridTypes'
import PanelCard from './PanelCard.vue'
import ServerChart from './ServerChart.vue'

const props = withDefaults(
  defineProps<{
    realtime?: boolean
    mode?: MapMode
    keyFilter?: string
    keyValueFilter?: string
    showControls?: boolean
    showChart?: boolean
    showFeed?: boolean
    maxRows?: number
    windowMinutes?: number
    fadeMinutes?: number
    history?: boolean
    source?: WatcherSource
    travelLimitKmh?: number
    minimumKilometres?: number
    focusSuffix?: string | null
    trails?: boolean
    heat?: boolean
    replayFrom?: string
    replayTo?: string
    tall?: boolean
    autoFit?: boolean
    compact?: boolean
  }>(),
  {
    realtime: true,
    mode: 'latest',
    showControls: true,
    showChart: true,
    showFeed: true,
    maxRows: 10,
    windowMinutes: 0,
    fadeMinutes: 15,
    history: true,
    travelLimitKmh: impossibleKmh,
    minimumKilometres: impossibleKilometres,
    trails: true,
    heat: false,
    replayFrom: '',
    replayTo: '',
    tall: false,
  },
)

const emit = defineEmits<{ select: [activation: Activation | null]; view: [view: WatcherView] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const ui = useUiStore()
const map = useTemplateRef<{ fit: () => void; flyTo: (id: string) => void }>('map')

const direction = computed(() => directionFor(locale.value))
const live = ref(props.realtime)
const modeLocal = ref<MapMode>(props.mode)
const windowLocal = ref(props.windowMinutes)
const fadeLocal = ref(props.fadeMinutes)
const showHeat = ref(props.heat)
const showTrails = ref(props.trails)
const selectedId = ref<string | null>(null)
const followed = ref<string | null>(null)
const mapFailed = ref(false)
const replayFrom = ref(props.replayFrom || toLocalInput(new Date(Date.now() - 60_000)))
const replayTo = ref(props.replayTo || toLocalInput(new Date()))
const replayed = ref({ from: props.replayFrom, to: props.replayTo })
const now = ref(Date.now())

watch(
  () => props.realtime,
  (value) => {
    live.value = value
  },
)

const options = computed(() => ({
  mode: modeLocal.value,
  maxRows: props.maxRows,
  keyFilter: props.keyFilter,
  keyValueFilter: props.keyValueFilter,
}))

watch(
  () => props.trails,
  (value) => {
    showTrails.value = value
  },
)

watch(
  () => props.heat,
  (value) => {
    showHeat.value = value
  },
)

const watcher = useWatcher(options, live, props.source ?? activationWatcherSource)

const clock = setInterval(() => {
  now.value = Date.now()
}, 1000)

const snapshot = ref(ruleList(watcher.state.value))
const summary = setInterval(() => {
  snapshot.value = ruleList(watcher.state.value)
}, 3000)

onBeforeUnmount(() => {
  clearInterval(clock)
  clearInterval(summary)
})

const windowMs = computed(() => (windowLocal.value > 0 ? windowLocal.value * 60_000 : null))
const fadeMs = computed(() => (fadeLocal.value > 0 ? fadeLocal.value * 60_000 : null))

const scope = computed(() => {
  const inWindow = withinWindow(watcher.state.value.history, windowMs.value, now.value)

  return followed.value
    ? inWindow.filter((activation) => activation.entity === followed.value)
    : inWindow
})

const points = computed(() => {
  const located = locatedOnly(scope.value)

  return followed.value || modeLocal.value === 'all' ? located : latestPerEntity(located)
})

const dimmed = computed(() => points.value.some((activation) => activation.dim === true))

const unlocated = computed(() => scope.value.length - locatedOnly(scope.value).length)

const trails = computed(() =>
  showTrails.value ? trailsOf(scope.value, props.travelLimitKmh, props.minimumKilometres) : [],
)

const impossible = computed(() => trails.value.filter((segment) => segment.impossible))

const selected = computed(
  () =>
    watcher.state.value.history.find((activation) => activation.id === selectedId.value) ?? null,
)

const accent = computed(() =>
  ui.resolvedTheme ? (cssColourHex('--color-primary') ?? '#005f7f') : '#005f7f',
)
const lineColour = computed(() => (ui.resolvedTheme ? cssColourHex('--color-secondary') : null))
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const chartBody = computed(() => ({
  dark: dark.value,
  columnColor: accent.value,
  lineColor: lineColour.value,
  rules: snapshot.value.map((rule) => ({
    name: rule.name,
    frequency: rule.frequency,
    aggregate: rule.aggregate,
  })),
}))

function choose(id: string | null): void {
  selectedId.value = id
  emit('select', id ? (watcher.state.value.history.find((item) => item.id === id) ?? null) : null)
}

watch(
  [() => props.focusSuffix, () => watcher.state.value.history],
  () => {
    const suffix = props.focusSuffix

    if (!suffix) return

    const found = watcher.state.value.history.find((item) => item.id.endsWith(`|${suffix}`))

    if (!found || selectedId.value === found.id) return

    choose(found.id)
    window.setTimeout(() => map.value?.flyTo(found.id), 400)
  },
  { immediate: true },
)

function locate(event: GridActionEvent): void {
  const row = event.row as Activation

  choose(row.id)
  map.value?.flyTo(row.id)
}

function follow(): void {
  followed.value = selected.value?.entity ?? null
  window.setTimeout(() => map.value?.fit(), 50)
}

function release(): void {
  followed.value = null
}

function replay(): void {
  replayed.value = { from: replayFrom.value, to: replayTo.value }
  void watcher.replay(
    fromLocalInput(replayFrom.value),
    fromLocalInput(replayTo.value),
    props.keyFilter,
    props.keyValueFilter,
  )
}

function replayJourney(): void {
  if (selected.value) {
    void watcher.replay(null, null, selected.value.key, selected.value.keyValue)
    followed.value = selected.value.entity
    window.setTimeout(() => map.value?.fit(), 600)
  }
}

if (props.replayFrom && props.replayTo) {
  void watcher.replay(
    fromLocalInput(props.replayFrom),
    fromLocalInput(props.replayTo),
    props.keyFilter,
    props.keyValueFilter,
  )
} else if (props.history && props.keyFilter && props.keyValueFilter) {
  void watcher.replay(null, null, props.keyFilter, props.keyValueFilter)
}

watch([live, modeLocal, windowLocal, fadeLocal, showTrails, showHeat, replayed], () => {
  emit('view', {
    live: live.value,
    mode: modeLocal.value,
    window: windowLocal.value,
    fade: fadeLocal.value,
    trails: showTrails.value,
    heat: showHeat.value,
    from: replayed.value.from,
    to: replayed.value.to,
  })
})

const statusKey = computed(() => {
  switch (watcher.status.value) {
    case 'open':
      return 'watcher.status.open'
    case 'connecting':
      return 'watcher.status.connecting'
    case 'closed':
      return 'watcher.status.closed'
    default:
      return 'watcher.status.paused'
  }
})

function swatch(row: object): Element {
  const activation = row as Activation
  const dot = document.createElement('span')

  dot.className = 'inline-block size-3 rounded-full border border-base-content/30'
  dot.style.backgroundColor = activation.backColor ?? accent.value
  dot.setAttribute('role', 'img')
  dot.setAttribute('aria-label', activation.responseElevationContent || t('watcher.col.colour'))

  return dot
}

const columns = computed<GridColumn[]>(() => [
  { field: 'backColor', title: '', hiddenTitle: t('watcher.col.colour'), width: 44, draw: swatch },
  {
    field: 'createdDate',
    title: t('watcher.col.created'),
    mono: true,
    width: 170,
    format: (value) => (value ? dateTime(new Date(value as string)) : ''),
  },
  { field: 'entity', title: t('watcher.col.entity'), minWidth: 140 },
  { field: 'ruleSummary', title: t('watcher.col.rule'), minWidth: 160 },
  {
    field: 'responseElevation',
    title: t('watcher.col.elevation'),
    align: 'end',
    mono: true,
    width: 100,
  },
  { field: 'responseElevationName', title: t('watcher.col.range'), minWidth: 120 },
  { field: 'responseElevationContent', title: t('watcher.col.content'), minWidth: 140 },
  {
    field: 'locate',
    title: '',
    hiddenTitle: t('watcher.col.actions'),
    width: 90,
    actions: () => [{ id: 'locate', label: t('watcher.locate') }],
  },
])

const plotted = computed(() => points.value.length)
const fades = computed(() => [
  { value: 0, label: t('watcher.fade.off') },
  { value: 1, label: t('watcher.fade.minute') },
  { value: 5, label: t('watcher.fade.five') },
  { value: 15, label: t('watcher.fade.fifteen') },
  { value: 60, label: t('watcher.fade.hour') },
])
const modes = computed(() => [
  { value: 'latest' as MapMode, label: t('watcher.mode.latest') },
  { value: 'all' as MapMode, label: t('watcher.mode.all') },
])
const windows = computed(() => [
  { value: 0, label: t('watcher.window.all') },
  { value: 1, label: t('watcher.window.minute') },
  { value: 5, label: t('watcher.window.five') },
  { value: 15, label: t('watcher.window.fifteen') },
  { value: 60, label: t('watcher.window.hour') },
])
defineExpose({ fit: () => map.value?.fit() })
</script>

<template>
  <div class="@container flex min-w-0 flex-col gap-3" data-testid="activation-watcher">
    <div
      v-if="showControls && !compact"
      key="controls"
      class="flex flex-col gap-3 rounded-box border border-base-300 bg-base-100 p-3"
      role="group"
      :aria-label="t('watcher.controls')"
    >
      <div class="flex flex-wrap items-end gap-x-6 gap-y-3">
        <div class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.live') }}</span>
          <div class="flex h-8 items-center gap-3">
            <label class="flex items-center gap-2 text-sm">
              <input
                v-model="live"
                type="checkbox"
                class="toggle toggle-sm toggle-primary"
                data-testid="watcher-live"
              />
              <span>{{ t('watcher.realtime') }}</span>
            </label>
            <span
              class="flex items-center gap-2 text-sm whitespace-nowrap"
              role="status"
              data-testid="watcher-status"
            >
              <span class="status status-sm" aria-hidden="true"></span>
              {{ t(statusKey) }}
            </span>
          </div>
        </div>

        <div class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.mode.label') }}</span>
          <div class="join" role="group" :aria-label="t('watcher.mode.label')">
            <input
              v-for="entry in modes"
              :key="entry.value"
              v-model="modeLocal"
              type="radio"
              name="watcher-mode"
              class="btn btn-sm join-item"
              :value="entry.value"
              :aria-label="entry.label"
              :data-testid="`watcher-mode-${entry.value}`"
            />
          </div>
        </div>

        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.window.label') }}</span>
          <select
            v-model.number="windowLocal"
            class="select select-sm"
            data-testid="watcher-window"
          >
            <option v-for="entry in windows" :key="entry.value" :value="entry.value">
              {{ entry.label }}
            </option>
          </select>
        </label>

        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.fade.label') }}</span>
          <select v-model.number="fadeLocal" class="select select-sm" data-testid="watcher-fade">
            <option v-for="entry in fades" :key="entry.value" :value="entry.value">
              {{ entry.label }}
            </option>
          </select>
        </label>

        <div class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.layers') }}</span>
          <div class="flex h-8 items-center gap-4">
            <label class="flex items-center gap-2 text-sm">
              <input
                v-model="showTrails"
                type="checkbox"
                class="toggle toggle-sm"
                data-testid="watcher-trails"
              />
              <span>{{ t('watcher.trails') }}</span>
            </label>
            <label class="flex items-center gap-2 text-sm">
              <input
                v-model="showHeat"
                type="checkbox"
                class="toggle toggle-sm"
                data-testid="watcher-heat"
              />
              <span>{{ t('watcher.heat') }}</span>
            </label>
          </div>
        </div>

        <button
          type="button"
          class="btn btn-sm ms-auto"
          data-testid="watcher-fit"
          @click="map?.fit()"
        >
          {{ t('watcher.fit') }}
        </button>
      </div>

      <form
        class="flex flex-wrap items-end gap-x-6 gap-y-3 border-t border-base-300 pt-3"
        :aria-label="t('watcher.replayLabel')"
        @submit.prevent="replay"
      >
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.replayFrom') }}</span>
          <input
            v-model="replayFrom"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="watcher-from"
          />
        </label>
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.replayTo') }}</span>
          <input
            v-model="replayTo"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="watcher-to"
          />
        </label>
        <div class="flex gap-2">
          <button
            type="submit"
            class="btn btn-sm"
            :disabled="watcher.replaying.value"
            data-testid="watcher-replay"
          >
            {{ t('watcher.replay') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-ghost"
            data-testid="watcher-clear"
            @click="watcher.clear()"
          >
            {{ t('watcher.clear') }}
          </button>
        </div>
      </form>
    </div>

    <p v-if="watcher.replayFailed.value" key="replayFailed" role="alert" class="text-sm text-error">
      {{ t('watcher.replayFailed') }}
    </p>

    <div
      class="grid min-w-0 grid-cols-[minmax(0,1fr)] gap-3"
      :class="showFeed ? '@5xl:grid-cols-[minmax(0,1fr)_26rem]' : ''"
    >
      <div class="flex min-w-0 flex-col gap-2">
        <div
          class="relative min-w-0"
          :class="tall ? 'h-[max(24rem,calc(100dvh-26rem))]' : 'h-[32rem] @5xl:h-[40rem]'"
        >
          <p
            v-if="mapFailed"
            key="mapFailed"
            role="alert"
            class="flex size-full items-center justify-center rounded-box border border-base-300 bg-base-200 p-6 text-center text-sm"
            data-testid="watcher-map-failed"
          >
            {{ t('watcher.mapFailed') }}
          </p>
          <ActivationMap
            v-else
            key="map"
            ref="map"
            :points="points"
            :trails="trails"
            :show-trails="showTrails"
            :show-heat="showHeat"
            :dark="dark"
            :accent="accent"
            :selected-id="selectedId"
            :fade-ms="fadeMs"
            :now="now"
            :auto-fit="autoFit"
            :label="t('watcher.mapLabel')"
            @select="choose"
            @failed="mapFailed = true"
          />
          <p
            class="pointer-events-none absolute start-2 top-2 flex flex-wrap items-center gap-x-3 gap-y-1 rounded-field bg-base-100/90 px-2 py-1 text-xs"
            data-testid="watcher-legend"
          >
            <span class="flex items-center gap-1">
              <span class="status status-primary" aria-hidden="true"></span
              >{{ t('watcher.legend.activation') }}
            </span>
            <span class="flex items-center gap-1">
              <span class="status status-error" aria-hidden="true"></span
              >{{ t('watcher.legend.impossible', { speed: travelLimitKmh }) }}
            </span>
            <span v-if="dimmed" key="dimmed" class="flex items-center gap-1">
              <span class="status opacity-40" aria-hidden="true"></span
              >{{ t('watcher.legend.dim') }}
            </span>
          </p>
        </div>

        <div
          v-if="!compact"
          class="flex flex-wrap items-center gap-3 text-sm"
          data-testid="watcher-counts"
        >
          <span class="font-mono tabular-nums">{{
            t('watcher.counts', {
              plotted,
              unlocated,
              pending: watcher.pendingCount.value,
              received: watcher.received.value,
            })
          }}</span>
          <span
            v-if="impossible.length > 0"
            key="impossible"
            class="badge badge-error badge-soft"
            data-testid="watcher-impossible"
            >{{ t('watcher.impossibleCount', impossible.length) }}</span
          >
          <span
            v-if="followed"
            key="followed"
            class="badge badge-sm"
            data-testid="watcher-followed"
          >
            {{ t('watcher.following', { entity: followed }) }}
            <button
              type="button"
              class="btn btn-ghost btn-xs"
              data-testid="watcher-release"
              @click="release"
            >
              {{ t('watcher.release') }}
            </button>
          </span>
        </div>

        <section
          v-if="selected"
          key="selected"
          class="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-box border border-base-300 bg-base-100 p-2 text-sm"
          :aria-label="t('watcher.selected')"
          data-testid="watcher-selected"
        >
          <span class="font-semibold">{{ selected.entity }}</span>
          <span>{{ selected.ruleSummary || '-' }}</span>
          <span class="font-mono tabular-nums"
            >{{ t('watcher.col.elevation') }} {{ selected.responseElevation }}</span
          >
          <span v-if="selected.responseElevationName" data-testid="watcher-selected-range">{{
            selected.responseElevationName
          }}</span>
          <span class="font-mono tabular-nums">{{ dateTime(new Date(selected.createdAt)) }}</span>
          <div class="ms-auto flex gap-2">
            <button type="button" class="btn btn-sm" data-testid="watcher-follow" @click="follow">
              {{ t('watcher.follow') }}
            </button>
            <button
              type="button"
              class="btn btn-sm"
              data-testid="watcher-journey"
              @click="replayJourney"
            >
              {{ t('watcher.journey') }}
            </button>
          </div>
        </section>
      </div>

      <PanelCard
        v-if="showFeed"
        key="feed"
        :title="t('watcher.feed')"
        class="h-[24rem] @5xl:h-auto"
        compact
        fill
      >
        <DataGrid
          :columns="columns"
          :rows="watcher.state.value.rows"
          :direction="direction"
          :empty-text="t('watcher.empty')"
          :label="t('watcher.feed')"
          row-key="id"
          fill
          @action="locate"
        />
      </PanelCard>
    </div>

    <PanelCard v-if="showChart" key="chart" :title="t('watcher.summary')" compact>
      <p v-if="snapshot.length === 0" key="none" class="text-sm text-secondary">
        {{ t('watcher.summaryEmpty') }}
      </p>
      <ServerChart
        v-else
        key="chart"
        url="/api/Chart/Watcher/ActivationSummary"
        :body="chartBody"
        :label="t('watcher.summary')"
        :stale-seconds="3600"
      />
    </PanelCard>
  </div>
</template>
