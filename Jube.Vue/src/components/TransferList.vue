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
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-vue-next'

export interface TransferItem {
  key: string
  label: string
}

const props = defineProps<{
  items: TransferItem[]
  modelValue: ReadonlySet<string>
  availableTitle: string
  assignedTitle: string
  disabled?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: Set<string>] }>()

const { t } = useI18n()

const availableSelection = ref<string[]>([])
const assignedSelection = ref<string[]>([])

const available = computed(() => props.items.filter((item) => !props.modelValue.has(item.key)))
const assigned = computed(() => props.items.filter((item) => props.modelValue.has(item.key)))

function commit(next: Set<string>): void {
  availableSelection.value = []
  assignedSelection.value = []
  emit('update:modelValue', next)
}

function assign(keys: string[]): void {
  commit(new Set([...props.modelValue, ...keys]))
}

function unassign(keys: string[]): void {
  const drop = new Set(keys)

  commit(new Set([...props.modelValue].filter((key) => !drop.has(key))))
}
</script>

<template>
  <div
    class="grid items-stretch gap-3 md:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)]"
    data-testid="transfer-list"
  >
    <section
      class="flex min-w-0 flex-col overflow-hidden rounded-box border border-base-300 bg-base-100"
      :aria-labelledby="'transfer-available-title'"
    >
      <h3
        id="transfer-available-title"
        class="flex items-center justify-between gap-2 border-b border-base-300 bg-base-200 px-3 py-2 text-sm font-semibold"
      >
        {{ availableTitle }}
        <span class="badge badge-ghost badge-sm font-mono" data-testid="transfer-available-count">
          {{ available.length }}
        </span>
      </h3>
      <ul class="h-64 overflow-y-auto py-1" data-testid="transfer-available">
        <li v-for="item in available" :key="item.key">
          <label
            class="flex cursor-pointer items-center gap-3 px-3 py-1.5 hover:bg-base-200"
            @dblclick="assign([item.key])"
          >
            <input
              v-model="availableSelection"
              type="checkbox"
              class="checkbox checkbox-sm"
              :value="item.key"
              :disabled="disabled"
              :data-testid="`transfer-item-${item.key}`"
            />
            <span class="min-w-0 flex-1 truncate">{{ item.label }}</span>
          </label>
        </li>
        <li v-if="available.length === 0" class="px-3 py-2 text-sm">
          {{ t('transferList.empty') }}
        </li>
      </ul>
    </section>

    <div class="flex flex-row items-center justify-center gap-2 md:flex-col">
      <button
        type="button"
        class="btn btn-sm btn-square"
        :aria-label="t('transferList.assignSelected')"
        :disabled="disabled || availableSelection.length === 0"
        data-testid="transfer-assign"
        @click="assign(availableSelection)"
      >
        <ChevronRight aria-hidden="true" class="size-4 rtl:rotate-180" />
      </button>
      <button
        type="button"
        class="btn btn-sm btn-square"
        :aria-label="t('transferList.assignAll')"
        :disabled="disabled || available.length === 0"
        data-testid="transfer-assign-all"
        @click="assign(available.map((item) => item.key))"
      >
        <ChevronsRight aria-hidden="true" class="size-4 rtl:rotate-180" />
      </button>
      <button
        type="button"
        class="btn btn-sm btn-square"
        :aria-label="t('transferList.unassignSelected')"
        :disabled="disabled || assignedSelection.length === 0"
        data-testid="transfer-unassign"
        @click="unassign(assignedSelection)"
      >
        <ChevronLeft aria-hidden="true" class="size-4 rtl:rotate-180" />
      </button>
      <button
        type="button"
        class="btn btn-sm btn-square"
        :aria-label="t('transferList.unassignAll')"
        :disabled="disabled || assigned.length === 0"
        data-testid="transfer-unassign-all"
        @click="unassign(assigned.map((item) => item.key))"
      >
        <ChevronsLeft aria-hidden="true" class="size-4 rtl:rotate-180" />
      </button>
    </div>

    <section
      class="flex min-w-0 flex-col overflow-hidden rounded-box border border-base-300 bg-base-100"
      :aria-labelledby="'transfer-assigned-title'"
    >
      <h3
        id="transfer-assigned-title"
        class="flex items-center justify-between gap-2 border-b border-base-300 bg-base-200 px-3 py-2 text-sm font-semibold"
      >
        {{ assignedTitle }}
        <span class="badge badge-ghost badge-sm font-mono" data-testid="transfer-assigned-count">
          {{ assigned.length }}
        </span>
      </h3>
      <ul class="h-64 overflow-y-auto py-1" data-testid="transfer-assigned">
        <li v-for="item in assigned" :key="item.key">
          <label
            class="flex cursor-pointer items-center gap-3 px-3 py-1.5 hover:bg-base-200"
            @dblclick="unassign([item.key])"
          >
            <input
              v-model="assignedSelection"
              type="checkbox"
              class="checkbox checkbox-sm"
              :value="item.key"
              :disabled="disabled"
              :data-testid="`transfer-item-${item.key}`"
            />
            <span class="min-w-0 flex-1 truncate">{{ item.label }}</span>
          </label>
        </li>
        <li v-if="assigned.length === 0" class="px-3 py-2 text-sm">
          {{ t('transferList.empty') }}
        </li>
      </ul>
    </section>
  </div>
</template>
