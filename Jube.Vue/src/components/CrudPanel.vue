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
import { computed, onBeforeUnmount, provide, ref, toRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import {
  CircleAlert,
  FilePlus,
  Info,
  FilePenLine,
  List,
  Lock,
  Plus,
  Save,
  Search,
  ShieldCheck,
  Sheet,
  Trash2,
  X,
} from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { meQueryOptions } from '../auth/me'
import { onServiceChange } from '../events/appEvents'
import { changedFieldsKey } from '../formModel/changedKey'
import { editorValidityKey } from '../formModel/editorValidityKey'
import { recordName, type OpenedRecord } from '../formModel/hierarchy'
import { extraFor } from '../formModel/recordExtras'
import { useFormat } from '../composables/useFormat'
import { useDirtyGuard } from '../composables/useDirtyGuard'
import { formModelApi } from '../formModel/api'
import { useRecordForm } from '../formModel/useRecordForm'
import { useToasts } from '../stores/toasts'
import { valueConfigOf } from '../valueGrid/model'
import ApprovalPanel from './ApprovalPanel.vue'
import PanelCard from './PanelCard.vue'
import RoleAllocation from './RoleAllocation.vue'
import type { FormOperation } from '../formModel/types'
import ProblemAlert from './ProblemAlert.vue'
import SchemaForm from './SchemaForm.vue'
import SchemaList from './SchemaList.vue'
import ValueGrid from './ValueGrid.vue'

const props = defineProps<{
  area: string
  parentId?: number | string
  listRoute?: string
  initialId?: number
  startNew?: boolean
  formOnly?: boolean
  externalNew?: boolean
}>()

const emit = defineEmits<{
  saved: [record: Record<string, unknown>]
  deleted: [id: number]
  opened: [record: OpenedRecord]
  closed: []
  dirty: [dirty: boolean]
  create: []
}>()

const { t } = useI18n()
const { dateTime } = useFormat()
const toasts = useToasts()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel(props.area)),
  queryFn: () => formModelApi.model(props.area),
  retry: false,
})

const modelValue = computed(() => model.data.value)

const records = useQuery({
  queryKey: computed(() => queryKeys.formRecords(props.area, props.parentId)),
  queryFn: () => formModelApi.records(modelValue.value!, props.parentId, props.listRoute),
  enabled: computed(() => modelValue.value !== undefined),
  retry: false,
})

const form = useRecordForm({
  model: modelValue as never,
  parentId: toRef(() => props.parentId),
  refused: () => t('crud.refused'),
  numberProblem: (label) => t('crud.notNumber', { field: label }),
  onSaved: (record, created) => {
    toasts.success(created ? t('crud.created') : t('crud.saved'))
    emit('saved', record)
  },
  onDeleted: (id) => {
    toasts.success(t('crud.deleted'))
    emit('deleted', id)
  },
  onFailure: (error) => toasts.apiError(error),
})

useDirtyGuard(form.dirty, () => t('crud.leave'))
watch(form.dirty, (dirty) => emit('dirty', dirty))

function onNew(): void {
  if (props.externalNew) emit('create')
  else form.startNew()
}

let openedOnStart = false
let startedNew = false

watch(
  () => [modelValue.value, records.isSuccess.value] as const,
  ([model, ready]) => {
    if (!model || !ready || startedNew || !props.startNew || !model.operations.includes('create'))
      return

    startedNew = true
    form.startNew()
  },
  { immediate: true },
)

watch(
  () => records.isSuccess.value,
  (ready) => {
    if (!ready || openedOnStart || props.initialId === undefined) return

    openedOnStart = true
    void form.open(props.initialId)
  },
  { immediate: true },
)

watch(
  () => [form.mode.value, form.id.value] as const,
  ([mode, id]) => {
    const model = modelValue.value

    if (mode === 'edit' && id !== undefined && model) {
      const values = form.values.value
      const guid = values.guid

      emit('opened', {
        id,
        guid: typeof guid === 'string' ? guid : undefined,
        name: recordName(model, values, id),
        values,
      })
    } else if (mode === 'idle') {
      emit('closed')
    }
  },
)

const me = useQuery(meQueryOptions)

provide(editorValidityKey, (editor, valid) => form.reportEditor(editor, valid))

provide(
  changedFieldsKey,
  computed(() => form.changed.value),
)

const stopListening = onServiceChange((change) => {
  if (change.area !== props.area) return
  if (change.actor !== '' && change.actor === me.data.value?.userName) return

  void form.receive(change)
})

onBeforeUnmount(stopListening)

