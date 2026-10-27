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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { describeApiError } from '../api/problems'
import { queryKeys } from '../api/queryKeys'
import { useExpandHosts } from '../composables/useExpandHosts'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { datasetApi } from '../dataset/api'
import {
  canInclude,
  confirmBody,
  initialMapping,
  mappingProblems,
  parseReference,
  pickable,
  processingTypes,
  reference,
  type ColumnMapping,
  type Dataset,
  type DatasetColumn,
} from '../dataset/model'
import DataGrid from './DataGrid.vue'
import type { GridColumn, GridInputEvent } from './dataGridTypes'
import FileUpload from './FileUpload.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(
  defineProps<{ modelValue: string; locked?: boolean; allowed?: boolean }>(),
  { locked: false, allowed: true },
)

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const { t, locale } = useI18n()
const { number } = useFormat()
const queryClient = useQueryClient()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))
const opened = ref(false)
const draft = ref<Dataset | null>(null)
const problems = ref<string[]>([])
const saving = ref(false)
const dialog = ref<HTMLDialogElement>()
let mapping: ColumnMapping[] = []

const list = useQuery({
  queryKey: queryKeys.datasets(),
  queryFn: datasetApi.list,
  retry: false,
})

const options = computed(() => pickable(list.data.value ?? []))
const selected = computed(() => parseReference(props.modelValue))

const detail = useQuery({
  queryKey: computed(() =>
    queryKeys.datasetDetail(selected.value?.kind ?? 'Csv', selected.value?.id ?? 0),
  ),
  queryFn: () => datasetApi.get(selected.value!.kind, selected.value!.id),
  enabled: computed(() => selected.value !== null && draft.value === null),
  retry: false,
})

const inspected = computed(() => draft.value ?? (selected.value ? detail.data.value : undefined))
const editable = computed(() => draft.value !== null && !props.locked)
const showPanel = computed(() => opened.value || inspected.value !== undefined)

watch(
  () => props.modelValue,
  () => {
    problems.value = []

    if (draft.value) void discard()
  },
)

function fail(error: unknown, fallback: string): void {
  if (error instanceof ApiError && error.status === 413) {
    problems.value = [t('dataset.tooLarge')]
  } else if (error instanceof ApiError && error.kind === 'conflict') {
    problems.value = [t('dataset.inUse')]
  } else {
    problems.value = describeApiError(error, { failed: fallback, refused: t('dataset.refused') })
  }
}

async function upload(file: File): Promise<void> {
  problems.value = []

  const dataset = await datasetApi.upload(file)

  draft.value = dataset
  mapping = initialMapping(dataset.columns)
  opened.value = true
}

async function discard(): Promise<void> {
  const dataset = draft.value

  draft.value = null

  if (dataset && dataset.statusId === 0) {
    await datasetApi.remove(dataset.kind, dataset.id).catch(() => undefined)
  }
}

async function cancel(): Promise<void> {
  problems.value = []
  opened.value = false
  await discard()
}

function describeProblem(code: 'none' | 'name' | 'duplicate', index?: number): string {
  const header = draft.value?.columns.find((column) => column.index === index)?.header ?? ''

  if (code === 'none') return t('dataset.problem.none')

  return code === 'name'
    ? t('dataset.problem.name', { column: header })
    : t('dataset.problem.duplicate', { column: header })
}

async function confirm(): Promise<void> {
  const dataset = draft.value

  if (!dataset) return

  const found = mappingProblems(mapping)

  problems.value = found.map((problem) =>
    describeProblem(problem.code, 'index' in problem ? problem.index : undefined),
  )

  if (found.length > 0) return

  saving.value = true

  try {
    const confirmed = await datasetApi.confirm(dataset.kind, confirmBody(dataset, mapping))

    draft.value = null
    opened.value = false
    await queryClient.invalidateQueries({ queryKey: queryKeys.datasets() })
    emit('update:modelValue', reference(confirmed))
  } catch (error) {
    fail(error, t('dataset.confirmFailed'))
  } finally {
    saving.value = false
  }
}

function pick(event: Event): void {
  const value = (event.target as HTMLSelectElement).value

  opened.value = false
  emit('update:modelValue', value)
}

function startUpload(): void {
  problems.value = []
  void discard()
  opened.value = true
}

async function remove(): Promise<void> {
  const target = selected.value

  dialog.value?.close()

  if (!target) return

  try {
    await datasetApi.remove(target.kind, target.id)
    await queryClient.invalidateQueries({ queryKey: queryKeys.datasets() })
    emit('update:modelValue', '')
  } catch (error) {
    fail(error, t('dataset.deleteFailed'))
  }
}

function mappingOf(index: number): ColumnMapping | undefined {
  return mapping.find((entry) => entry.index === index)
}

