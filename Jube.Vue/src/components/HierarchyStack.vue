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
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormModels } from '../formModel/useFormModels'
import {
  decodePath,
  encodePath,
  humanise,
  newLevel,
  parentValueOf,
  type Level,
  type OpenedRecord,
  type PathItem,
} from '../formModel/hierarchy'
import type { FormChild } from '../formModel/types'
import ChildHubs from './ChildHubs.vue'
import PageTrail from './PageTrail.vue'
import CrudPanel from './CrudPanel.vue'

const props = defineProps<{ rootArea: string; rootParent?: number; path?: string }>()

const emit = defineEmits<{ path: [text: string] }>()

const { t } = useI18n()
const { models } = useFormModels()

const levels = ref<Level[]>([])
const pending = ref<PathItem[]>([])
const announcement = ref('')

const trail = computed(() =>
  levels.value.map((level, index) => ({
    index,
    label: level.label,
    name: level.record?.name,
    current: index === levels.value.length - 1,
  })),
)

const crumbs = computed(() =>
  trail.value.map((crumb) => ({
    id: crumb.index,
    label: crumb.name ? `${crumb.label}: ${crumb.name}` : crumb.label,
    backLabel: t('hierarchy.goBack', { label: crumb.label }),
    testid: `crumb-${crumb.index}`,
  })),
)

function modelOf(area: string) {
  return models.value.find((model) => model.area === area)
}

onMounted(() => {
  const items = decodePath(props.path)
  const first = items[0]?.area === props.rootArea ? items[0] : undefined

  pending.value = first ? items.slice(1) : []
  levels.value = [
    newLevel(props.rootArea, humanise(props.rootArea), {
      parentValue: props.rootParent,
      initialId: first?.id,
    }),
  ]
})

function addLevel(
  index: number,
  child: FormChild,
  options: { initialId?: number; startNew?: boolean } = {},
): void {
  const parent = levels.value[index]
  const model = parent && modelOf(parent.area)

  if (!parent?.record || !model) return

  levels.value = [
    ...levels.value.slice(0, index + 1),
    newLevel(child.area, child.label, {
      parentValue: parentValueOf(child, model, parent.record),
      listRoute: child.listRoute,
      ...options,
    }),
  ]

  emit('path', encodePath(levels.value))
}

function opened(index: number, record: OpenedRecord): void {
  const current = levels.value[index]

  if (!current) return

  const same = current.record?.id === record.id

  levels.value = [
    ...levels.value.slice(0, index),
    { ...current, record },
    ...(same ? levels.value.slice(index + 1) : []),
  ]

  const next = pending.value[0]
  const child =
    next && modelOf(current.area)?.children.find((candidate) => candidate.area === next.area)

  if (child && levels.value.length === index + 1) {
    pending.value = pending.value.slice(1)
    addLevel(index, child, { initialId: next.id })
  }

  emit('path', encodePath(levels.value))
}

function closed(index: number): void {
  const current = levels.value[index]

  if (!current?.record) return

  levels.value = [...levels.value.slice(0, index), { ...current, record: undefined }]

  emit('path', encodePath(levels.value))
}

function climb(index: number): void {
  const level = levels.value[index]

  if (!level || index === levels.value.length - 1) return

  levels.value = levels.value.slice(0, index + 1)
  announcement.value = t('hierarchy.showing', {
    where: level.record ? `${level.label}: ${level.record.name}` : level.label,
  })

  emit('path', encodePath(levels.value))
}

function show(index: number, child: FormChild, startNew = false): void {
  addLevel(index, child, { startNew })
  announcement.value = t('hierarchy.showing', { where: child.label })
}
</script>

<template>
  <div class="flex flex-col gap-4" data-testid="hierarchy">
    <PageTrail
      :crumbs="crumbs"
      :label="t('hierarchy.trail')"
      class="sticky top-0 z-20 border-b border-base-300 bg-base-100/95 backdrop-blur"
      data-testid="hierarchy-trail"
      @select="(index) => climb(Number(index))"
    />

    <p class="sr-only" role="status" aria-live="polite" data-testid="hierarchy-announce">
      {{ announcement }}
    </p>

    <section
      v-for="(level, index) in levels"
      v-show="index === levels.length - 1"
      :key="level.key"
      class="flex flex-col gap-4 transition-opacity duration-200 starting:opacity-0 motion-reduce:transition-none"
      :data-testid="`level-${index}`"
    >
      <header class="flex items-center gap-3">
        <h2 class="text-lg font-semibold text-primary">{{ level.label }}</h2>
        <span v-if="level.record" class="badge badge-soft badge-primary font-mono">{{
          level.record.name
        }}</span>
      </header>

      <div class="flex flex-col gap-5" :data-testid="`level-body-${index}`">
        <CrudPanel
          :area="level.area"
          :parent-id="level.parentValue"
          :list-route="level.listRoute"
          :initial-id="level.initialId"
          :start-new="level.startNew"
          @opened="(record) => opened(index, record)"
          @closed="closed(index)"
        />

        <ChildHubs
          v-if="level.record"
          :model="modelOf(level.area)"
          :record="level.record"
          :models="models"
          :data-testid="`hubs-${index}`"
          @open="(child) => show(index, child)"
          @add="(child) => show(index, child, true)"
        />
      </div>
    </section>
  </div>
</template>
