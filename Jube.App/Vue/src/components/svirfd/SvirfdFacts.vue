<!--  Copyright (C) 2022-present Jube Holdings Limited.

  This file is part of Jube™ software.

  Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
  as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
  Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
  of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

  You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
  see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useFormat} from '../../composables/useFormat'
import {claimKey, confidenceTone, rpkiTone, toneClass} from '../../svirfd/format'
import {isChanged} from '../../svirfd/drift'
import {facts, valueAt, type FactSpec} from '../../svirfd/sections'
import type {IpRecord} from '../../svirfd/types'

const props = defineProps<{ record: IpRecord; section: string; changed: Set<string> }>()

const {t} = useI18n()
const {dateTime} = useFormat()

interface Row {
  spec: FactSpec
  value: unknown
}

const rows = computed<Row[]>(() =>
  facts
    .filter((spec) => spec.section === props.section)
    .map((spec) => ({spec, value: valueAt(props.record, spec.path)}))
    .filter((row) => row.spec.kind === 'claim' || (row.value !== null && row.value !== '')),
)

function claimText(value: boolean | null): string {
  const key = claimKey(value)

  if (key === 'yes') return t('svirfd.yes')
  if (key === 'no') return t('svirfd.no')

  return t('svirfd.notClaimed')
}

function text(value: unknown): string {
  if (Array.isArray(value)) return value.join(', ')

  return String(value)
}
</script>

<template>
  <dl
    v-if="rows.length > 0"
    class="grid grid-cols-[minmax(8rem,auto)_1fr] gap-x-4 gap-y-1 text-sm"
    :data-testid="`svirfd-section-${section}`"
  >
    <template v-for="row in rows" :key="row.spec.path">
      <dt class="text-secondary">{{ t(row.spec.label) }}</dt>
      <dd
        :class="[
          'min-w-0 break-words',
          isChanged(changed, row.spec.path) ? 'bg-warning/20 rounded-field px-1' : '',
        ]"
        :data-testid="`svirfd-fact-${row.spec.path}`"
        :data-changed="isChanged(changed, row.spec.path) ? 'true' : undefined"
      >
        <span
          v-if="row.spec.kind === 'claim'"
          :class="['badge badge-sm', toneClass(row.value === true ? 'warning' : 'neutral')]"
        >{{ claimText(row.value as boolean | null) }}</span
        >
        <span
          v-else-if="row.spec.kind === 'bool'"
          :class="['badge badge-sm', toneClass(row.value === true ? 'info' : 'neutral')]"
        >{{ row.value === true ? t('svirfd.yes') : t('svirfd.no') }}</span
        >
        <span
          v-else-if="row.spec.kind === 'rpki'"
          :class="['badge badge-sm', toneClass(rpkiTone(row.value as string))]"
        >{{ row.value }}</span
        >
        <span
          v-else-if="row.spec.kind === 'confidence'"
          :class="['badge badge-sm', toneClass(confidenceTone(row.value as string))]"
        >{{ row.value }}</span
        >
        <span v-else-if="row.spec.kind === 'date'" dir="ltr" class="font-mono">{{
            dateTime(row.value as string)
          }}</span>
        <span v-else-if="row.spec.kind === 'number'" dir="ltr" class="font-mono">{{
            row.value
          }}</span>
        <span v-else-if="row.spec.kind === 'list'" class="flex flex-wrap gap-1">
          <span
            v-for="item in row.value as string[]"
            :key="item"
            dir="auto"
            class="badge badge-ghost badge-sm"
          >{{ item }}</span
          >
        </span>
        <span v-else dir="auto">{{ text(row.value) }}</span>
      </dd>
    </template>
  </dl>
</template>
