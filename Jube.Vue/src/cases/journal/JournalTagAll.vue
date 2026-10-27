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
import { computed, ref, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { Tags } from 'lucide-vue-next'
import { useFormat } from '../../composables/useFormat'
import type { TagJobPreview } from './types'

const props = defineProps<{
  preview: TagJobPreview | null
  pending: boolean
  failed: boolean
  options: string[]
  busy: boolean
  disabled: boolean
  filtered?: boolean
}>()

const emit = defineEmits<{
  open: []
  close: []
  apply: [change: { add: string[]; remove: string[] }]
}>()

const { t } = useI18n()
const { number } = useFormat()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const add = ref<string[]>([])
const remove = ref<string[]>([])

const ready = computed(
  () =>
    (add.value.length > 0 || remove.value.length > 0) &&
    props.preview !== null &&
    props.preview.matching > 0 &&
    !props.preview.tooMany,
)

function show(): void {
  add.value = []
  remove.value = []
  emit('open')
  dialog.value?.showModal()
}

function hide(): void {
  dialog.value?.close?.()
  emit('close')
}

function toggle(list: 'add' | 'remove', name: string, on: boolean): void {
  const own = list === 'add' ? add : remove
  const other = list === 'add' ? remove : add

  own.value = on
    ? [...own.value.filter((item) => item !== name), name]
    : own.value.filter((item) => item !== name)

  if (on) other.value = other.value.filter((item) => item !== name)
}

function apply(): void {
  emit('apply', { add: [...add.value], remove: [...remove.value] })
  hide()
}
</script>

<template>
  <div class="contents">
    <button
      type="button"
      class="btn btn-ghost btn-sm"
      data-testid="journal-tag-all-open"
      :disabled="disabled || busy"
      @click="show"
    >
      <Tags class="size-4" aria-hidden="true" />
      {{ t('journal.tags.all') }}
    </button>

    <dialog ref="dialog" class="modal" data-testid="journal-tag-all-dialog" @cancel.prevent="hide">
      <div class="modal-box">
        <h3 class="text-lg font-bold">{{ t('journal.tags.allTitle') }}</h3>

        <p v-if="pending" key="counting" class="mt-3 text-sm" role="status">
          {{ t('journal.tags.allCounting') }}
        </p>
        <p v-else-if="failed" key="failed" class="mt-3 text-sm" role="alert">
          {{ t('journal.tags.allFailed') }}
        </p>
        <template v-else-if="preview">
          <p class="mt-3 text-sm font-semibold" data-testid="journal-tag-all-count">
            {{
              preview.tooMany
                ? t('journal.tags.allTooMany', { n: number(preview.limit) })
                : preview.matching === 0
                  ? t('journal.tags.allNone')
                  : t('journal.tags.allMatching', { n: number(preview.matching) })
            }}
          </p>
          <p
            v-if="filtered"
            key="filtered"
            class="mt-1 text-sm"
            data-testid="journal-tag-all-filtered"
          >
            {{ t('journal.tags.allFiltered') }}
          </p>
          <p class="mt-1 text-xs text-base-content/70">{{ t('journal.tags.allNote') }}</p>
        </template>

        <fieldset class="fieldset mt-3">
          <legend class="fieldset-legend">{{ t('journal.tags.add') }}</legend>
          <p v-if="options.length === 0" key="none" class="text-sm">
            {{ t('journal.tags.noOptions') }}
          </p>
          <label v-for="name in options" :key="`a${name}`" class="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              class="checkbox checkbox-sm"
              data-testid="journal-tag-all-add"
              :checked="add.includes(name)"
              @change="toggle('add', name, ($event.target as HTMLInputElement).checked)"
            />
            {{ name }}
          </label>
        </fieldset>

        <fieldset class="fieldset mt-3">
          <legend class="fieldset-legend">{{ t('journal.tags.remove') }}</legend>
          <label v-for="name in options" :key="`r${name}`" class="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              class="checkbox checkbox-sm"
              data-testid="journal-tag-all-remove"
              :checked="remove.includes(name)"
              @change="toggle('remove', name, ($event.target as HTMLInputElement).checked)"
            />
            {{ name }}
          </label>
        </fieldset>

        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="journal-tag-all-cancel"
            @click="hide"
          >
            {{ t('journal.tags.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-primary"
            data-testid="journal-tag-all-apply"
            :disabled="!ready || busy"
            @click="apply"
          >
            {{ t('journal.tags.allApply', { n: number(preview?.matching ?? 0) }) }}
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
