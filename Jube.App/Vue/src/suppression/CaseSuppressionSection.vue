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
import {useQuery} from '@tanstack/vue-query'
import {useRoute} from 'vue-router'
import {parseJournalState} from '../cases/journal/state'
import {useFormat} from '../composables/useFormat'
import {suppressionApi} from './api'
import CaseSuppressionCard from './CaseSuppressionCard.vue'
import {isolate, mergeParameter, targetName} from './state'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const {dateTime} = useFormat()
const route = useRoute()

const merge = computed(() => mergeParameter(parseJournalState(route.query).merge))

const own = useQuery({
  queryKey: computed(() => ['service', 'CaseSuppression', 'byCase', props.caseId] as const),
  queryFn: () => suppressionApi.listByCase(props.caseId),
  staleTime: 15_000,
  retry: false,
})

const inForce = useQuery({
  queryKey: computed(
    () => ['service', 'CaseSuppression', 'inForce', props.caseId, merge.value] as const,
  ),
  queryFn: () => suppressionApi.inForce(props.caseId, merge.value),
  staleTime: 15_000,
  retry: false,
})

const elsewhere = computed(() =>
  (inForce.data.value ?? []).filter((row) => row.originCaseId !== props.caseId),
)
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-3 rounded-box border border-base-300 bg-base-100 p-3"
    :aria-label="t('suppression.section.title')"
    data-testid="case-suppressions"
  >
    <div class="flex flex-col gap-1">
      <h3 class="text-sm font-semibold">{{ t('suppression.section.title') }}</h3>
      <p class="text-xs text-base-content/70">{{ t('suppression.section.hint') }}</p>
    </div>

    <div class="flex flex-col gap-2">
      <h4 class="text-xs font-semibold">{{ t('suppression.section.own') }}</h4>
      <div v-if="own.isPending.value" key="loading" class="skeleton h-16 w-full" role="status">
        <span class="sr-only">{{ t('cases.loading') }}</span>
      </div>
      <p v-else-if="own.isError.value" key="failed" role="alert" class="text-sm text-error">
        {{ t('suppression.section.failed') }}
      </p>
      <p v-else-if="(own.data.value ?? []).length === 0" key="empty" class="text-sm" role="status">
        {{ t('suppression.section.ownEmpty') }}
      </p>
      <ul v-else key="cards" class="flex flex-col gap-2">
        <CaseSuppressionCard
          v-for="item in own.data.value ?? []"
          :key="item.id"
          :suppression="item"
        />
      </ul>
    </div>

    <div class="flex flex-col gap-2">
      <h4 class="text-xs font-semibold">{{ t('suppression.section.elsewhere') }}</h4>
      <p v-if="inForce.isError.value" key="failed" role="alert" class="text-sm text-error">
        {{ t('suppression.section.failed') }}
      </p>
      <p v-else-if="elsewhere.length === 0" key="empty" class="text-sm" role="status">
        {{ t('suppression.section.elsewhereEmpty') }}
      </p>
      <ul
        v-else
        key="list"
        class="list rounded-box border border-base-300"
        data-testid="suppression-in-force"
      >
        <li v-for="row in elsewhere" :key="`${row.isRule}-${row.id}`" class="list-row">
          <div class="list-col-grow flex flex-col gap-1 text-sm">
            <span class="font-mono font-medium">{{
                t('suppression.pair', {
                  key: isolate(row.overrideKey),
                  value: isolate(row.overrideKeyValue),
                })
              }}</span>
            <span class="flex flex-wrap gap-2">
              <span class="badge badge-ghost badge-sm">
                {{
                  row.overrideKindId === 1 ? t('override.kind.force') : t('override.kind.suppress')
                }}
              </span>
              <span>{{ targetName(row.entityAnalysisModelName, row.ruleName) }}</span>
            </span>
            <span class="font-mono text-xs text-base-content/70">
              {{
                t('suppression.section.meta', {
                  expiry: row.expiryDate
                    ? t('suppression.card.until', {date: dateTime(row.expiryDate)})
                    : t('suppression.section.noExpiry'),
                  user: isolate(row.createdUser),
                  origin: row.originCaseId
                    ? t('suppression.section.fromCase', {id: row.originCaseId})
                    : t('suppression.section.fromOverridePage'),
                })
              }}
            </span>
          </div>
        </li>
      </ul>
    </div>
  </section>
</template>
