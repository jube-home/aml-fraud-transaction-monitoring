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
import { useI18n } from 'vue-i18n'
import type { ColumnRole } from './state'
import type { IntrospectionColumn } from './types'

defineProps<{ columns: IntrospectionColumn[]; roles: Map<string, ColumnRole> }>()

const { t } = useI18n()
</script>

<template>
  <ol
    class="flex flex-wrap gap-2"
    :aria-label="t('datasource.columns.title')"
    data-testid="datasource-columns"
  >
    <li
      v-for="column in columns"
      :key="column.name"
      class="flex flex-col gap-1 rounded-field border border-base-300 bg-base-100 px-3 py-2"
      :data-testid="`datasource-column-${column.ordinal}`"
    >
      <span class="flex items-center gap-2">
        <span class="badge badge-sm font-mono badge-soft">{{ column.ordinal }}</span>
        <span class="font-mono text-sm font-semibold">{{ column.name }}</span>
      </span>
      <span class="font-mono text-xs text-secondary">{{ column.dataTypeName }}</span>
      <span
        v-if="roles.get(column.name)"
        key="role"
        class="badge badge-sm font-mono"
        :class="roles.get(column.name)?.explicit ? 'badge-primary' : 'badge-soft'"
        :data-testid="`datasource-column-role-${column.ordinal}`"
      >
        {{ roles.get(column.name)?.name }}
      </span>
      <span v-else key="unused" class="text-xs text-secondary">{{
        t('datasource.columns.unused')
      }}</span>
    </li>
  </ol>
</template>
