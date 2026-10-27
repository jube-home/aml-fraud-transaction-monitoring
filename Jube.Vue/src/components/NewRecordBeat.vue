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
 */

 -->

<script setup vapor lang="ts">
import { useI18n } from 'vue-i18n'
import { ArrowLeft } from 'lucide-vue-next'
import type { OpenedRecord } from '../formModel/hierarchy'
import CrudPanel from './CrudPanel.vue'

defineProps<{
  area: string
  backLabel: string
  parentId?: number | string
  listRoute?: string
}>()

const emit = defineEmits<{
  back: []
  opened: [record: OpenedRecord]
  dirty: [dirty: boolean]
}>()

const { t } = useI18n()
</script>

<template>
  <div class="flex flex-col gap-5" data-testid="new-record-beat">
    <div
      class="flex flex-wrap items-center gap-x-4 gap-y-2 rounded-box border border-base-300 bg-base-200 px-4 py-3 text-sm"
    >
      <button type="button" class="btn btn-sm" data-testid="new-back" @click="emit('back')">
        <ArrowLeft class="size-4" aria-hidden="true" />
        {{ t('story.backTo', { label: backLabel }) }}
      </button>
      <span class="text-xs text-secondary">{{ t('story.newHint') }}</span>
    </div>

    <CrudPanel
      :area="area"
      :parent-id="parentId"
      :list-route="listRoute"
      start-new
      form-only
      @opened="(record) => emit('opened', record)"
      @closed="emit('back')"
      @dirty="(value) => emit('dirty', value)"
    />
  </div>
</template>
