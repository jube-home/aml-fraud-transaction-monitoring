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
import { CircleDashed, Ellipsis, Hexagon, Layers, MoveRight, Star, Tag } from 'lucide-vue-next'
import { edgeKindKey, legendKeys, type GraphEdgeKindKey } from './state'
import { iconComponent, nodeIcon, nodeIconMap } from './icons'
import type { GraphEdgeDefinition } from './types'

const props = defineProps<{
  definitions: GraphEdgeDefinition[]
  keys: string[]
  seedKey?: string | null
}>()

const { t } = useI18n()

const configuredIcons = computed(() => nodeIconMap(props.definitions))

const keyed = computed(() => {
  return legendKeys(props.keys, props.seedKey).map((entry) => ({
    key: entry.key,
    icon: nodeIcon(entry.key, configuredIcons.value),
  }))
})

const kindLabels = computed<Record<GraphEdgeKindKey, string>>(() => ({
  identity: t('graph.kind.identity'),
  money: t('graph.kind.money'),
  ownership: t('graph.kind.ownership'),
  resolution: t('graph.kind.resolution'),
  case: t('graph.kind.case'),
}))

function kindLabel(kindId: number): string {
  const key = edgeKindKey(kindId)

  return key === null ? '' : kindLabels.value[key]
}
</script>

<template>
  <dl class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs" data-testid="graph-legend">
    <div class="flex items-center gap-1">
      <dt class="text-secondary">{{ t('graph.legend.title') }}</dt>
    </div>
    <div v-for="entry in keyed" :key="entry.key" class="flex items-center gap-1">
      <dt>
        <component :is="entry.icon" class="size-3.5" aria-hidden="true" />
      </dt>
      <dd class="font-mono">{{ entry.key }}</dd>
    </div>
    <div class="flex items-center gap-1">
      <dt>
        <Hexagon class="size-3.5" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.hub') }}</dd>
    </div>
    <div class="flex items-center gap-1">
      <dt>
        <Layers class="size-3.5" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.cluster') }}</dd>
    </div>
    <div class="flex items-center gap-1">
      <dt>
        <Star class="size-3.5 text-error" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.confirmed') }}</dd>
    </div>
    <div class="flex items-center gap-1">
      <dt>
        <CircleDashed class="size-3.5" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.openCase') }}</dd>
    </div>
    <div class="flex items-center gap-1">
      <dt>
        <Ellipsis class="size-3.5" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.soft') }}</dd>
    </div>
    <div class="flex items-center gap-1">
      <dt>
        <MoveRight class="size-3.5 rtl:rotate-180" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.directed') }}</dd>
    </div>
    <div class="flex items-center gap-1" data-testid="graph-legend-tags">
      <dt>
        <Tag class="size-3.5" aria-hidden="true" />
      </dt>
      <dd>{{ t('graph.legend.tags') }}</dd>
    </div>
    <div
      v-for="definition in definitions"
      :key="definition.guid"
      class="flex items-center gap-1"
      :data-testid="`graph-legend-edge-${definition.guid}`"
    >
      <dt dir="auto" class="badge badge-ghost badge-xs gap-1 font-mono">
        <component
          :is="iconComponent(definition.iconName)"
          v-if="iconComponent(definition.iconName)"
          class="size-3"
          aria-hidden="true"
        />
        {{ definition.name }}
      </dt>
      <dd v-if="kindLabel(definition.kindId) !== ''" key="kind" class="text-secondary">
        {{ kindLabel(definition.kindId) }}
      </dd>
      <dd v-if="definition.restricted" key="restricted" class="badge badge-soft badge-xs">
        {{ t('graph.legend.restricted') }}
      </dd>
    </div>
  </dl>
</template>
