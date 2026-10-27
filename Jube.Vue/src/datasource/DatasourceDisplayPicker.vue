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
import FieldHelp from '../components/FieldHelp.vue'
import DatasourceTemplateEditor from './DatasourceTemplateEditor.vue'
import { assignRole, choices, fitOf, resolvedColumns, roleColumns } from './state'
import {
  templateKindId,
  type ChartKind,
  type ChartRole,
  type Introspection,
  type IntrospectionColumn,
  type MappingEntry,
} from './types'

const props = defineProps<{
  kinds: ChartKind[]
  introspection: Introspection | undefined
  displayKindId: number
  includeGrid: boolean
  mapping: MappingEntry[]
  templateText: string
  columns: IntrospectionColumn[]
  locked?: boolean
}>()

const emit = defineEmits<{
  'update:displayKindId': [value: number]
  'update:includeGrid': [value: boolean]
  'update:mapping': [value: MappingEntry[]]
  'update:templateText': [value: string]
}>()

const { t } = useI18n()

const kind = computed(() =>
  props.kinds.find((candidate) => candidate.kindId === props.displayKindId),
)

const fitting = computed(() =>
  props.kinds.filter((candidate) => fitOf(props.introspection, candidate.kindId)?.fitsByPosition),
)

const other = computed(() =>
  props.kinds.filter((candidate) => !fitOf(props.introspection, candidate.kindId)?.fitsByPosition),
)

const required = computed(() => kind.value?.roles.filter((role) => !role.optional) ?? [])

const optional = computed(() => kind.value?.roles.filter((role) => role.optional) ?? [])

const chartOn = computed(() => props.displayKindId > 0)

const templateOn = computed(() => props.displayKindId === templateKindId)

function pickKind(event: Event): void {
  emit('update:displayKindId', Number((event.target as HTMLSelectElement).value))
}

function toggleChart(event: Event): void {
  const on = (event.target as HTMLInputElement).checked

  if (!on) {
    emit('update:displayKindId', 0)
    emit('update:mapping', [])

    return
  }

  const first = fitting.value[0] ?? props.kinds[0]

  if (first) emit('update:displayKindId', first.kindId)
}

function positional(role: ChartRole): string {
  const names = resolvedColumns(props.introspection?.resolvedMapping ?? [], role.roleId)

  return names.length > 0 ? names.join(', ') : t('datasource.show.nothing')
}

function setSingle(role: ChartRole, event: Event): void {
  const value = (event.target as HTMLSelectElement).value

  emit('update:mapping', assignRole(props.mapping, role.roleId, value ? [value] : []))
}

function setMany(role: ChartRole, name: string, event: Event): void {
  const current = roleColumns(props.mapping, role.roleId)
  const on = (event.target as HTMLInputElement).checked
  const next = on ? [...current, name] : current.filter((column) => column !== name)

  emit('update:mapping', assignRole(props.mapping, role.roleId, next))
}
</script>

