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
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {
  Banknote,
  Camera,
  GitBranch,
  LayoutList,
  Lock,
  Orbit,
  Sparkles,
  Waypoints,
  Workflow,
} from 'lucide-vue-next'
import type {Component} from 'vue'
import type {FlowDirection, GraphHops, GraphView} from './address'
import {defaultFlowTolerance, defaultFlowWindowHours} from './state'
import type {GraphEdgeDefinition, GraphLayoutId} from './types'

const props = withDefaults(
  defineProps<{
    view: GraphView
    hops: GraphHops | null
    layout: GraphLayoutId
    edges: string[]
    definitions: GraphEdgeDefinition[]
    hiddenHubs: number
    includeHubs: boolean
    minimumAmount: number
    nodes: number
    cap: number
    explode: boolean
    canCapture?: boolean
    capturing?: boolean
    disabled?: boolean
    flowAvailable?: boolean
    flowDirection?: FlowDirection | null
    flowWindowHours?: number | null
    flowTolerance?: number | null
  }>(),
  {
    canCapture: false,
    capturing: false,
    disabled: false,
    flowAvailable: false,
    flowDirection: null,
    flowWindowHours: null,
    flowTolerance: null,
  },
)

const emit = defineEmits<{
  'update:view': [view: GraphView]
  'update:hops': [hops: GraphHops]
  'update:layout': [layout: GraphLayoutId]
  toggleEdge: [guid: string]
  'update:includeHubs': [value: boolean]
  'update:minimumAmount': [value: number]
  'update:explode': [value: boolean]
  'update:flowDirection': [direction: FlowDirection]
  'update:flowWindowHours': [hours: number | null]
  'update:flowTolerance': [tolerance: number | null]
  capture: []
}>()

const {t} = useI18n()

const hopChoices: GraphHops[] = [1, 2, 3]

const layouts = computed<{ id: GraphLayoutId; label: string; icon: Component }[]>(() => [
  {id: 1, label: t('graph.layout.force'), icon: Waypoints},
  {id: 2, label: t('graph.layout.radial'), icon: Orbit},
  {id: 3, label: t('graph.layout.flow'), icon: GitBranch},
  {id: 4, label: t('graph.layout.hierarchy'), icon: Workflow},
])

const restrictedCount = computed(() => props.definitions.filter((d) => d.restricted).length)

const amountText = ref(String(props.minimumAmount))

watch(
  () => props.minimumAmount,
  (value) => {
    if (Number(amountText.value) !== value) amountText.value = String(value)
  },
)

function onAmount(): void {
  const value = Number(amountText.value)

  emit('update:minimumAmount', Number.isFinite(value) && value >= 0 ? value : 0)
}

function onHours(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)

  emit('update:flowWindowHours', Number.isFinite(value) && value > 0 ? value : null)
}

function onTolerance(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)

  emit('update:flowTolerance', Number.isFinite(value) && value >= 0 ? value : null)
}

function checked(guid: string): boolean {
  return props.edges.length === 0 || props.edges.includes(guid)
}

const flowDisabled = computed(
  () => props.disabled || (!props.flowAvailable && props.view !== 'flow'),
)
</script>

