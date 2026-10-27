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
import { computed, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { ChevronRight, EllipsisVertical, Plus } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { formModelApi } from '../formModel/api'
import { summariseChildren } from '../formModel/hierarchy'
import type { FormChild, FormModel } from '../formModel/types'

const props = defineProps<{
  child: FormChild
  childModel: FormModel | undefined
  parentValue: number | string | undefined
  awaiting?: number
}>()

const emit = defineEmits<{ open: []; add: [] }>()

const { t } = useI18n()
const menuId = `hub-menu-${useId()}`

const rows = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.child.area, props.parentValue)),
  queryFn: () => formModelApi.records(props.childModel!, props.parentValue, props.child.listRoute),
  enabled: computed(() => props.childModel !== undefined && props.parentValue !== undefined),
  retry: false,
})

const summary = computed(() => summariseChildren(props.childModel, rows.data.value ?? []))
const canAdd = computed(() => props.childModel?.operations.includes('create') ?? false)
const anchor = computed(() => `anchor-name: --${menuId}`)
const position = computed(() => `position-anchor: --${menuId}`)
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" :data-testid="`hub-${child.area}`">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h4 class="card-title text-base">{{ child.label }}</h4>
        <span
          v-if="rows.isPending.value"
          key="pending"
          class="skeleton h-6 w-10"
          role="status"
          :aria-label="t('crud.loading')"
        ></span>
        <span v-else-if="rows.isError.value" key="error" class="badge badge-error badge-sm">{{
          t('hierarchy.failed')
        }}</span>
        <span
          v-else
          key="total"
          class="font-mono text-2xl font-semibold tabular-nums"
          :data-testid="`hub-total-${child.area}`"
          >{{ summary.total }}</span
        >
      </div>

      <p class="flex min-h-5 flex-wrap items-center gap-x-3 gap-y-1 text-sm text-secondary">
        <template v-if="rows.isSuccess.value && summary.total === 0">
          <span>{{ t('hierarchy.none') }}</span>
        </template>
        <template v-else-if="rows.isSuccess.value">
          <span v-if="summary.active !== null" class="flex items-center gap-1.5">
            <span class="status status-success" aria-hidden="true"></span>
            {{ t('hierarchy.active', { count: summary.active }) }}
          </span>
          <span
            v-if="summary.locked !== null && summary.locked > 0"
            class="flex items-center gap-1.5"
          >
            <span class="status status-warning" aria-hidden="true"></span>
            {{ t('hierarchy.locked', { count: summary.locked }) }}
          </span>
        </template>
      </p>

      <p v-if="awaiting !== undefined && awaiting > 0" key="awaiting">
        <span
          class="badge badge-warning badge-sm font-mono"
          :data-testid="`hub-awaiting-${child.area}`"
          >{{ t('hierarchy.awaiting', { count: awaiting }) }}</span
        >
      </p>

      <div class="card-actions items-center justify-end gap-1">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`hub-open-${child.area}`"
          @click="emit('open')"
        >
          {{ t('hierarchy.open') }}
          <ChevronRight aria-hidden="true" class="size-3.5" />
        </button>
        <template v-if="canAdd">
          <button
            type="button"
            class="btn btn-sm btn-square btn-ghost"
            :popovertarget="menuId"
            :style="anchor"
            :aria-label="t('hierarchy.more', { label: child.label })"
            :data-testid="`hub-more-${child.area}`"
          >
            <EllipsisVertical aria-hidden="true" class="size-4" />
          </button>
          <ul
            :id="menuId"
            popover
            class="dropdown dropdown-end menu z-50 w-52 rounded-box bg-base-100 p-2 shadow-md"
            :style="position"
          >
            <li>
              <button type="button" :data-testid="`hub-add-${child.area}`" @click="emit('add')">
                <Plus aria-hidden="true" class="size-4" />
                {{ t('hierarchy.add', { label: child.label }) }}
              </button>
            </li>
          </ul>
        </template>
      </div>
    </div>
  </div>
</template>
