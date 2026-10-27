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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { formModelApi } from '../formModel/api'
import { parentValueOf, recordName } from '../formModel/hierarchy'
import type { FormChild, RecordValues } from '../formModel/types'
import type { GridExpandEvent } from './dataGridTypes'
import ScrollTabs from './ScrollTabs.vue'
import SchemaList from './SchemaList.vue'

interface Step {
  area: string
  id: number
}

const props = defineProps<{
  area: string
  parentValue?: number | string
  listRoute?: string
  trail: Step[]
}>()

const router = useRouter()
const { t } = useI18n()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel(props.area)),
  queryFn: () => formModelApi.model(props.area),
  retry: false,
})

const modelValue = computed(() => model.data.value)

const records = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.area, props.parentValue)),
  queryFn: () => formModelApi.records(modelValue.value!, props.parentValue, props.listRoute),
  enabled: computed(() => modelValue.value !== undefined),
  retry: false,
})

const rows = computed(() => records.data.value ?? [])
const hosts = ref(new Map<unknown, HTMLElement>())
const chosen = ref(new Map<unknown, string>())

function rowById(id: unknown): RecordValues | undefined {
  const key = modelValue.value?.keys.id ?? 'id'

  return rows.value.find((row) => row[key] === id)
}

function onExpand(event: GridExpandEvent): void {
  const next = new Map(hosts.value)

  if (event.host) next.set(event.id, event.host)
  else next.delete(event.id)

  hosts.value = next
}

function kindsFor(): FormChild[] {
  return modelValue.value?.children ?? []
}

function kindOf(id: unknown): FormChild | undefined {
  const kinds = kindsFor()

  return kinds.find((kind) => kind.area === chosen.value.get(id)) ?? kinds[0]
}

function onTab(event: MouseEvent, id: unknown, area: string): void {
  choose(id, area)
  ;(event.currentTarget as HTMLElement).scrollIntoView({
    behavior: 'smooth',
    block: 'nearest',
    inline: 'center',
  })
}

function choose(id: unknown, area: string): void {
  chosen.value = new Map(chosen.value).set(id, area)
}

function trailFor(id: unknown): Step[] {
  return [...props.trail, { area: props.area, id: id as number }]
}

function parentOfRow(id: unknown, kind: FormChild): number | string | undefined {
  const row = rowById(id)
  const model = modelValue.value

  if (!row || !model) return undefined

  const guid = row.guid

  return parentValueOf(kind, model, {
    id: id as number,
    guid: typeof guid === 'string' ? guid : undefined,
    name: recordName(model, row, id as number),
    values: row,
  })
}

function open(id: number): void {
  const steps = [...props.trail, { area: props.area, id }]

  void router.push({
    path: '/tools',
    query: {
      tool: 'crud',
      area: steps[0]!.area,
      path: steps.map((step) => `${step.area}:${step.id}`).join(','),
    },
  })
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" :data-testid="`grid-level-${area}`">
    <SchemaList
      v-if="modelValue && records.isSuccess.value"
      :model="modelValue"
      :rows="rows"
      :can-open="modelValue.operations.includes('read')"
      :expandable="modelValue.children.length > 0"
      auto-height
      @open="open"
      @expand="onExpand"
    />
    <p
      v-else-if="model.isError.value || records.isError.value"
      role="alert"
      class="alert alert-error"
    >
      {{ area }}
    </p>
    <div v-else class="skeleton h-24 w-full" role="status"></div>

    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <div class="flex flex-col gap-3" :data-testid="`grid-detail-${id}`">
          <ScrollTabs
            v-if="kindsFor().length > 1"
            :label="t('hierarchy.kinds')"
            :data-testid="`grid-kinds-${id}`"
          >
            <button
              v-for="kind in kindsFor()"
              :key="kind.area"
              type="button"
              role="tab"
              :class="[
                'tab shrink-0 whitespace-nowrap',
                kindOf(id)?.area === kind.area ? 'tab-active' : '',
              ]"
              :aria-selected="kindOf(id)?.area === kind.area"
              :data-testid="`grid-kind-${kind.area}`"
              @click="onTab($event, id, kind.area)"
            >
              {{ kind.label }}
            </button>
          </ScrollTabs>
          <p v-else-if="kindOf(id)" class="text-sm font-semibold text-primary">
            {{ kindOf(id)!.label }}
          </p>

          <GridLevel
            v-if="kindOf(id)"
            :key="kindOf(id)!.area"
            :area="kindOf(id)!.area"
            :parent-value="parentOfRow(id, kindOf(id)!)"
            :list-route="kindOf(id)!.listRoute"
            :trail="trailFor(id)"
          />
        </div>
      </Teleport>
    </template>
  </div>
</template>
