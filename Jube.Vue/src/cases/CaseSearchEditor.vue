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
import { useMutation, useQuery } from '@tanstack/vue-query'
import { X } from 'lucide-vue-next'
import { ApiError } from '../api/useApi'
import { roleAllocationApi } from '../roleAllocation/api'
import { useToasts } from '../stores/toasts'
import CaseFilterBuilder, { type CaseFilterValue } from '../components/CaseFilterBuilder.vue'
import ConfirmDialog from '../components/ConfirmDialog.vue'
import { searchApi, type CaseSavedSearch } from './searchApi'

const props = defineProps<{ open: boolean; workflow: string; searchGuid: string | null }>()

const emit = defineEmits<{ close: []; saved: [guid: string]; deleted: [] }>()

const { t } = useI18n()
const toasts = useToasts()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const builder = useTemplateRef<{ getValue: () => CaseFilterValue }>('builder')

const name = ref('')
const sharedRole = ref('')
const value = ref<CaseFilterValue>()
const confirming = ref(false)
const seeded = ref<CaseSavedSearch>()

const existing = useQuery({
  queryKey: computed(() => ['service', 'Case', 'search', props.searchGuid] as const),
  queryFn: () => searchApi.get(props.searchGuid!),
  enabled: computed(() => props.open && props.searchGuid !== null),
  staleTime: 0,
  gcTime: 0,
})

const roles = useQuery({
  queryKey: ['service', 'Case', 'roles'] as const,
  queryFn: roleAllocationApi.roles,
  enabled: computed(() => props.open),
  staleTime: 60_000,
})

const ready = computed(() => props.searchGuid === null || seeded.value !== undefined)

const canSave = computed(
  () => name.value.trim() !== '' && value.value?.valid === true && !saving.value,
)

watch(
  () => props.open,
  (open) => {
    if (open) {
      name.value = ''
      sharedRole.value = ''
      value.value = undefined
      seeded.value = undefined
      dialog.value?.showModal()
    } else dialog.value?.close?.()
  },
  { flush: 'post' },
)

watch(
  () => existing.data.value,
  (found) => {
    if (!found || seeded.value) return

    seeded.value = found
    name.value = found.name ?? ''
    sharedRole.value = found.sharedRoleRegistryGuid ?? ''
  },
)

const save = useMutation({
  mutationFn: () => {
    const current = builder.value?.getValue() ?? value.value

    if (!current?.valid) throw new Error(t('cases.editor.invalid'))

    const body = {
      caseWorkflowGuid: props.workflow,
      name: name.value.trim(),
      filterJson: current.filterJson,
      selectJson: current.selectJson,
      sharedRoleRegistryGuid: sharedRole.value === '' ? null : sharedRole.value,
    }

    return seeded.value
      ? searchApi.update({ ...body, guid: seeded.value.guid, version: seeded.value.version })
      : searchApi.create(body)
  },
  onSuccess: (saved) => {
    toasts.success(t('cases.editor.saved'))
    emit('saved', saved.guid)
  },
  onError: (error) =>
    error instanceof ApiError ? toasts.apiError(error) : toasts.error(error.message),
})

const remove = useMutation({
  mutationFn: () => searchApi.remove(props.searchGuid!),
  onSuccess: () => {
    confirming.value = false
    toasts.success(t('cases.editor.deleted'))
    emit('deleted')
  },
  onError: (error) => {
    confirming.value = false
    toasts.apiError(error)
  },
})

const saving = computed(() => save.isPending.value)

const initial = computed(() =>
  seeded.value
    ? {
        filterJson: seeded.value.filterJson ?? undefined,
        selectJson: seeded.value.selectJson ?? undefined,
      }
    : undefined,
)
</script>

<template>
  <dialog
    ref="dialog"
    class="modal modal-end"
    data-testid="cases-editor-dialog"
    @cancel.prevent="emit('close')"
  >
    <div class="modal-box flex h-full max-h-full w-full max-w-4xl flex-col gap-4 rounded-none">
      <div class="flex items-center justify-between gap-2">
        <h2 class="text-lg font-bold">
          {{ searchGuid === null ? t('cases.editor.new') : t('cases.editor.edit') }}
        </h2>
        <button
          type="button"
          class="btn btn-ghost btn-sm btn-square"
          :aria-label="t('cases.editor.close')"
          data-testid="cases-editor-close"
          @click="emit('close')"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
      </div>

      <div v-if="open" key="body" class="flex min-h-0 grow flex-col gap-4 overflow-y-auto">
        <div
          v-if="searchGuid !== null && existing.isError.value"
          key="failed"
          role="alert"
          class="alert alert-error"
        >
          <span>{{ t('cases.editor.loadFailed') }}</span>
        </div>

        <div
          v-else-if="!ready"
          key="loading"
          class="flex flex-col gap-2"
          role="status"
          data-testid="cases-editor-loading"
        >
          <span class="sr-only">{{ t('cases.editor.loading') }}</span>
          <div class="skeleton h-10 w-full"></div>
          <div class="skeleton h-40 w-full"></div>
        </div>

        <template v-else>
          <div class="grid gap-3 sm:grid-cols-2">
            <fieldset class="fieldset">
              <label class="label" for="cases-editor-name">{{ t('cases.editor.name') }}</label>
              <input
                id="cases-editor-name"
                v-model="name"
                type="text"
                class="input input-sm w-full"
                maxlength="200"
                data-testid="cases-editor-name"
              />
            </fieldset>
            <fieldset class="fieldset">
              <label class="label" for="cases-editor-share">{{ t('cases.editor.share') }}</label>
              <select
                id="cases-editor-share"
                v-model="sharedRole"
                class="select select-sm w-full"
                data-testid="cases-editor-share"
              >
                <option value="">{{ t('cases.editor.private') }}</option>
                <option v-for="role in roles.data.value ?? []" :key="role.guid" :value="role.guid">
                  {{ role.name }}
                </option>
              </select>
            </fieldset>
          </div>

          <CaseFilterBuilder
            ref="builder"
            :case-workflow="{ kind: 'guid', value: workflow }"
            :initial-value="initial"
            @change="value = $event"
          />
        </template>
      </div>

      <div class="modal-action mt-0 justify-between">
        <button
          v-if="searchGuid !== null"
          key="delete"
          type="button"
          class="btn btn-error btn-sm"
          data-testid="cases-editor-delete"
          @click="confirming = true"
        >
          {{ t('cases.editor.delete') }}
        </button>
        <span v-else key="spacer"></span>
        <div class="flex gap-2">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="cases-editor-cancel"
            @click="emit('close')"
          >
            {{ t('cases.editor.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-primary btn-sm"
            :disabled="!canSave"
            data-testid="cases-editor-save"
            @click="save.mutate()"
          >
            {{ t('cases.editor.save') }}
          </button>
        </div>
      </div>
    </div>
    <div class="modal-backdrop">
      <button
        type="button"
        tabindex="-1"
        :aria-label="t('cases.editor.close')"
        @click="emit('close')"
      ></button>
    </div>

    <ConfirmDialog
      id="cases-editor-delete"
      :open="confirming"
      :title="t('cases.editor.deleteTitle')"
      :body="t('cases.editor.deleteBody', { name })"
      :confirm-label="t('cases.editor.delete')"
      :cancel-label="t('cases.editor.cancel')"
      @confirm="remove.mutate()"
      @cancel="confirming = false"
    />
  </dialog>
</template>