function includeBox(row: DatasetColumn): Element {
  const box = document.createElement('input')

  box.type = 'checkbox'
  box.className = 'toggle toggle-sm'
  box.checked = mappingOf(row.index)?.include ?? row.include
  box.disabled = !editable.value || !canInclude(row)
  box.setAttribute('aria-label', t('dataset.includeColumn', { column: row.header ?? '' }))
  box.dataset.testid = `dataset-include-${row.index}`
  box.addEventListener('change', () => {
    const entry = mappingOf(row.index)

    if (entry) entry.include = box.checked
  })

  return box
}

function processingSelect(row: DatasetColumn): Element {
  const select = document.createElement('select')

  select.className = 'select select-xs'
  select.disabled = !editable.value
  select.setAttribute('aria-label', t('dataset.processingFor', { column: row.header ?? '' }))
  select.dataset.testid = `dataset-processing-${row.index}`

  for (const type of processingTypes) {
    const option = document.createElement('option')

    option.value = String(type.id)
    option.textContent = processingLabel(type.id)
    select.append(option)
  }

  select.value = String(mappingOf(row.index)?.processingTypeId ?? row.processingTypeId)
  select.addEventListener('change', () => {
    const entry = mappingOf(row.index)

    if (entry) entry.processingTypeId = Number(select.value)
  })

  return select
}

function processingLabel(id: number): string {
  switch (id) {
    case 1:
      return t('dataset.processing.payload')
    case 2:
      return t('dataset.processing.dictionary')
    case 3:
      return t('dataset.processing.ttlCounter')
    case 4:
      return t('dataset.processing.sanction')
    case 5:
      return t('dataset.processing.abstraction')
    default:
      return t('dataset.processing.abstractionCalculation')
  }
}

function onInput(event: GridInputEvent): void {
  const entry = mappingOf((event.row as DatasetColumn).index)

  if (entry && event.field === 'name') entry.name = event.value
}

const yes = (value: unknown) => (value ? t('dataset.yes') : t('dataset.no'))
const figure = (value: unknown) => (typeof value === 'number' ? number(value) : '')

const columns = computed<GridColumn[]>(() => [
  {
    field: 'include',
    title: t('dataset.col.include'),
    width: 90,
    draw: (row) => includeBox(row as DatasetColumn),
  },
  { field: 'header', title: t('dataset.col.column'), minWidth: 160 },
  { field: 'kind', title: t('dataset.col.type'), width: 100 },
  { field: 'isCategorical', title: t('dataset.col.categorical'), width: 120, format: yes },
  {
    field: 'missingCount',
    title: t('dataset.col.missing'),
    align: 'end',
    mono: true,
    width: 100,
    format: figure,
  },
  {
    field: 'min',
    title: t('dataset.col.min'),
    align: 'end',
    mono: true,
    width: 100,
    format: figure,
  },
  {
    field: 'max',
    title: t('dataset.col.max'),
    align: 'end',
    mono: true,
    width: 100,
    format: figure,
  },
  editable.value
    ? {
        field: 'name',
        title: t('dataset.col.name'),
        minWidth: 180,
        input: (row) => ({
          placeholder: t('dataset.col.name'),
          label: t('dataset.nameFor', { column: (row as DatasetColumn).header ?? '' }),
          value: mappingOf((row as DatasetColumn).index)?.name ?? '',
        }),
      }
    : { field: 'name', title: t('dataset.col.name'), minWidth: 180 },
  editable.value
    ? {
        field: 'processingTypeId',
        title: t('dataset.col.processing'),
        minWidth: 200,
        draw: (row) => processingSelect(row as DatasetColumn),
      }
    : {
        field: 'processingTypeId',
        title: t('dataset.col.processing'),
        minWidth: 200,
        format: (value) => processingLabel(Number(value)),
      },
])

const distinctColumns = computed<GridColumn[]>(() => [
  { field: 'value', title: t('dataset.distinct.value'), minWidth: 200 },
  {
    field: 'count',
    title: t('dataset.distinct.count'),
    align: 'end',
    mono: true,
    width: 120,
    format: figure,
  },
])

const rows = computed(() => inspected.value?.columns ?? [])

function distinctOf(id: unknown) {
  return rows.value.find((row) => row.index === id)?.distinctValues ?? []
}
</script>

