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
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { FolderOpen, Save, Trash2 } from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import { useFormat } from '../../composables/useFormat'
import { useToasts } from '../../stores/toasts'
import { journalApi } from './api'
import { serialiseFilter } from './filter'
import { mergeText, viewPatch } from './saved'
import {
  defaultJournalState,
  defaultLimitKmh,
  defaultMinimumKilometres,
  type JournalState,
} from './state'
import type { SavedView } from './types'

const props = defineProps<{ caseId: number; state: JournalState }>()

const emit = defineEmits<{ load: [patch: Partial<JournalState>] }>()

const { t } = useI18n()
const { number } = useFormat()
const toasts = useToasts()
const queryClient = useQueryClient()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const viewName = ref('')

const saved = useQuery({
  queryKey: computed(() => ['service', 'Case', 'keyjournal', 'replays', props.caseId] as const),
  queryFn: () => journalApi.replays(props.caseId),
  staleTime: 30_000,
  retry: false,
})

const saving = useMutation({
  mutationFn: (name: string) =>
    journalApi.saveReplay({
      caseId: props.caseId,
      name,
      days: props.state.days,
      merge: mergeText(props.state.merge),
      filter: serialiseFilter(props.state.filter),
      limitKmh: props.state.speed === defaultLimitKmh ? undefined : props.state.speed,
      minimumKilometres:
        props.state.distance === defaultMinimumKilometres ? undefined : props.state.distance,
      impossibleOnly: props.state.improbable ? true : undefined,
      showAll: props.state.all ? true : undefined,
      lens: props.state.lens === defaultJournalState.lens ? undefined : props.state.lens,
    }),
  onSuccess: () => {
    toasts.success(t('journal.views.done'))
    viewName.value = ''
    dialog.value?.close?.()
    void queryClient.invalidateQueries({ queryKey: ['service', 'Case', 'keyjournal', 'replays'] })
  },
  onError: (error) => toasts.apiError(error),
})

const deleting = useMutation({
  mutationFn: (guid: string) => journalApi.deleteReplay(guid),
  onSuccess: () => {
    toasts.success(t('journal.views.removed'))
    void queryClient.invalidateQueries({ queryKey: ['service', 'Case', 'keyjournal', 'replays'] })
  },
  onError: (error) => toasts.apiError(error),
})

function load(view: SavedView): void {
  emit('load', viewPatch(view))
  document.getElementById('journal-views')?.hidePopover?.()
}

function openSave(): void {
  viewName.value = ''
  dialog.value?.showModal()
}

function closeSave(): void {
  dialog.value?.close?.()
}

function submitSave(): void {
  const name = viewName.value.trim()

  if (name !== '') saving.mutate(name)
}
</script>

<template>
  <div class="flex items-center gap-1" data-testid="journal-views-bar">
    <button type="button" class="btn btn-sm" data-testid="journal-views-save" @click="openSave">
      <Save class="size-4" aria-hidden="true" />
      {{ t('journal.views.save') }}
    </button>
    <button
      type="button"
      class="btn btn-sm"
      popovertarget="journal-views"
      style="anchor-name: --journal-views"
      data-testid="journal-views-open"
    >
      <FolderOpen class="size-4" aria-hidden="true" />
      {{ t('journal.views.label') }}
      <span class="badge badge-soft badge-sm font-mono tabular-nums">{{
        number(saved.data.value?.replays.length ?? 0)
      }}</span>
    </button>
    <FieldHelp :label="t('journal.views.label')" :text="t('journal.views.help')" />
    <div
      id="journal-views"
      class="dropdown dropdown-end card card-sm z-1 max-h-96 w-80 overflow-y-auto bg-base-100 shadow-md"
      popover
      style="position-anchor: --journal-views"
      data-testid="journal-views"
    >
      <div class="card-body gap-2">
        <p
          v-if="(saved.data.value?.replays.length ?? 0) === 0"
          key="none"
          class="text-sm"
          data-testid="journal-views-none"
        >
          {{ t('journal.views.none') }}
        </p>
        <ul v-else key="list" class="flex flex-col gap-2">
          <li
            v-for="view in saved.data.value?.replays ?? []"
            :key="view.guid"
            class="flex items-center justify-between gap-2"
            data-testid="journal-views-item"
          >
            <span class="flex min-w-0 flex-col">
              <span class="truncate text-sm font-semibold">{{ view.name }}</span>
              <span class="text-xs">{{ t('journal.window.days', { n: view.days }) }}</span>
            </span>
            <span class="flex gap-1">
              <button
                type="button"
                class="btn btn-sm"
                :aria-label="t('journal.views.open', { name: view.name })"
                data-testid="journal-views-load"
                @click="load(view)"
              >
                {{ t('journal.views.load') }}
              </button>
              <button
                type="button"
                class="btn btn-ghost btn-sm btn-square"
                :aria-label="t('journal.views.delete', { name: view.name })"
                :disabled="deleting.isPending.value"
                data-testid="journal-views-delete"
                @click="deleting.mutate(view.guid)"
              >
                <Trash2 class="size-4" aria-hidden="true" />
              </button>
            </span>
          </li>
        </ul>
      </div>
    </div>

    <dialog
      ref="dialog"
      class="modal"
      data-testid="journal-views-dialog"
      @cancel.prevent="closeSave"
    >
      <form class="modal-box" @submit.prevent="submitSave">
        <h3 class="text-lg font-bold">{{ t('journal.views.title') }}</h3>
        <p class="mt-1 text-sm">{{ t('journal.views.hint') }}</p>
        <label class="mt-3 flex flex-col gap-1">
          <span class="text-xs">{{ t('journal.views.name') }}</span>
          <input
            v-model="viewName"
            type="text"
            maxlength="100"
            class="input input-sm w-full"
            data-testid="journal-views-name"
          />
        </label>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="journal-views-cancel"
            @click="closeSave"
          >
            {{ t('journal.views.cancel') }}
          </button>
          <button
            type="submit"
            class="btn btn-sm btn-primary"
            :disabled="viewName.trim() === '' || saving.isPending.value"
            data-testid="journal-views-apply"
          >
            {{ t('journal.views.save') }}
          </button>
        </div>
      </form>
      <div class="modal-backdrop">
        <button
          type="button"
          tabindex="-1"
          :aria-label="t('journal.views.cancel')"
          @click="closeSave"
        ></button>
      </div>
    </dialog>
  </div>
</template>
