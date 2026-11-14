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
import {RouterLink, useRoute} from 'vue-router'
import {useFormat} from '../composables/useFormat'
import SuppressionCounts from './SuppressionCounts.vue'
import SuppressionStanding from './SuppressionStanding.vue'
import {isolate, targetName} from './state'
import type {CaseSuppression} from './types'

defineProps<{ suppression: CaseSuppression }>()

const {t} = useI18n()
const {date, dateTime} = useFormat()
const route = useRoute()

function journalLink(archiveId: number) {
  return {query: {...route.query, section: 'journal', jt: String(archiveId)}}
}
</script>

<template>
  <li
    class="flex flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-3"
    data-testid="suppression-card"
  >
    <div class="flex flex-wrap items-center gap-2">
      <span class="font-mono text-sm font-semibold" data-testid="suppression-card-key">
        {{
          t('suppression.pair', {
            key: isolate(suppression.overrideKey),
            value: isolate(suppression.overrideKeyValue),
          })
        }}
      </span>
      <SuppressionStanding :standing="suppression.standingId"/>
      <span v-if="suppression.sourceId === 2" key="jooby" class="badge badge-ghost badge-sm">
        {{ t('suppression.card.askJooby') }}
      </span>
    </div>

    <ul class="flex flex-col gap-1 text-sm" data-testid="suppression-card-targets">
      <li v-for="target in suppression.targets" :key="target.id" class="flex flex-wrap gap-2">
        <span class="badge badge-ghost badge-sm">
          {{ target.overrideKindId === 1 ? t('override.kind.force') : t('override.kind.suppress') }}
        </span>
        <span>{{ targetName(target.entityAnalysisModelName, target.ruleName) }}</span>
      </li>
    </ul>

    <p class="text-sm" dir="auto" data-testid="suppression-card-reason">{{ suppression.reason }}</p>

    <p class="flex flex-wrap gap-x-4 gap-y-1 font-mono text-xs text-base-content/70">
      <span>{{ t('suppression.card.until', {date: dateTime(suppression.expiryDate)}) }}</span>
      <span>
        {{
          t('suppression.card.requestedBy', {
            user: isolate(suppression.createdUser),
            date: date(suppression.createdDate ?? suppression.expiryDate),
          })
        }}
      </span>
      <span v-if="suppression.decidedUser" key="decided">
        {{ t('suppression.card.decidedBy', {user: isolate(suppression.decidedUser)}) }}
      </span>
      <span v-if="suppression.stateId === 1" key="progress">
        {{
          t('suppression.card.progress', {
            n: suppression.approvalsRecorded,
            total: suppression.approvalsRequired,
          })
        }}
      </span>
    </p>

    <div class="grid gap-3 sm:grid-cols-2">
      <SuppressionCounts
        :heading="t('suppression.card.then')"
        :counts="suppression.then"
        testid="suppression-card-then"
      />
      <SuppressionCounts
        v-if="suppression.now"
        key="now"
        :heading="t('suppression.card.now')"
        :counts="suppression.now"
        testid="suppression-card-now"
      />
    </div>

    <div v-if="suppression.transactions.length > 0" key="evidence" class="flex flex-col gap-1">
      <span class="text-xs font-medium">
        {{
          t(
            'suppression.card.evidence',
            {n: suppression.transactions.length},
            suppression.transactions.length,
          )
        }}
      </span>
      <ul class="flex flex-wrap gap-2" data-testid="suppression-card-evidence">
        <li v-for="item in suppression.transactions.slice(0, 12)" :key="item.id">
          <RouterLink
            class="badge badge-ghost badge-sm font-mono"
            :to="journalLink(item.archiveId)"
            data-testid="suppression-evidence-link"
          >
            {{ item.archiveId }}
          </RouterLink>
        </li>
      </ul>
    </div>
  </li>
</template>
