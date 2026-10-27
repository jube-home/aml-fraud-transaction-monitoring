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
import { computed, inject, onBeforeUnmount } from 'vue'
import { exhaustiveSourceKey } from '../filterBuilder/api'
import { editorValidityKey } from '../formModel/editorValidityKey'
import {
  caseFilterChanges,
  caseFilterInitial,
  datasetChanges,
  datasourceChanges,
  datasourceInitial,
  datasetLocked,
  datasetReference,
  datasetShown,
  exhaustiveFilterChanges,
  exhaustiveFilterInitial,
  exhaustiveSource,
  ruleEditorChanges,
  ruleEditorInitial,
} from '../formModel/editors'
import type { FormEditor, FormModel, RecordValues } from '../formModel/types'
import type { BuilderCoderValue } from '../ruleBuilder/types'
import BuilderCoder from './BuilderCoder.vue'
import CaseSqlEditor from './CaseSqlEditor.vue'
import CaseFilterBuilder, { type CaseFilterValue } from './CaseFilterBuilder.vue'
import DatasetUpload from './DatasetUpload.vue'
import DatasourceBuilder from '../datasource/DatasourceBuilder.vue'
import type { DatasourceValue } from '../datasource/types'
import ExhaustiveFilterBuilder, { type ExhaustiveFilterValue } from './ExhaustiveFilterBuilder.vue'

const props = defineProps<{
  model: FormModel
  editor: FormEditor
  values: RecordValues
  parentId?: number | string
  locked?: boolean
  disabled?: boolean
}>()

const emit = defineEmits<{ change: [name: string, value: unknown] }>()

const recordKey = computed(() => String(props.values[props.model.keys.id] ?? 'new'))
const parentValue = computed(() => {
  const key = props.model.keys.parent

  return Number((key ? props.values[key] : undefined) ?? props.parentId)
})
const frozen = computed(() => props.locked === true || props.disabled === true)

const ruleInitial = computed(() => ruleEditorInitial(props.editor, props.values))
const filterInitial = computed(() => caseFilterInitial(props.editor, props.values))

const shown = computed(() =>
  props.editor.kind === 'exhaustiveDataset' ? datasetShown(props.editor, props.values) : true,
)
const datasetValue = computed(() => datasetReference(props.editor, props.values))
const datasetFrozen = computed(() => frozen.value || datasetLocked(props.editor, props.values))
const filterSource = computed(() => exhaustiveSource(props.editor, props.values, parentValue.value))
const datasourceStart = computed(() =>
  props.editor.kind === 'datasource' ? datasourceInitial(props.editor, props.values) : undefined,
)
const exhaustiveInitial = computed(() => exhaustiveFilterInitial(props.editor, props.values))

function write(changes: [string, unknown][]): void {
  for (const [name, value] of changes) {
    if (String(props.values[name] ?? '') !== String(value ?? '')) emit('change', name, value)
  }
}

const sqlValue = computed(() => String(props.values[props.editor.roles.sql ?? ''] ?? ''))

function sqlChanged(value: string): void {
  write([[props.editor.roles.sql ?? '', value]])
}

function ruleChanged(value: BuilderCoderValue): void {
  write(ruleEditorChanges(props.editor, value))
}

function datasetPicked(picked: string): void {
  write(datasetChanges(props.editor, picked))
}

function exhaustiveFilterChanged(value: ExhaustiveFilterValue): void {
  write(exhaustiveFilterChanges(props.editor, value))
}

const reportValidity = inject(editorValidityKey, undefined)

onBeforeUnmount(() => reportValidity?.(props.editor.kind, true))

function datasourceChanged(value: DatasourceValue): void {
  reportValidity?.(props.editor.kind, value.valid)

  for (const [name, next] of datasourceChanges(props.editor, value, props.values))
    emit('change', name, next)
}

function filterChanged(value: CaseFilterValue): void {
  write(caseFilterChanges(props.editor, value))
}
</script>

<template>
  <div v-if="shown" class="sm:col-span-2" :inert="frozen" :data-testid="`editor-${editor.kind}`">
    <BuilderCoder
      v-if="editor.kind === 'ruleBuilder' && editor.parseType !== null"
      :key="`rule:${recordKey}`"
      :parse-type="editor.parseType"
      :entity-analysis-model-id="parentValue"
      :initial-value="ruleInitial"
      @change="ruleChanged"
    />
    <CaseFilterBuilder
      v-else-if="editor.kind === 'caseFilter'"
      :key="`case:${recordKey}`"
      :case-workflow="{ kind: 'id', value: parentValue }"
      :initial-value="filterInitial"
      @change="filterChanged"
    />
    <DatasetUpload
      v-else-if="editor.kind === 'exhaustiveDataset'"
      :key="`dataset:${recordKey}`"
      :model-value="datasetValue"
      :locked="datasetFrozen"
      @update:model-value="datasetPicked"
    />
    <DatasourceBuilder
      v-else-if="editor.kind === 'datasource' && parentValue > 0"
      :key="`datasource:${recordKey}`"
      :registry-id="parentValue"
      :initial-value="datasourceStart"
      :locked="frozen"
      @change="datasourceChanged"
    />
    <CaseSqlEditor
      v-else-if="editor.kind === 'caseSql'"
      :key="`case-sql:${recordKey}`"
      :model-value="sqlValue"
      :locked="frozen"
      @update:model-value="sqlChanged"
    />
    <ExhaustiveFilterBuilder
      v-else-if="editor.kind === 'exhaustiveFilter'"
      :key="`exhaustive:${recordKey}:${exhaustiveSourceKey(filterSource)}`"
      :source="filterSource"
      :initial-value="exhaustiveInitial"
      @change="exhaustiveFilterChanged"
    />
  </div>
</template>