<template>
  <PanelCard :title="t('dataset.title')" compact>
    <div class="flex min-w-0 flex-col gap-4" data-testid="dataset-upload">
      <p class="text-sm text-secondary">{{ t('dataset.hint') }}</p>

      <p v-if="!allowed" key="refused" class="text-sm" role="status" data-testid="dataset-refused">
        {{ t('dataset.refused') }}
      </p>

      <template v-else>
        <div class="flex flex-wrap items-end gap-3">
          <label class="flex flex-col gap-1">
            <span class="label text-xs">{{ t('dataset.pick') }}</span>
            <select
              class="select select-sm min-w-72"
              :value="modelValue"
              :disabled="locked || list.isPending.value"
              data-testid="dataset-picker"
              @change="pick"
            >
              <option value="">{{ t('dataset.none') }}</option>
              <option
                v-for="dataset in options"
                :key="reference(dataset)"
                :value="reference(dataset)"
              >
                {{ dataset.name }} ({{ dataset.kind }})
              </option>
            </select>
          </label>
          <button
            type="button"
            class="btn btn-sm"
            :disabled="locked"
            data-testid="dataset-new"
            @click="startUpload"
          >
            {{ t('dataset.uploadNew') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-error"
            :disabled="locked || selected === null"
            data-testid="dataset-delete"
            @click="dialog?.showModal()"
          >
            {{ t('dataset.delete') }}
          </button>
        </div>

        <FileUpload
          v-if="opened && draft === null && !locked"
          key="upload"
          :label="t('dataset.file')"
          :extensions="['.csv', '.json']"
          :upload="upload"
          @failed="(error) => fail(error, t('dataset.uploadFailed'))"
        />

        <ProblemAlert
          v-if="problems.length > 0"
          key="problems"
          :reasons="problems"
          data-testid="dataset-problems"
        />

        <section
          v-if="showPanel && inspected"
          key="inspection"
          class="flex min-w-0 flex-col gap-3"
          data-testid="dataset-inspection"
        >
          <p class="text-sm" data-testid="dataset-summary">
            {{
              t('dataset.summary', {
                kind: inspected.kind,
                rows: number(inspected.rowCount),
                columns: number(inspected.columnCount),
                bytes: number(inspected.sizeBytes),
              })
            }}
            <template v-if="inspected.delimiter">
              {{ t('dataset.delimiter', { delimiter: inspected.delimiter }) }}
            </template>
          </p>

          <ul
            v-if="inspected.warnings.length > 0"
            key="warnings"
            class="alert alert-warning flex-col items-start gap-1"
            data-testid="dataset-warnings"
          >
            <li v-for="warning in inspected.warnings" :key="warning">{{ warning }}</li>
          </ul>

          <p v-if="editable" key="review" class="text-sm text-secondary">
            {{ t('dataset.review') }}
          </p>

          <DataGrid
            :key="`${inspected.kind}:${inspected.id}:${editable}`"
            :columns="columns"
            :rows="rows"
            :direction="direction"
            :empty-text="t('dataset.noColumns')"
            :label="t('dataset.title')"
            row-key="index"
            expandable
            auto-height
            :expand-labels="{
              expand: t('dataset.distinct.show'),
              collapse: t('dataset.distinct.hide'),
            }"
            @input="onInput"
            @expand="onExpand"
          />
          <template v-for="[id, host] in hosts" :key="String(id)">
            <Teleport :to="host">
              <DataGrid
                :columns="distinctColumns"
                :rows="distinctOf(id)"
                :direction="direction"
                :empty-text="t('dataset.distinct.none')"
                :label="t('dataset.distinct.show')"
                auto-height
              />
            </Teleport>
          </template>

          <div v-if="editable" key="actions" class="flex gap-2">
            <button
              type="button"
              class="btn btn-sm btn-primary"
              :disabled="saving"
              data-testid="dataset-confirm"
              @click="confirm"
            >
              {{ t('dataset.confirm') }}
            </button>
            <button
              type="button"
              class="btn btn-sm"
              :disabled="saving"
              data-testid="dataset-cancel"
              @click="cancel"
            >
              {{ t('dataset.cancel') }}
            </button>
          </div>
        </section>
      </template>

      <dialog ref="dialog" class="modal" data-testid="dataset-confirm-delete">
        <div class="modal-box">
          <h3 class="text-lg font-bold">{{ t('dataset.deleteTitle') }}</h3>
          <p class="py-4">{{ t('dataset.deleteBody') }}</p>
          <div class="modal-action">
            <button
              type="button"
              class="btn btn-sm"
              data-testid="dataset-delete-no"
              @click="dialog?.close()"
            >
              {{ t('dataset.cancel') }}
            </button>
            <button
              type="button"
              class="btn btn-sm btn-error"
              data-testid="dataset-delete-yes"
              @click="remove"
            >
              {{ t('dataset.delete') }}
            </button>
          </div>
        </div>
        <form method="dialog" class="modal-backdrop">
          <button type="submit">{{ t('dataset.cancel') }}</button>
        </form>
      </dialog>
    </div>
  </PanelCard>
</template>