<template>
  <div
    class="flex flex-wrap items-end gap-x-5 gap-y-3 rounded-box border border-base-300 bg-base-100 px-3 py-2 text-sm"
    role="toolbar"
    :aria-label="t('graph.toolbar.label')"
    data-testid="graph-toolbar"
  >
    <fieldset class="fieldset p-0">
      <legend class="fieldset-legend p-0 pb-1">{{ t('graph.toolbar.view') }}</legend>
      <div class="join" role="group" :aria-label="t('graph.toolbar.view')">
        <button
          type="button"
          class="btn btn-sm join-item"
          :class="{ 'btn-active': view === 'relevance' }"
          :aria-pressed="view === 'relevance'"
          :disabled="disabled"
          data-testid="graph-view-relevance"
          @click="emit('update:view', 'relevance')"
        >
          <Sparkles class="size-4" aria-hidden="true"/>
          {{ t('graph.toolbar.relevance') }}
        </button>
        <button
          v-for="choice in hopChoices"
          :key="choice"
          type="button"
          class="btn btn-sm join-item"
          :class="{ 'btn-active': view === 'hops' && hops === choice }"
          :aria-pressed="view === 'hops' && hops === choice"
          :disabled="disabled"
          :data-testid="`graph-hops-${choice}`"
          @click="emit('update:hops', choice)"
        >
          {{ t('graph.toolbar.hops', {count: choice}, choice) }}
        </button>
        <div
          class="tooltip tooltip-bottom"
          :data-tip="
            flowAvailable || view === 'flow' ? undefined : t('graph.toolbar.flowNeedsNode')
          "
        >
          <button
            type="button"
            class="btn btn-sm join-item"
            :class="{ 'btn-active': view === 'flow' }"
            :aria-pressed="view === 'flow'"
            :disabled="flowDisabled"
            data-testid="graph-view-flow"
            @click="emit('update:view', 'flow')"
          >
            <Banknote class="size-4" aria-hidden="true"/>
            {{ t('graph.toolbar.flow') }}
          </button>
        </div>
      </div>
    </fieldset>

    <fieldset v-if="view === 'flow'" key="flow" class="fieldset p-0">
      <legend class="fieldset-legend p-0 pb-1">{{ t('graph.toolbar.direction') }}</legend>
      <div class="flex flex-wrap items-center gap-2">
        <div class="join" role="group" :aria-label="t('graph.toolbar.direction')">
          <button
            type="button"
            class="btn btn-sm join-item"
            :class="{ 'btn-active': flowDirection !== 'backward' }"
            :aria-pressed="flowDirection !== 'backward'"
            :disabled="disabled"
            data-testid="graph-flow-forward"
            @click="emit('update:flowDirection', 'forward')"
          >
            {{ t('graph.toolbar.forward') }}
          </button>
          <button
            type="button"
            class="btn btn-sm join-item"
            :class="{ 'btn-active': flowDirection === 'backward' }"
            :aria-pressed="flowDirection === 'backward'"
            :disabled="disabled"
            data-testid="graph-flow-backward"
            @click="emit('update:flowDirection', 'backward')"
          >
            {{ t('graph.toolbar.backward') }}
          </button>
        </div>
        <label class="input input-sm w-40">
          <span class="label">{{ t('graph.toolbar.hopWindow') }}</span>
          <input
            type="number"
            min="1"
            max="720"
            step="1"
            class="tabular-nums"
            :value="flowWindowHours ?? defaultFlowWindowHours"
            :disabled="disabled"
            data-testid="graph-flow-hours"
            @change="onHours"
          />
        </label>
        <label class="input input-sm w-40">
          <span class="label">{{ t('graph.toolbar.tolerance') }}</span>
          <input
            type="number"
            min="0"
            max="0.9"
            step="0.05"
            class="tabular-nums"
            :value="flowTolerance ?? defaultFlowTolerance"
            :disabled="disabled"
            data-testid="graph-flow-tolerance"
            @change="onTolerance"
          />
        </label>
      </div>
    </fieldset>

    <fieldset v-else key="layout" class="fieldset p-0">
      <legend class="fieldset-legend p-0 pb-1">{{ t('graph.toolbar.layout') }}</legend>
      <div class="join" role="group" :aria-label="t('graph.toolbar.layout')">
        <button
          v-for="choice in layouts"
          :key="choice.id"
          type="button"
          class="btn btn-sm join-item"
          :class="{ 'btn-active': layout === choice.id }"
          :aria-pressed="layout === choice.id"
          :disabled="disabled"
          :data-testid="`graph-layout-${choice.id}`"
          @click="emit('update:layout', choice.id)"
        >
          <component :is="choice.icon" class="size-4" aria-hidden="true"/>
          {{ choice.label }}
        </button>
      </div>
    </fieldset>

    <fieldset
      v-if="definitions.length > 0"
      key="edges"
      class="fieldset p-0"
      data-testid="graph-edges"
    >
      <legend class="fieldset-legend p-0 pb-1">{{ t('graph.toolbar.edges') }}</legend>
      <div class="flex flex-wrap items-center gap-2">
        <div class="join" role="group" :aria-label="t('graph.toolbar.edges')">
          <label
            v-for="definition in definitions"
            :key="definition.guid"
            class="btn btn-sm join-item has-[:checked]:btn-active"
            :class="{ 'btn-disabled': disabled || !definition.visible }"
          >
            <input
              type="checkbox"
              class="sr-only"
              :checked="definition.visible && checked(definition.guid)"
              :disabled="disabled || !definition.visible"
              :data-testid="`graph-edge-${definition.guid}`"
              @change="emit('toggleEdge', definition.guid)"
            />
            <Lock v-if="definition.restricted" key="lock" class="size-3" aria-hidden="true"/>
            <bdi>{{ definition.name }}</bdi>
          </label>
        </div>
        <span
          v-if="restrictedCount > 0"
          key="restricted"
          class="badge badge-ghost badge-sm gap-1"
          :title="t('graph.toolbar.restricted')"
          data-testid="graph-edges-restricted"
        >
          <Lock class="size-3" aria-hidden="true"/>
          {{ t('graph.toolbar.restricted') }}
          <span class="font-mono tabular-nums">{{ restrictedCount }}</span>
        </span>
      </div>
    </fieldset>

    <fieldset class="fieldset p-0">
      <legend class="fieldset-legend p-0 pb-1">{{ t('graph.toolbar.showHubs') }}</legend>
      <label class="label h-8 gap-2 text-sm" data-testid="graph-hubs">
        <input
          type="checkbox"
          class="toggle toggle-sm"
          :checked="includeHubs"
          :disabled="disabled"
          :aria-label="t('graph.toolbar.showHubs')"
          data-testid="graph-hubs-toggle"
          @change="emit('update:includeHubs', ($event.target as HTMLInputElement).checked)"
        />
        <span
          class="badge badge-ghost badge-sm font-mono tabular-nums"
          data-testid="graph-hubs-hidden"
        >
          {{ t('graph.toolbar.hubsHidden', {count: hiddenHubs}, hiddenHubs) }}
        </span>
      </label>
    </fieldset>

    <fieldset class="fieldset p-0">
      <legend class="fieldset-legend p-0 pb-1">{{ t('graph.toolbar.minimumAmount') }}</legend>
      <label class="input input-sm w-32">
        <span class="sr-only">{{ t('graph.toolbar.minimumAmount') }}</span>
        <input
          v-model="amountText"
          type="number"
          min="0"
          step="any"
          inputmode="decimal"
          class="tabular-nums"
          :disabled="disabled"
          data-testid="graph-minimum-amount"
          @change="onAmount"
        />
      </label>
    </fieldset>

    <div class="ms-auto flex flex-wrap items-center gap-2 self-end">
      <span class="badge badge-ghost badge-sm font-mono tabular-nums" data-testid="graph-nodes">
        {{ t('graph.toolbar.nodes', {count: nodes, cap}) }}
      </span>
      <button
        type="button"
        class="btn btn-sm"
        :class="{ 'btn-active': explode }"
        :aria-pressed="explode"
        data-testid="graph-explode-toggle"
        @click="emit('update:explode', !explode)"
      >
        <LayoutList class="size-4" aria-hidden="true"/>
        {{ t('graph.toolbar.explode') }}
      </button>
      <button
        v-if="canCapture"
        key="capture"
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="disabled || capturing"
        data-testid="graph-capture"
        @click="emit('capture')"
      >
        <span v-if="capturing" key="busy" class="loading loading-spinner loading-xs"></span>
        <Camera v-else key="icon" class="size-4" aria-hidden="true"/>
        {{ capturing ? t('graph.toolbar.capturing') : t('graph.toolbar.capture') }}
      </button>
    </div>
  </div>
</template>
