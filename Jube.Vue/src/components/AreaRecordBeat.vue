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
import { computed, ref, useSlots } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowLeft } from 'lucide-vue-next'
import { useFormat } from '../composables/useFormat'
import type { OpenedRecord } from '../formModel/hierarchy'
import type { FormChild, FormModel } from '../formModel/types'
import ChildHubs from './ChildHubs.vue'
import CrudPanel from './CrudPanel.vue'

const props = defineProps<{
  record: OpenedRecord
  model: FormModel | undefined
  models: FormModel[]
  backLabel: string
  parentId?: number | string
  listRoute?: string
}>()

const emit = defineEmits<{
  child: [child: FormChild, startNew: boolean]
  back: []
  opened: [record: OpenedRecord]
  dirty: [dirty: boolean]
}>()

defineSlots<{ extra?(): unknown }>()

const slots = useSlots()
const { t } = useI18n()
const { dateTime } = useFormat()

const hasChildren = computed(() => (props.model?.children.length ?? 0) > 0 || !!slots.extra)
const open = ref(!hasChildren.value)
const mounted = ref(open.value)

const updated = computed(() => {
  const when = props.record.values.updatedDate

  return typeof when === 'string' && when !== '' ? dateTime(when) : undefined
})

const updatedBy = computed(() => {
  const by = props.record.values.updatedUser ?? props.record.values.createdUser

  return typeof by === 'string' && by !== '' ? by : undefined
})

function toggled(event: Event): void {
  const details = event.target as HTMLDetailsElement

  open.value = details.open

  if (details.open) mounted.value = true
}

function formClosed(): void {
  if (hasChildren.value) open.value = false
  else emit('back')
}
</script>

<template>
  <div class="flex flex-col gap-5" data-testid="record-beat">
    <div
      class="flex flex-wrap items-center gap-x-4 gap-y-2 rounded-box border border-base-300 bg-base-200 px-4 py-3 text-sm"
      data-testid="record-strip"
    >
      <button type="button" class="btn btn-sm" data-testid="record-back" @click="emit('back')">
        <ArrowLeft class="size-4" aria-hidden="true" />
        {{ t('story.backTo', { label: backLabel }) }}
      </button>
      <span v-if="updatedBy" key="by" class="flex items-center gap-2">
        <span class="text-xs text-secondary">{{ t('models.strip.updatedBy') }}</span>
        <span class="font-semibold">{{ updatedBy }}</span>
      </span>
      <span v-if="updated" key="when" class="badge badge-sm badge-neutral font-mono">{{
        updated
      }}</span>
    </div>

    <ChildHubs
      :model="model"
      :record="record"
      :models="models"
      @open="(child) => emit('child', child, false)"
      @add="(child) => emit('child', child, true)"
    >
      <template v-if="slots.extra" #extra>
        <slot name="extra" />
      </template>
    </ChildHubs>

    <details
      class="collapse collapse-arrow rounded-box border border-base-300 bg-base-100"
      :open="open"
      data-testid="record-properties"
      @toggle="toggled"
    >
      <summary class="collapse-title flex flex-wrap items-baseline gap-x-3 gap-y-1 py-3">
        <h3 class="text-sm font-semibold">{{ t('story.properties.title') }}</h3>
        <span class="text-xs font-normal text-secondary">{{ t('story.properties.hint') }}</span>
      </summary>
      <div class="collapse-content flex min-w-0 flex-col gap-4">
        <CrudPanel
          v-if="mounted && model"
          :area="model.area"
          :parent-id="parentId"
          :list-route="listRoute"
          :initial-id="record.id"
          form-only
          @opened="(opened) => emit('opened', opened)"
          @closed="formClosed"
          @deleted="emit('back')"
          @dirty="(value) => emit('dirty', value)"
        />
      </div>
    </details>
  </div>
</template>
