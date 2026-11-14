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
import {useI18n} from 'vue-i18n'
import {matchesFormat} from './text'
import type {CaseFieldEntry} from './types'

defineProps<{ fields: CaseFieldEntry[] }>()

const {t} = useI18n()

function valueStyle(entry: CaseFieldEntry): Record<string, string> {
  if (!matchesFormat(entry)) return {}

  const style: Record<string, string> = {}

  if (entry.cellFormatForeColor) style.color = entry.cellFormatForeColor
  if (entry.cellFormatBackColor) style.backgroundColor = entry.cellFormatBackColor

  return style
}

function painted(entry: CaseFieldEntry): boolean {
  return matchesFormat(entry) && !!(entry.cellFormatForeColor || entry.cellFormatBackColor)
}
</script>

<template>
  <section class="@container min-w-0" data-testid="case-details">
    <p v-if="fields.length === 0" class="text-sm text-base-content/70" role="status">
      {{ t('caseDetail.details.empty') }}
    </p>
    <dl
      v-else
      key="fields"
      class="min-w-0 gap-x-10 @xl:columns-2 @xl:[column-rule:1px_solid_var(--color-base-300)]"
      :aria-label="t('caseDetail.details.title')"
    >
      <div
        v-for="(entry, index) in fields"
        :key="`${entry.name}-${index}`"
        class="grid min-w-0 grid-cols-[minmax(6rem,2fr)_3fr] items-baseline gap-3 break-inside-avoid border-b border-base-200 px-2 py-1.5 hover:bg-base-200/60"
      >
        <dt class="min-w-0 truncate text-xs text-base-content/70" :title="entry.name ?? ''">
          {{ entry.name }}
        </dt>
        <dd class="min-w-0 break-words font-mono text-sm">
          <span
            :class="painted(entry) ? 'rounded-sm px-1' : ''"
            :style="valueStyle(entry)"
          >{{ entry.value }}</span>
        </dd>
      </div>
    </dl>
  </section>
</template>
