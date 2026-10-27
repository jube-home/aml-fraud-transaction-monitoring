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
import { useFormat } from '../../composables/useFormat'
import { legKindText, legOutcomeClass, legOutcomeText } from './holdText'
import type { HoldLeg } from './operationsApi'

defineProps<{ legs: readonly HoldLeg[]; testid: string }>()

const { t } = useI18n()
const format = useFormat()
</script>

<template>
  <ol
    v-if="legs.length > 0"
    class="flex min-w-0 flex-col gap-1 text-xs"
    role="list"
    :data-testid="testid"
  >
    <li
      v-for="(leg, index) in legs"
      :key="`${leg.kindId}-${leg.outcomeId}-${index}`"
      class="flex min-w-0 flex-wrap items-center gap-2"
    >
      <span class="badge badge-sm" :class="legOutcomeClass(leg.outcomeId)">{{
        legOutcomeText(t, leg.outcomeId)
      }}</span>
      <span class="font-medium">{{ legKindText(t, leg.kindId) }}</span>
      <span v-if="leg.destination" class="min-w-0 wrap-break-word font-mono text-base-content/70">{{
        leg.destination
      }}</span>
      <span v-if="leg.httpStatus" class="font-mono">{{
        t('caseDetail.hold.leg.answer', { status: leg.httpStatus })
      }}</span>
      <span class="ms-auto font-mono text-base-content/70">{{ format.dateTime(leg.date) }}</span>
      <span v-if="leg.detail" class="w-full wrap-break-word text-error">{{ leg.detail }}</span>
    </li>
  </ol>
</template>