<template>
  <div class="flex flex-col gap-4" data-testid="datasource-picker">
    <div class="flex flex-wrap items-center gap-6">
      <label class="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          class="toggle toggle-sm toggle-primary"
          :checked="chartOn"
          :disabled="locked"
          data-testid="datasource-chart-toggle"
          @change="toggleChart"
        />
        <span>{{ t('datasource.show.chart') }}</span>
      </label>
      <label class="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          class="toggle toggle-sm toggle-primary"
          :checked="includeGrid"
          :disabled="locked"
          data-testid="datasource-grid-toggle"
          @change="emit('update:includeGrid', ($event.target as HTMLInputElement).checked)"
        />
        <span>{{ t('datasource.show.grid') }}</span>
      </label>
    </div>

    <template v-if="chartOn">
      <label class="flex max-w-md flex-col gap-1 text-sm">
        <span class="label flex items-center gap-1">
          {{ t('datasource.show.kind') }}
          <FieldHelp v-if="kind" :label="kind.name" :text="kind.description" />
        </span>
        <select
          class="select select-sm w-full"
          :value="displayKindId"
          :disabled="locked"
          data-testid="datasource-kind"
          @change="pickKind"
        >
          <optgroup :label="t('datasource.show.fits')">
            <option v-for="item in fitting" :key="item.kindId" :value="item.kindId">
              {{ item.name }}
            </option>
          </optgroup>
          <optgroup :label="t('datasource.show.needs')">
            <option v-for="item in other" :key="item.kindId" :value="item.kindId">
              {{ item.name }}
            </option>
          </optgroup>
        </select>
      </label>

      <p
        v-if="
          fitOf(introspection, displayKindId)?.reason &&
          !fitOf(introspection, displayKindId)?.fitsByPosition
        "
        key="reason"
        class="text-sm text-secondary"
        data-testid="datasource-kind-reason"
      >
        {{ fitOf(introspection, displayKindId)?.reason }}
      </p>

      <template v-if="templateOn">
        <div class="flex flex-col gap-1 text-sm" key="template">
          <span class="label flex items-center gap-1">
            {{ t('datasource.template.label') }}
            <FieldHelp
              :label="t('datasource.template.label')"
              :text="t('datasource.template.hint')"
            />
          </span>
          <DatasourceTemplateEditor
            :model-value="templateText"
            :label="t('datasource.template.label')"
            :readonly="locked"
            @update:model-value="emit('update:templateText', $event)"
          />
        </div>
        <p
          v-if="introspection?.templateError"
          key="templateError"
          role="alert"
          class="alert alert-error text-sm"
          data-testid="datasource-template-error"
        >
          {{ introspection.templateError }}
        </p>
      </template>

      <div
        v-else-if="kind"
        key="roles"
        class="grid gap-4 sm:grid-cols-2"
        data-testid="datasource-roles"
      >
        <template v-for="role in required" :key="role.roleId">
          <fieldset v-if="role.many" class="flex flex-col gap-1 text-sm">
            <legend class="label flex items-center gap-1">
              {{ role.name }}
              <FieldHelp :label="role.name" :text="role.description" />
            </legend>
            <label
              v-for="column in choices(role, columns)"
              :key="column.name"
              class="flex items-center gap-2"
            >
              <input
                type="checkbox"
                class="checkbox checkbox-sm"
                :checked="roleColumns(mapping, role.roleId).includes(column.name)"
                :disabled="locked"
                :data-testid="`datasource-role-${role.roleId}-${column.name}`"
                @change="setMany(role, column.name, $event)"
              />
              <span class="font-mono">{{ column.name }}</span>
            </label>
          </fieldset>
          <label v-else :key="`single-${role.roleId}`" class="flex flex-col gap-1 text-sm">
            <span class="label flex items-center gap-1">
              {{ role.name }}
              <FieldHelp :label="role.name" :text="role.description" />
            </span>
            <select
              class="select select-sm w-full"
              :value="roleColumns(mapping, role.roleId)[0] ?? ''"
              :disabled="locked"
              :data-testid="`datasource-role-${role.roleId}`"
              @change="setSingle(role, $event)"
            >
              <option value="">
                {{ t('datasource.show.byPosition', { columns: positional(role) }) }}
              </option>
              <option
                v-for="column in choices(role, columns)"
                :key="column.name"
                :value="column.name"
              >
                {{ column.name }}
              </option>
            </select>
          </label>
        </template>
      </div>

      <details
        v-if="optional.length > 0"
        key="optional"
        class="collapse-arrow collapse rounded-box border border-base-300"
      >
        <summary class="collapse-title text-sm font-semibold">
          {{ t('datasource.show.optional') }}
        </summary>
        <div class="collapse-content grid gap-4 sm:grid-cols-2">
          <template v-for="role in optional" :key="role.roleId">
            <fieldset v-if="role.many" class="flex flex-col gap-1 text-sm">
              <legend class="label">{{ role.name }}</legend>
              <label
                v-for="column in choices(role, columns)"
                :key="column.name"
                class="flex items-center gap-2"
              >
                <input
                  type="checkbox"
                  class="checkbox checkbox-sm"
                  :checked="roleColumns(mapping, role.roleId).includes(column.name)"
                  :disabled="locked"
                  @change="setMany(role, column.name, $event)"
                />
                <span class="font-mono">{{ column.name }}</span>
              </label>
            </fieldset>
            <label v-else :key="`single-${role.roleId}`" class="flex flex-col gap-1 text-sm">
              <span class="label">{{ role.name }}</span>
              <select
                class="select select-sm w-full"
                :value="roleColumns(mapping, role.roleId)[0] ?? ''"
                :disabled="locked"
                :data-testid="`datasource-role-${role.roleId}`"
                @change="setSingle(role, $event)"
              >
                <option value="">
                  {{ t('datasource.show.byPosition', { columns: positional(role) }) }}
                </option>
                <option
                  v-for="column in choices(role, columns)"
                  :key="column.name"
                  :value="column.name"
                >
                  {{ column.name }}
                </option>
              </select>
            </label>
          </template>
        </div>
      </details>

      <p
        v-if="introspection?.mappingError"
        key="mapping"
        role="alert"
        class="alert alert-error text-sm"
        data-testid="datasource-mapping-error"
      >
        {{ introspection.mappingError }}
      </p>
    </template>
  </div>
</template>
