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
import {computed, useSlots} from 'vue'
import {parentValueOf, type OpenedRecord} from '../formModel/hierarchy'
import type {FormChild, FormModel} from '../formModel/types'
import ChildHubCard from './ChildHubCard.vue'

const props = defineProps<{
  model: FormModel | undefined
  record: OpenedRecord
  models: FormModel[]
  awaiting?: Map<number, number>
}>()

const emit = defineEmits<{ open: [child: FormChild]; add: [child: FormChild] }>()

defineSlots<{ operate?(): unknown; extra?(): unknown }>()

const slots = useSlots()

const children = computed(() =>
  [...(props.model?.children ?? [])].sort((a, b) => a.order - b.order),
)

function childModel(child: FormChild): FormModel | undefined {
  return props.models.find((candidate) => candidate.area === child.area)
}

function awaitingFor(child: FormChild): number | undefined {
  const kind = childModel(child)?.approval?.kind

  return kind === undefined ? undefined : props.awaiting?.get(kind)
}
</script>

<template>
  <div
    v-if="children.length > 0 || slots.extra || slots.operate"
    class="flex flex-col gap-2"
    data-testid="child-hubs"
  >
    <template v-if="slots.operate">
      <div
        class="grid grid-cols-[repeat(auto-fill,minmax(17rem,1fr))] gap-2"
        data-testid="operate-hubs"
      >
        <slot name="operate"/>
      </div>
      <div class="divider my-0"></div>
    </template>
    <div
      class="grid grid-cols-[repeat(auto-fill,minmax(17rem,1fr))] gap-2"
      data-testid="pipeline-hubs"
    >
      <ChildHubCard
        v-for="child in children"
        :key="child.area"
        :child="child"
        :child-model="childModel(child)"
        :parent-value="model ? parentValueOf(child, model, record) : undefined"
        :awaiting="awaitingFor(child)"
        @open="emit('open', child)"
        @add="emit('add', child)"
      />
      <slot name="extra"/>
    </div>
  </div>
</template>
