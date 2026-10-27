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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { editorOf } from '../formModel/editors'
import { bodyOf, featureBody, featureTitle, type GroupBody } from '../formModel/features'
import type { FormEditor, FormField, FormModel, RecordValues } from '../formModel/types'
import EditorField from './EditorField.vue'
import FeatureSwitch from './FeatureSwitch.vue'
import PanelCard from './PanelCard.vue'
import SchemaField from './SchemaField.vue'

const props = defineProps<{
  body: GroupBody
  model: FormModel
  values: RecordValues
  errors: Record<string, string[]>
  parentId?: number | string
  locked?: boolean
  disabled?: boolean
}>()

const emit = defineEmits<{ change: [name: string, value: unknown] }>()

const { t } = useI18n()

interface Entry {
  field: FormField
  editor?: FormEditor
}

const entries = computed<Entry[]>(() =>
  props.body.fields.map((field) => ({ field, editor: editorOf(props.model, field.name) })),
)

function isLockField(name: string): boolean {
  return props.model.lockFields.includes(name)
}

function spanOf(entry: Entry): string {
  return entry.editor || ['textarea', 'code'].includes(entry.field.widget) || entry.field.span >= 2
    ? '@lg:col-span-2'
    : ''
}

const isFlag = (entry: Entry): boolean => entry.field.type === 'boolean' && !entry.editor

const flagEntries = computed(() => entries.value.filter(isFlag))
const plainEntries = computed(() => entries.value.filter((entry) => !isFlag(entry)))

const inactive = computed(() => props.disabled === true || props.locked === true)
</script>

<template>
  <div class="@container">
    <div class="grid gap-x-6 gap-y-4 @lg:grid-cols-2">
      <template v-for="entry in plainEntries" :key="entry.field.name">
        <EditorField
          v-if="entry.editor"
          class="@lg:col-span-2"
          :model="model"
          :editor="entry.editor"
          :values="values"
          :parent-id="parentId"
          :locked="locked"
          :disabled="disabled"
          @change="(name, value) => emit('change', name, value)"
        />
        <SchemaField
          v-else
          :class="spanOf(entry)"
          :field="entry.field"
          :values="values"
          :errors="errors[entry.field.name]"
          :locked="locked"
          :disabled="disabled"
          :lock-field="isLockField(entry.field.name)"
          @change="(name, value) => emit('change', name, value)"
        />
      </template>

      <div v-if="flagEntries.length > 0" class="flex flex-col gap-1 @lg:col-span-2">
        <span class="px-1 text-xs font-medium text-secondary">{{ t('crud.options') }}</span>
        <div
          class="flex flex-wrap gap-x-8 gap-y-1 rounded-box border border-base-300 bg-base-200 px-4 py-1.5"
          data-testid="flags"
        >
          <SchemaField
            v-for="entry in flagEntries"
            :key="entry.field.name"
            :field="entry.field"
            :values="values"
            :errors="errors[entry.field.name]"
            :locked="locked"
            :disabled="disabled"
            :lock-field="isLockField(entry.field.name)"
            @change="(name, value) => emit('change', name, value)"
          />
        </div>
      </div>

      <PanelCard
        v-for="feature in body.features"
        :key="`feature:${feature.switchField.name}`"
        class="@lg:col-span-2"
        compact
        :title="featureTitle(feature)"
        :hint="feature.switchField.description"
        :bodyless="values[feature.switchField.name] !== true"
        :data-testid="`feature-${feature.switchField.name}`"
      >
        <template #actions>
          <FeatureSwitch
            :name="feature.switchField.name"
            :label="feature.switchField.label"
            :on="values[feature.switchField.name] === true"
            :disabled="inactive || feature.switchField.readOnly"
            @change="(name, value) => emit('change', name, value)"
          />
        </template>
        <SchemaGroupBody
          :body="featureBody(feature)"
          :model="model"
          :values="values"
          :errors="errors"
          :parent-id="parentId"
          :locked="locked"
          :disabled="disabled"
          @change="(name, value) => emit('change', name, value)"
        />
      </PanelCard>

      <PanelCard
        v-for="child in body.children"
        :key="child.path"
        class="@lg:col-span-2"
        compact
        :title="child.label"
      >
        <SchemaGroupBody
          :body="bodyOf(child)"
          :model="model"
          :values="values"
          :errors="errors"
          :parent-id="parentId"
          :locked="locked"
          :disabled="disabled"
          @change="(name, value) => emit('change', name, value)"
        />
      </PanelCard>
    </div>
  </div>
</template>
