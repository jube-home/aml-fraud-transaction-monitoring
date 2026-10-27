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
import { History } from 'lucide-vue-next'
import { useFormat } from '../composables/useFormat'
import { groupTree } from '../formModel/groups'
import type { FormModel, RecordValues } from '../formModel/types'
import { bodyOf, featureBody, groupFeature } from '../formModel/features'
import type { GroupNode } from '../formModel/groups'
import FeatureSwitch from './FeatureSwitch.vue'
import PanelCard from './PanelCard.vue'
import SchemaGroupBody from './SchemaGroupBody.vue'

const props = defineProps<{
  model: FormModel
  values: RecordValues
  errors: Record<string, string[]>
  parentId?: number | string
  locked?: boolean
  disabled?: boolean
}>()

const emit = defineEmits<{ change: [name: string, value: unknown] }>()

const { t } = useI18n()
const { dateTime } = useFormat()

const tree = computed(() => groupTree(props.model))

const features = computed(
  () => new Map(tree.value.map((node) => [node.path, groupFeature(node)] as const)),
)

const inactive = computed(() => props.disabled === true || props.locked === true)

function stamp(user: string, date: string): { user: string; date: string } | undefined {
  const who = props.values[user]
  const when = props.values[date]

  return typeof who === 'string' && who !== '' && typeof when === 'string' && when !== ''
    ? { user: who, date: dateTime(when) }
    : undefined
}

function auditLine(node: GroupNode): string {
  const names = new Set(node.fields.map((field) => field.name))

  if (names.has('updatedUser') && names.has('updatedDate')) {
    const updated = stamp('updatedUser', 'updatedDate')

    if (updated) return t('crud.auditUpdated', updated)
  }

  if (names.has('createdUser') && names.has('createdDate')) {
    const created = stamp('createdUser', 'createdDate')

    if (created) return t('crud.auditCreated', created)
  }

  return ''
}
</script>

<template>
  <div class="flex flex-col gap-4" data-testid="schema-form">
    <template v-for="(node, index) in tree" :key="node.path">
      <PanelCard
        v-if="features.get(node.path)"
        key="feature"
        :title="node.label"
        :step="index + 1"
        :hint="features.get(node.path)!.switchField.description"
        :bodyless="values[features.get(node.path)!.switchField.name] !== true"
        :data-testid="`group-${node.path}`"
      >
        <template #actions>
          <FeatureSwitch
            :name="features.get(node.path)!.switchField.name"
            :label="features.get(node.path)!.switchField.label"
            :on="values[features.get(node.path)!.switchField.name] === true"
            :disabled="inactive || features.get(node.path)!.switchField.readOnly"
            @change="(name, value) => emit('change', name, value)"
          />
        </template>
        <SchemaGroupBody
          :body="featureBody(features.get(node.path)!)"
          :model="model"
          :values="values"
          :errors="errors"
          :parent-id="parentId"
          :locked="locked"
          :disabled="disabled"
          @change="(name, value) => emit('change', name, value)"
        />
      </PanelCard>

      <details
        v-else-if="node.collapsed"
        key="details"
        class="collapse collapse-arrow border border-base-300 bg-base-200"
        :open="node.fields.some((field) => errors[field.name]?.length) || undefined"
        :data-testid="`group-${node.path}`"
      >
        <summary class="collapse-title flex flex-wrap items-center gap-3 font-semibold">
          <span
            aria-hidden="true"
            class="grid size-6 shrink-0 place-items-center rounded-full bg-primary text-primary-content"
          >
            <History class="size-3.5" />
          </span>
          <span class="text-primary">{{ node.label }}</span>
          <span
            v-if="auditLine(node)"
            class="font-mono text-xs font-normal text-secondary"
            :data-testid="`audit-line-${node.path}`"
            >{{ auditLine(node) }}</span
          >
        </summary>
        <div class="collapse-content">
          <SchemaGroupBody
            :body="bodyOf(node)"
            :model="model"
            :values="values"
            :errors="errors"
            :parent-id="parentId"
            :locked="locked"
            :disabled="disabled"
            @change="(name, value) => emit('change', name, value)"
          />
        </div>
      </details>

      <PanelCard
        v-else
        key="plain"
        :title="node.label"
        :step="index + 1"
        :data-testid="`group-${node.path}`"
      >
        <SchemaGroupBody
          :body="bodyOf(node)"
          :model="model"
          :values="values"
          :errors="errors"
          :parent-id="parentId"
          :locked="locked"
          :disabled="disabled"
          @change="(name, value) => emit('change', name, value)"
        />
      </PanelCard>
    </template>
  </div>
</template>