const externalTitle = computed(() => {
  const external = form.external.value

  if (!external) return ''

  const actor = external.actor || t('crud.someone')

  if (external.kind === 'deleted') return t('crud.externalDeleted', { actor })
  if (external.kind === 'conflict') return t('crud.externalConflict')
  if (external.pending) return t('crud.externalPending', { actor })

  return external.at
    ? t('crud.externalUpdated', { actor, when: dateTime(external.at) })
    : t('crud.externalUpdatedNoTime', { actor })
})

const externalDetail = computed(() => {
  const external = form.external.value

  if (!external || external.pending || external.kind !== 'updated') return ''

  const versions =
    external.from !== null && external.to !== null
      ? `${t('crud.externalVersions', { from: external.from, to: external.to })} `
      : ''

  return `${versions}${t('crud.changedCount', { count: external.count }, external.count)}`
})

const dialog = ref<HTMLDialogElement>()
const confirming = ref(false)
const search = ref('')
const visible = ref<number>()

const extra = computed(() => extraFor(props.area))
const foldOpen = ref(false)
const folded = computed(
  () => form.mode.value === 'edit' && extra.value?.folds?.(form.values.value) === true,
)
const foldSummary = computed(() => extra.value?.summary?.(form.values.value, t) ?? '')

watch(
  () => form.id.value,
  () => {
    foldOpen.value = false
  },
)

const can = (operation: FormOperation): boolean =>
  modelValue.value?.operations.includes(operation) ?? false

const recordGuid = computed(() => {
  const guid = form.values.value.guid

  return typeof guid === 'string' && guid !== '' ? guid : undefined
})

watch(
  () => [props.area, props.parentId],
  () => {
    form.close()
    search.value = ''
    visible.value = undefined
  },
)

watch(confirming, (open) => {
  if (open) dialog.value?.showModal()
  else dialog.value?.close()
})

const problems = computed(() =>
  Object.entries(form.fieldErrors.value).flatMap(([name, messages]) => {
    const label = modelValue.value?.fields.find((field) => field.name === name)?.label ?? name

    return messages.map((message) => ({ name, label, message }))
  }),
)

function focusField(name: string): void {
  document.getElementById(name)?.focus()
}

const heading = computed(() => {
  if (form.mode.value === 'new') return t('crud.newTitle', { area: props.area })

  return t('crud.editTitle', { area: props.area })
})

function confirmDelete(): void {
  confirming.value = false
  form.remove()
}
</script>

