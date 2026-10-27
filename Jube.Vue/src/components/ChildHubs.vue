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
import { computed, useSlots } from 'vue'
import { useI18n } from 'vue-i18n'
import { hubsOf, parentValueOf, type OpenedRecord } from '../formModel/hierarchy'
import type { FormChild, FormModel } from '../formModel/types'
import ChildHubCard from './ChildHubCard.vue'

const props = defineProps<{
  model: FormModel | undefined
  record: OpenedRecord
  models: FormModel[]
  awaiting?: Map<number, number>
  otherLabel?: string
}>()

const emit = defineEmits<{ open: [child: FormChild]; add: [child: FormChild] }>()

defineSlots<{ extra?(): unknown }>()

const { t } = useI18n()
const slots = useSlots()

const hubs = computed(() => hubsOf(props.model?.children ?? []))

function childModel(child: FormChild): FormModel | undefined {
  return props.models.find((candidate) => candidate.area === child.area)
}

function awaitingFor(child: FormChild): number | undefined {
  const kind = childModel(child)?.approval?.kind

  return kind === undefined ? undefined : props.awaiting?.get(kind)
}

function headingOf(hub: string): string {
  return hub === '' ? (props.otherLabel ?? '') : hub
}
</script>

<template>
  <div v-if="hubs.length > 0 || slots.extra" class="flex flex-col gap-4">
    <h3 class="text-sm font-semibold">
      {{ t('hierarchy.under', { name: record.name }) }}
    </h3>
    <div v-for="hub in hubs" :key="hub.hub || hub.children[0]!.area" class="flex flex-col gap-2">
      <p v-if="headingOf(hub.hub)" class="text-xs font-medium text-secondary">
        {{ headingOf(hub.hub) }}
      </p>
      <div class="grid grid-cols-[repeat(auto-fill,minmax(15rem,1fr))] gap-3">
        <ChildHubCard
          v-for="child in hub.children"
          :key="child.area"
          :child="child"
          :child-model="childModel(child)"
          :parent-value="model ? parentValueOf(child, model, record) : undefined"
          :awaiting="awaitingFor(child)"
          @open="emit('open', child)"
          @add="emit('add', child)"
        />
      </div>
    </div>
    <slot name="extra" />
  </div>
</template>
