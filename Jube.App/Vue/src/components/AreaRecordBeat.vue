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
import {useSlots} from 'vue'
import {useI18n} from 'vue-i18n'
import {ArrowLeft} from 'lucide-vue-next'
import {useBeatBack} from '../story/beatBack'
import type {OpenedRecord} from '../formModel/hierarchy'
import type {FormChild, FormModel} from '../formModel/types'
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

defineSlots<{ operate?(): unknown; extra?(): unknown }>()

const slots = useSlots()
const {t} = useI18n()
const hosted = useBeatBack(() => ({label: props.backLabel, testid: 'record-back', run: () => emit('back')}))
</script>

<template>
  <div class="flex flex-col gap-3" data-testid="record-beat">
    <div>
      <button v-if="!hosted" type="button" class="btn btn-sm" data-testid="record-back" @click="emit('back')">
        <ArrowLeft class="size-4 rtl:rotate-180" aria-hidden="true"/>
        {{ t('story.backTo', {label: backLabel}) }}
      </button>
    </div>

    <ChildHubs
      :model="model"
      :record="record"
      :models="models"
      @open="(child) => emit('child', child, false)"
      @add="(child) => emit('child', child, true)"
    >
      <template v-if="slots.operate" #operate>
        <slot name="operate"/>
      </template>
      <template v-if="slots.extra" #extra>
        <slot name="extra"/>
      </template>
    </ChildHubs>

    <section data-testid="record-properties">
      <CrudPanel
        v-if="model"
        :area="model.area"
        :parent-id="parentId"
        :list-route="listRoute"
        :initial-id="record.id"
        form-only
        @opened="(opened) => emit('opened', opened)"
        @closed="emit('back')"
        @deleted="emit('back')"
        @dirty="(value) => emit('dirty', value)"
      >
      </CrudPanel>
    </section>
  </div>
</template>
