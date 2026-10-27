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
import { computed, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Tag, Undo2 } from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import type { PresentTag } from './tags'

const props = defineProps<{
  count: number
  total: number
  present: PresentTag[]
  options: string[]
  optionsFailed: boolean
  busy: boolean
  undoText: string | null
}>()

const emit = defineEmits<{
  selectAll: []
  clear: []
  apply: [change: { add: string[]; remove: string[] }]
  undo: []
  dismiss: []
}>()

const { t } = useI18n()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const add = ref<string[]>([])
const remove = ref<string[]>([])

const ready = computed(() => add.value.length > 0 || remove.value.length > 0)

watch(
  () => props.undoText,
  () => {
    if (props.undoText !== null) dialog.value?.close?.()
  },
)

function show(): void {
  add.value = []
  remove.value = []
  dialog.value?.showModal()
}

function hide(): void {
  dialog.value?.close?.()
}

function toggle(list: 'add' | 'remove', name: string, on: boolean): void {
  const other = list === 'add' ? remove : add
  const own = list === 'add' ? add : remove

  own.value = on
    ? [...own.value.filter((item) => item !== name), name]
    : own.value.filter((item) => item !== name)

  if (on) other.value = other.value.filter((item) => item !== name)
}

function apply(): void {
  emit('apply', { add: [...add.value], remove: [...remove.value] })
}
</script>

<template>
  <div class="contents" data-testid="journal-tag-bar">
    <span v-if="count > 0" key="selected" class="text-sm" data-testid="journal-selected">
      {{ t('journal.tags.selected', { n: count }) }}
    </span>
    <button
      v-if="count > 0"
      key="tag"
      type="button"
      class="btn btn-sm btn-primary"
      data-testid="journal-tag-open"
      :disabled="busy"
      @click="show"
    >
      <Tag class="size-4" aria-hidden="true" />
      {{ t('journal.tags.tag') }}
    </button>
    <button
      v-if="count > 0"
      key="clear"
      type="button"
      class="btn btn-ghost btn-sm"
      data-testid="journal-select-clear"
      @click="emit('clear')"
    >
      {{ t('journal.tags.clear') }}
    </button>
    <button
      v-if="count < total"
      key="all"
      type="button"
      class="btn btn-ghost btn-sm"
      data-testid="journal-select-all"
      @click="emit('selectAll')"
    >
      {{ t('journal.tags.selectAll', { n: total }) }}
    </button>
    <slot />
    <FieldHelp :label="t('journal.tags.tag')" :text="t('journal.help.tags')" />

    <div
      v-if="undoText !== null"
      key="undo"
      role="status"
      class="alert alert-soft alert-vertical sm:alert-horizontal basis-full"
      data-testid="journal-tag-undo"
    >
      <span>{{ undoText }}</span>
      <div class="flex gap-2">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="journal-tag-undo-run"
          :disabled="busy"
          @click="emit('undo')"
        >
          <Undo2 class="size-4" aria-hidden="true" />
          {{ t('journal.tags.undo') }}
        </button>
        <button type="button" class="btn btn-sm btn-ghost" @click="emit('dismiss')">
          {{ t('journal.tags.dismiss') }}
        </button>
      </div>
    </div>

    <dialog ref="dialog" class="modal" data-testid="journal-tag-dialog" @cancel.prevent="hide">
      <div class="modal-box">
        <h3 class="text-lg font-bold">{{ t('journal.tags.title', { n: count }) }}</h3>

        <fieldset class="fieldset mt-3">
          <legend class="fieldset-legend">{{ t('journal.tags.add') }}</legend>
          <p v-if="optionsFailed" key="failed" class="text-sm" role="alert">
            {{ t('journal.tags.optionsFailed') }}
          </p>
          <p v-else-if="options.length === 0" key="none" class="text-sm">
            {{ t('journal.tags.noOptions') }}
          </p>
          <label v-for="name in options" :key="name" class="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              class="checkbox checkbox-sm"
              data-testid="journal-tag-add"
              :checked="add.includes(name)"
              @change="toggle('add', name, ($event.target as HTMLInputElement).checked)"
            />
            {{ name }}
          </label>
        </fieldset>

        <fieldset class="fieldset mt-3">
          <legend class="fieldset-legend">{{ t('journal.tags.remove') }}</legend>
          <p v-if="present.length === 0" key="nothing" class="text-sm">
            {{ t('journal.tags.noneToRemove') }}
          </p>
          <label v-for="item in present" :key="item.name" class="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              class="checkbox checkbox-sm"
              data-testid="journal-tag-remove"
              :checked="remove.includes(item.name)"
              @change="toggle('remove', item.name, ($event.target as HTMLInputElement).checked)"
            />
            {{ item.name }}
            <span>
              {{ t('journal.tags.of', { n: item.rows, total: count }) }}
            </span>
          </label>
        </fieldset>

        <div class="modal-action">
          <button type="button" class="btn btn-sm" data-testid="journal-tag-cancel" @click="hide">
            {{ t('journal.tags.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-primary"
            data-testid="journal-tag-apply"
            :disabled="!ready || busy"
            @click="apply"
          >
            {{ t('journal.tags.apply') }}
          </button>
        </div>
      </div>
      <div class="modal-backdrop">
        <button
          type="button"
          tabindex="-1"
          :aria-label="t('journal.tags.cancel')"
          @click="hide"
        ></button>
      </div>
    </dialog>
  </div>
</template>
