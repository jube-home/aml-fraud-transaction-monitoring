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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {ListChecks, ShieldAlert} from 'lucide-vue-next'
import type {ChecklistOverride} from './types'

const props = defineProps<{
  names: string[]
  override: ChecklistOverride | null
  canOverride: boolean
  own?: boolean
  testid: string
}>()

const emit = defineEmits<{ openChecklist: []; override: [] }>()

const {t} = useI18n()

const shown = computed(() => props.names.slice(0, 5))
const hidden = computed(() => Math.max(0, props.names.length - shown.value.length))
</script>

<template>
  <div
    v-if="override"
    key="granted"
    role="status"
    class="alert alert-info alert-soft alert-vertical text-sm sm:alert-horizontal"
    :data-testid="`${testid}-override`"
  >
    <ShieldAlert class="size-5 shrink-0" aria-hidden="true"/>
    <span v-if="own" key="own" :data-testid="`${testid}-override-own`">
      {{ t('caseDetail.checklist.gate.ownOverride', {reason: override.reason}) }}
    </span>
    <span v-else key="other">
      {{
        t('caseDetail.checklist.gate.inPlace', {
          user: override.grantedUser,
          reason: override.reason,
        })
      }}
    </span>
  </div>
  <div
    v-else-if="names.length > 0"
    key="open"
    role="alert"
    class="alert alert-warning alert-soft alert-vertical text-sm sm:alert-horizontal"
    :data-testid="testid"
  >
    <ListChecks class="size-5 shrink-0" aria-hidden="true"/>
    <div class="flex min-w-0 flex-col gap-1">
      <span class="font-medium" :data-testid="`${testid}-count`">
        {{ t('caseDetail.checklist.gate.open', {n: names.length}, names.length) }}
      </span>
      <ul class="flex flex-col gap-0.5 text-xs" :data-testid="`${testid}-names`">
        <li v-for="name in shown" :key="name" class="before:me-1.5 before:content-['•']">
          {{ name }}
        </li>
        <li v-if="hidden > 0" key="more">
          {{ t('caseDetail.checklist.gate.more', {n: hidden}) }}
        </li>
      </ul>
    </div>
    <div class="flex flex-wrap gap-2">
      <button
        type="button"
        class="btn btn-sm"
        :data-testid="`${testid}-open`"
        @click="emit('openChecklist')"
      >
        {{ t('caseDetail.checklist.gate.openChecklist') }}
      </button>
      <button
        v-if="canOverride"
        key="override"
        type="button"
        class="btn btn-sm"
        :data-testid="`${testid}-override-action`"
        @click="emit('override')"
      >
        {{ t('caseDetail.checklist.gate.override') }}
      </button>
    </div>
  </div>
</template>
