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
import {useQuery} from '@tanstack/vue-query'
import {ChevronRight, ListTree} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {formModelApi} from '../formModel/api'
import {parentValueOf, recordName, summariseChildren} from '../formModel/hierarchy'
import type {FormChild, RecordValues} from '../formModel/types'
import type {StoryStep} from '../story/path'
import {viewStateFor} from '../story/viewState'
import ChildHubCard from './ChildHubCard.vue'
import HubDeck from './HubDeck.vue'

const props = defineProps<{
  child: FormChild
  parentValue: number | string | undefined
}>()

const emit = defineEmits<{ open: [steps: StoryStep[]] }>()

const shown = 25

const {t} = useI18n()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel(props.child.area)),
  queryFn: () => formModelApi.model(props.child.area),
  staleTime: 30_000,
  retry: false,
})

const rows = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.child.area, props.parentValue)),
  queryFn: () =>
    formModelApi.records(model.data.value!, props.parentValue, props.child.listRoute || undefined),
  enabled: computed(() => model.data.value !== undefined && props.parentValue !== undefined),
  retry: false,
})

const summary = computed(() => summariseChildren(model.data.value, rows.data.value ?? []))
const visible = computed(() => (rows.data.value ?? []).slice(0, shown))
const grandchildren = computed(() =>
  [...(model.data.value?.children ?? [])].sort((a, b) => a.order - b.order),
)
const canOpen = computed(() => model.data.value?.operations.includes('read') ?? false)
const lockName = computed(() => model.data.value?.lockFields[0])

const kept = viewStateFor('glimpses')
const glimpseKey = `${props.child.area}:${props.parentValue ?? ''}`
const mine = (kept.glimpses[glimpseKey] ??= {open: false, rows: []})
const expanded = ref(mine.open)
const opened = ref<number[]>([...mine.rows])

watch(expanded, (now) => {
  mine.open = now
})
watch(opened, (now) => {
  mine.rows = [...now]
})

function idOf(row: RecordValues): number {
  return row[model.data.value!.keys.id] as number
}

function nameOf(row: RecordValues): string {
  return recordName(model.data.value!, row, idOf(row))
}

function parentOf(grandchild: FormChild, row: RecordValues): number | string | undefined {
  const guid = row.guid

  return parentValueOf(grandchild, model.data.value!, {
    id: idOf(row),
    guid: typeof guid === 'string' && guid !== '' ? guid : undefined,
    name: nameOf(row),
    values: row,
  })
}

function toggle(row: RecordValues): void {
  const id = idOf(row)

  opened.value = opened.value.includes(id)
    ? opened.value.filter((candidate) => candidate !== id)
    : [...opened.value, id]
}

function open(row: RecordValues, below: StoryStep[] = []): void {
  emit('open', [{area: props.child.area, id: idOf(row)}, ...below])
}
</script>

<template>
  <li
    class="flex min-w-0 flex-col gap-2"
    :class="expanded ? 'col-span-full' : ''"
    :data-testid="`glimpse-${child.area}`"
  >
    <ChildHubCard
      :child="child"
      :child-model="model.data.value"
      :parent-value="parentValue"
      unordered
      @open="emit('open', [{area: child.area}])"
      @add="emit('open', [{area: child.area, startNew: true}])"
    >
      <button
        type="button"
        class="btn btn-ghost btn-xs btn-square relative z-10 shrink-0"
        :disabled="summary.total === 0"
        :aria-expanded="expanded"
        :aria-label="
          t(expanded ? 'crud.summaryHide' : 'crud.summaryShow', {label: child.label})
        "
        :data-testid="`glimpse-${child.area}-toggle`"
        @click="expanded = !expanded"
      >
        <ListTree aria-hidden="true" class="size-4" :class="expanded ? 'text-primary' : ''"/>
      </button>
    </ChildHubCard>
    <ul
      v-if="expanded && summary.total > 0"
      class="list rounded-field border border-base-300 bg-base-100"
      :aria-label="child.label"
      :data-testid="`glimpse-${child.area}-records`"
    >
      <li
        v-for="row in visible"
        :key="idOf(row)"
        class="list-row px-3 py-1.5"
        :data-testid="`glimpse-${child.area}-record-${idOf(row)}`"
      >
        <div class="list-col-grow flex min-w-0 flex-col gap-2">
        <div class="flex min-w-0 items-center gap-2">
        <div class="flex min-w-0 flex-1 flex-wrap items-center gap-2">
          <span class="truncate text-sm" dir="auto" :title="nameOf(row)">{{ nameOf(row) }}</span>
          <template v-if="summary.active !== null">
            <span v-if="row.active === true" key="active" class="badge badge-sm badge-success">{{
                t('models.active')
              }}</span>
            <span v-else key="inactive" class="badge badge-soft badge-sm">{{
                t('models.inactive')
              }}</span>
          </template>
          <span
            v-if="lockName && row[lockName] === true"
            key="locked"
            class="badge badge-sm badge-warning"
          >{{ t('models.locked') }}</span
          >
        </div>
        <button
          v-if="canOpen"
          type="button"
          class="btn btn-xs"
          :aria-label="`${t('crud.open')}: ${nameOf(row)}`"
          :data-testid="`glimpse-${child.area}-open-${idOf(row)}`"
          @click="open(row)"
        >
          {{ t('crud.open') }}
        </button>
        <button
          v-if="grandchildren.length > 0"
          type="button"
          class="btn btn-ghost btn-xs btn-circle"
          :aria-expanded="opened.includes(idOf(row))"
          :aria-label="
            t(opened.includes(idOf(row)) ? 'crud.summaryHide' : 'crud.summaryShow', {
              label: nameOf(row),
            })
          "
          :data-testid="`glimpse-${child.area}-below-${idOf(row)}`"
          @click="toggle(row)"
        >
          <ChevronRight
            aria-hidden="true"
            class="size-4"
            :class="opened.includes(idOf(row)) ? 'rotate-90' : 'rtl:rotate-180'"
          />
        </button>
        </div>
        <ul
          v-if="opened.includes(idOf(row))"
          class="grid gap-2 sm:grid-cols-2 xl:grid-cols-3"
          :aria-label="nameOf(row)"
        >
          <HubDeck bare>
            <RecordGlimpse
              v-for="grandchild in grandchildren"
              :key="grandchild.area"
              :child="grandchild"
              :parent-value="parentOf(grandchild, row)"
              @open="(below) => open(row, below)"
            />
          </HubDeck>
        </ul>
        </div>
      </li>
      <li v-if="summary.total > shown" key="more" class="px-3 py-1.5 text-xs text-secondary">
        {{ t('crud.summaryMore', {count: summary.total - shown}) }}
      </li>
    </ul>
  </li>
</template>