<template>
  <section class="flex min-h-0 flex-1 flex-col gap-4" data-testid="crud-panel">
    <p
      v-if="model.isError.value"
      role="alert"
      class="alert alert-error"
      data-testid="crud-model-error"
    >
      {{ t('crud.modelFailed', { area }) }}
    </p>
    <div
      v-else-if="model.isPending.value"
      role="status"
      class="flex flex-col gap-3"
      data-testid="crud-loading"
    >
      <span class="sr-only">{{ t('crud.loading') }}</span>
      <div class="skeleton h-12 w-full"></div>
      <div class="skeleton h-40 w-full"></div>
    </div>

    <template v-else-if="modelValue">
      <PanelCard v-if="!formOnly" :title="t('crud.list', { area })" flush data-testid="crud-list">
        <template #icon>
          <List class="size-4" />
        </template>
        <template #actions>
          <span
            class="badge badge-primary badge-sm font-mono tabular-nums"
            data-testid="crud-count"
            >{{ visible ?? records.data.value?.length ?? 0 }}</span
          >
          <label class="input input-sm ms-auto w-56">
            <Search aria-hidden="true" class="size-4 shrink-0 opacity-70" />
            <input
              v-model="search"
              type="search"
              class="grow"
              :aria-label="t('crud.search')"
              :placeholder="t('crud.searchPlaceholder')"
              data-testid="crud-search"
            />
          </label>
          <button
            v-if="can('create')"
            type="button"
            class="btn btn-sm btn-primary"
            :disabled="form.mode.value !== 'idle'"
            data-testid="crud-new"
            @click="onNew"
          >
            <Plus aria-hidden="true" class="size-4" />
            {{ t('crud.new') }}
          </button>
        </template>

        <p v-if="!can('list')" class="p-3 text-sm" data-testid="crud-no-list">
          {{ t('crud.noList') }}
        </p>
        <p v-else-if="records.isError.value" role="alert" class="alert alert-error">
          {{ t('crud.recordsFailed') }}
        </p>
        <SchemaList
          v-else-if="records.isSuccess.value"
          :can-open="can('read')"
          :model="modelValue"
          :rows="records.data.value ?? []"
          :filter-text="search"
          @open="(id) => form.open(id)"
          @visible="(count) => (visible = count)"
        />
      </PanelCard>
      <div
        v-else-if="form.mode.value === 'idle'"
        class="skeleton h-40 w-full"
        role="status"
        :aria-label="t('crud.loading')"
      ></div>

      <section
        v-if="form.mode.value !== 'idle'"
        class="card card-border border-base-300 bg-base-100"
        :aria-label="heading"
        data-testid="crud-form"
      >
        <header
          class="flex flex-wrap items-center gap-3 rounded-t-box border-b-2 border-b-accent bg-primary px-5 py-3 text-primary-content"
        >
          <FilePlus v-if="form.mode.value === 'new'" key="new" aria-hidden="true" class="size-5" />
          <FilePenLine v-else key="edit" aria-hidden="true" class="size-5" />
          <h3 class="text-base font-semibold">{{ heading }}</h3>
          <span
            v-if="form.id.value !== undefined"
            class="badge badge-sm font-mono tabular-nums"
            data-testid="crud-id"
            >{{ `#${form.id.value}` }}</span
          >
          <span v-else class="badge badge-accent badge-sm">{{ t('crud.new') }}</span>
          <span
            v-if="form.locked.value"
            class="badge badge-warning badge-sm"
            data-testid="crud-locked"
          >
            <Lock aria-hidden="true" class="size-3" />
            {{ t('crud.locked') }}
          </span>
        </header>

        <div class="flex flex-col gap-4 p-5">
          <ProblemAlert
            v-if="form.reasons.value.length > 0"
            :reasons="form.reasons.value"
            data-testid="crud-reasons"
          />

          <div
            v-if="form.external.value"
            role="status"
            class="alert alert-info items-start"
            data-testid="crud-external"
          >
            <Info aria-hidden="true" class="size-5 shrink-0" />
            <div class="flex flex-col gap-0.5">
              <p class="font-semibold">{{ externalTitle }}</p>
              <p v-if="externalDetail" class="text-sm">{{ externalDetail }}</p>
            </div>
            <div class="flex flex-wrap gap-2">
              <button
                v-if="form.external.value.pending"
                type="button"
                class="btn btn-sm"
                data-testid="crud-external-reload"
                @click="form.acceptExternal()"
              >
                {{ t('crud.reload') }}
              </button>
              <button
                v-if="form.external.value.pending"
                type="button"
                class="btn btn-sm btn-ghost"
                key="keep"
                data-testid="crud-external-keep"
                @click="form.dismissExternal()"
              >
                {{ t('crud.keepEditing') }}
              </button>
              <button
                v-else-if="form.external.value.kind === 'deleted'"
                type="button"
                class="btn btn-sm"
                key="close"
                data-testid="crud-external-close"
                @click="form.close()"
              >
                {{ t('crud.close') }}
              </button>
              <button
                v-else
                type="button"
                class="btn btn-sm btn-ghost"
                key="dismiss"
                data-testid="crud-external-dismiss"
                @click="form.dismissExternal()"
              >
                {{ t('crud.dismiss') }}
              </button>
            </div>
          </div>

          <div
            v-if="problems.length > 0"
            role="alert"
            class="alert alert-error items-start"
            data-testid="crud-problems"
          >
            <CircleAlert aria-hidden="true" class="size-5 shrink-0" />
            <div class="flex flex-col gap-1">
              <p class="font-semibold">
                {{ t('crud.problems', { count: problems.length }, problems.length) }}
              </p>
              <ul class="flex flex-col gap-0.5 text-sm">
                <li v-for="problem in problems" :key="`${problem.name}:${problem.message}`">
                  <button type="button" class="link text-start" @click="focusField(problem.name)">
                    {{ problem.label }}: {{ problem.message }}
                  </button>
                </li>
              </ul>
            </div>
          </div>

          <details
            v-if="folded"
            key="folded"
            class="collapse collapse-arrow rounded-box border border-base-300 bg-base-100"
            :open="foldOpen"
            data-testid="crud-form-fold"
            @toggle="foldOpen = ($event.target as HTMLDetailsElement).open"
          >
            <summary class="collapse-title flex flex-wrap items-baseline gap-x-3 gap-y-1 py-3">
              <span class="text-sm font-semibold text-primary">{{ t('crud.fold.title') }}</span>
              <span class="text-xs font-normal text-secondary" data-testid="crud-form-summary">{{
                foldSummary
              }}</span>
            </summary>
            <div class="collapse-content">
              <SchemaForm
                :model="modelValue"
                :values="form.values.value"
                :errors="form.fieldErrors.value"
                :parent-id="parentId"
                :locked="form.locked.value"
                :disabled="form.saving.value || (form.mode.value === 'edit' && !can('update'))"
                @change="(name, value) => form.setValue(name, value)"
              />
            </div>
          </details>
          <SchemaForm
            v-else
            key="open"
            :model="modelValue"
            :values="form.values.value"
            :errors="form.fieldErrors.value"
            :parent-id="parentId"
            :locked="form.locked.value"
            :disabled="form.saving.value || (form.mode.value === 'edit' && !can('update'))"
            @change="(name, value) => form.setValue(name, value)"
          />

          <ApprovalPanel
            v-if="form.mode.value === 'edit' && modelValue.approval && form.id.value !== undefined"
            :key="`${modelValue.endpoint}:${form.id.value}`"
            :endpoint="modelValue.endpoint"
            :approval="modelValue.approval"
            :entity-id="form.id.value"
            @changed="records.refetch()"
          />

          <component
            :is="extra.component"
            v-if="extra && form.mode.value === 'edit' && form.id.value !== undefined"
            :key="`${area}:${form.id.value}`"
            v-bind="extra.propsOf(form.values.value)"
            data-testid="crud-extra"
          />

          <PanelCard
            v-if="form.mode.value === 'edit' && modelValue.roles && recordGuid"
            :title="t('crud.roles')"
            data-testid="crud-roles"
          >
            <template #icon>
              <ShieldCheck class="size-4" />
            </template>
            <RoleAllocation
              :key="recordGuid"
              :base-endpoint="modelValue.roles.endpoint"
              :parent-key-field="modelValue.roles.parentKey"
              :guid="recordGuid"
            />
          </PanelCard>

          <PanelCard
            v-if="form.mode.value === 'edit' && modelValue.values && form.id.value !== undefined"
            :title="t('crud.values')"
            data-testid="crud-values"
          >
            <template #icon>
              <Sheet class="size-4" />
            </template>
            <ValueGrid
              :key="`${modelValue.values.area}:${form.id.value}`"
              :config="valueConfigOf(modelValue.values)"
              :parent-id="form.id.value"
            />
          </PanelCard>
        </div>

        <footer
          class="sticky bottom-0 z-10 flex flex-wrap items-center gap-2 rounded-b-box border-t border-base-300 bg-base-100 px-5 py-3"
        >
          <span
            v-if="form.dirty.value"
            key="dirty"
            class="flex items-center gap-2 text-sm font-medium"
            data-testid="crud-dirty"
          >
            <span class="status status-warning" aria-hidden="true"></span>
            {{ t('crud.unsaved') }}
          </span>
          <span
            v-if="form.incomplete.value.length > 0"
            key="incomplete"
            class="flex items-center gap-2 text-sm font-medium"
            data-testid="crud-incomplete"
          >
            <span class="status status-error" aria-hidden="true"></span>
            {{ t('crud.incomplete') }}
          </span>
          <span
            v-else-if="form.mode.value === 'edit'"
            key="clean"
            class="flex items-center gap-2 text-sm text-secondary"
          >
            <span class="status status-success" aria-hidden="true"></span>
            {{ t('crud.upToDate') }}
          </span>
          <div class="ms-auto flex flex-wrap items-center gap-2">
            <button
              v-if="form.mode.value === 'edit' && !form.locked.value && can('delete')"
              type="button"
              class="btn btn-sm btn-error btn-outline"
              :disabled="form.saving.value"
              data-testid="crud-delete"
              @click="confirming = true"
            >
              <Trash2 aria-hidden="true" class="size-4" />
              {{ t('crud.delete') }}
            </button>
            <button
              type="button"
              class="btn btn-sm btn-ghost"
              :disabled="form.saving.value"
              data-testid="crud-close"
              @click="form.close()"
            >
              <X aria-hidden="true" class="size-4" />
              {{ t('crud.close') }}
            </button>
            <button
              v-if="
                !form.locked.value && (form.mode.value === 'new' ? can('create') : can('update'))
              "
              type="button"
              class="btn btn-sm btn-primary"
              :disabled="form.saving.value || !form.dirty.value || form.incomplete.value.length > 0"
              data-testid="crud-save"
              @click="form.save()"
            >
              <Save aria-hidden="true" class="size-4" />
              {{ form.mode.value === 'new' ? t('crud.create') : t('crud.save') }}
            </button>
          </div>
        </footer>
      </section>
    </template>

    <dialog ref="dialog" class="modal" data-testid="crud-confirm" @close="confirming = false">
      <div class="modal-box">
        <h3 class="text-lg font-bold">{{ t('crud.confirmTitle') }}</h3>
        <p class="py-4">{{ t('crud.confirmBody') }}</p>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="crud-confirm-no"
            @click="confirming = false"
          >
            {{ t('crud.close') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-error"
            data-testid="crud-confirm-yes"
            @click="confirmDelete"
          >
            {{ t('crud.delete') }}
          </button>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button type="submit">{{ t('crud.close') }}</button>
      </form>
    </dialog>
  </section>
</template>
