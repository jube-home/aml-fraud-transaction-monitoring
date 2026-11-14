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
import {Link2} from 'lucide-vue-next'
import {RouterLink} from 'vue-router'
import FieldHelp from '../../components/FieldHelp.vue'
import {useFormat} from '../../composables/useFormat'
import {caseLabel} from '../labels'
import {
  headlineOf,
  outcomeKeys,
  outcomeOf,
  reasonKey,
  similarityApi,
  similarityKey,
  similaritySectionId,
  type SimilarOutcome,
} from './similarityApi'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const {number, dateTime} = useFormat()

const similarity = useQuery({
  queryKey: computed(() => similarityKey(props.caseId)),
  queryFn: () => similarityApi.get(props.caseId),
  staleTime: 60_000,
  retry: false,
})

const data = computed(() => similarity.data.value)
const neighbours = computed(() => data.value?.neighbours ?? [])
const headline = computed(() => headlineOf(data.value, similarity.isError.value))

const outcomeClass: Record<SimilarOutcome, string> = {
  confirmed: 'badge badge-sm badge-error',
  notConfirmed: 'badge badge-soft badge-sm',
  closed: 'badge badge-ghost badge-sm',
  open: 'badge badge-ghost badge-sm',
}
</script>

<template>
  <section
    :id="similaritySectionId"
    class="flex min-w-0 scroll-mt-4 flex-col gap-2"
    tabindex="-1"
    data-testid="case-similar"
  >
    <h4 class="flex items-center gap-2 text-sm font-semibold">
      {{ t('caseDetail.similar.title') }}
      <span
        v-if="data && data.similar > 0"
        class="badge badge-ghost badge-sm font-mono tabular-nums"
        data-testid="case-similar-count"
      >
        {{ number(data.similar) }}
      </span>
      <FieldHelp :label="t('caseDetail.similar.title')" :text="t('caseDetail.similar.hint')"/>
    </h4>

    <div v-if="headline === 'loading'" key="loading" class="skeleton h-16 w-full" role="status"></div>
    <p v-else-if="headline === 'failed'" key="failed" class="text-sm" role="alert">
      {{ t('caseDetail.similar.failed') }}
    </p>
    <p
      v-else-if="headline === 'pending' || headline === 'none'"
      key="empty"
      class="text-sm text-base-content/70"
      role="status"
      data-testid="case-similar-empty"
    >
      {{ t(headline === 'pending' ? 'caseDetail.similar.pending' : 'caseDetail.similar.none') }}
    </p>
    <template v-else-if="data">
      <p class="text-sm" data-testid="case-similar-headline">
        {{
          headline === 'headline'
            ? t('caseDetail.similar.headline', {
              confirmed: number(data.confirmed),
              closed: number(data.closed),
            })
            : t('caseDetail.similar.tooFew')
        }}
        <span v-if="data.open > 0" class="ms-2 text-base-content/70">
          {{ t('caseDetail.similar.open', {n: number(data.open)}) }}
        </span>
      </p>

      <ul class="list" data-testid="case-similar-list">
        <li
          v-for="neighbour in neighbours"
          :key="neighbour.caseId"
          class="list-row items-start px-0 py-2"
          :data-testid="`case-similar-${neighbour.caseId}`"
        >
          <RouterLink class="link font-mono text-sm tabular-nums" :to="`/cases/${neighbour.caseId}`">
            {{ caseLabel(neighbour.caseId) }}
          </RouterLink>
          <div class="list-col-grow flex min-w-0 flex-col gap-1">
            <div class="flex min-w-0 flex-wrap items-center gap-2 text-sm">
              <span class="min-w-0 break-all font-mono">{{ neighbour.caseKeyValue ?? '' }}</span>
              <span class="badge badge-soft badge-sm">
                {{
                  t(
                    neighbour.bandId === 3
                      ? 'caseDetail.similar.band.strong'
                      : 'caseDetail.similar.band.moderate',
                  )
                }}
              </span>
              <span
                v-if="neighbour.linked"
                class="badge badge-ghost badge-sm gap-1"
                :title="t('caseDetail.similar.linkedHint')"
                :data-testid="`case-similar-linked-${neighbour.caseId}`"
              >
                <Link2 class="size-3" aria-hidden="true"/>
                {{ t('caseDetail.similar.linked') }}
              </span>
            </div>
            <ul class="flex flex-wrap gap-1">
              <li
                v-for="reason in neighbour.reasons"
                :key="`${reason.kindId}-${reason.name}`"
                class="badge badge-ghost badge-sm max-w-full truncate"
              >
                {{ t(reasonKey(reason.kindId), {name: reason.name}) }}
              </li>
            </ul>
          </div>
          <div class="flex flex-col items-end gap-0.5">
            <span :class="outcomeClass[outcomeOf(neighbour)]" :title="neighbour.status ?? ''">
              {{
                outcomeOf(neighbour) === 'open' && neighbour.status
                  ? neighbour.status
                  : t(outcomeKeys[outcomeOf(neighbour)])
              }}
            </span>
            <span
              v-if="neighbour.closedDate"
              class="font-mono text-xs text-base-content/70 tabular-nums"
            >
              {{ dateTime(neighbour.closedDate) }}
            </span>
          </div>
        </li>
      </ul>

      <p
        v-if="data.similar > neighbours.length"
        key="more"
        class="text-xs text-base-content/70"
      >
        {{
          t('caseDetail.similar.more', {
            shown: number(neighbours.length),
            total: number(data.similar),
          })
        }}
      </p>
    </template>
  </section>
</template>
