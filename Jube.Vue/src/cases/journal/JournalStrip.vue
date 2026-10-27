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
import StatCard from '../../components/StatCard.vue'
import FieldHelp from '../../components/FieldHelp.vue'
import { useFormat } from '../../composables/useFormat'
import type { JournalAnalytics } from './types'

const props = defineProps<{ analytics: JournalAnalytics | null; failed: boolean }>()

const emit = defineEmits<{ open: [id: number] }>()

const { t } = useI18n()
const { number, fixed } = useFormat()

const summary = computed(() => props.analytics?.summary ?? null)
const fields = computed(() => (summary.value?.fields ?? []).slice(0, 3))
const notable = computed(() => (props.analytics?.notable?.rows ?? []).slice(0, 5))
const unusual = computed(() => new Set(notable.value.map((row) => row.worst)))
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="journal-strip">
    <p v-if="failed" key="failed" role="alert" class="text-sm">{{ t('journal.strip.failed') }}</p>

    <div v-if="summary" key="cards" class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <StatCard
        :title="t('journal.strip.transactions')"
        :help="t('journal.help.transactions')"
        value-testid="journal-strip-count"
        small
      >
        {{ number(summary.count) }}
        <template #more>
          <div v-if="analytics?.meta.truncated" key="truncated" class="stat-desc">
            {{ t('journal.strip.truncated', { n: number(analytics.meta.limit) }) }}
          </div>
        </template>
      </StatCard>
      <StatCard :title="t('journal.strip.activeDays')" :help="t('journal.help.activeDays')" small>{{
        number(summary.activeDays)
      }}</StatCard>
      <StatCard
        :title="t('journal.strip.withActivations')"
        :help="t('journal.help.withActivations')"
        small
      >
        {{ number(summary.activationRows) }}
        <template #more>
          <div class="stat-desc">
            {{ t('journal.strip.activations', { n: number(summary.activations) }) }}
          </div>
        </template>
      </StatCard>
      <StatCard :title="t('journal.strip.elevation')" :help="t('journal.help.elevation')" small>{{
        number(summary.maxElevation)
      }}</StatCard>
      <StatCard
        v-for="field in fields"
        :key="field.field"
        :title="field.field"
        :help="t('journal.help.field')"
        :data-testid="`journal-strip-field-${field.field}`"
        small
      >
        {{ fixed(field.latest, Number.isInteger(field.latest) ? 0 : 2) }}
        <template #more>
          <div class="stat-desc">
            {{
              t('journal.strip.typical', {
                n: fixed(field.median, Number.isInteger(field.median) ? 0 : 2),
              })
            }}
            <span v-if="unusual.has(field.field)" class="badge badge-warning badge-sm ms-1">
              {{ t('journal.strip.stands') }}
            </span>
          </div>
        </template>
      </StatCard>
    </div>

    <p
      v-if="analytics?.meta.lowConfidence"
      key="few"
      class="text-sm text-base-content/70"
      role="status"
      data-testid="journal-strip-few"
    >
      {{ t('journal.strip.few') }}
    </p>

    <div
      v-if="analytics"
      key="notable"
      class="flex flex-col gap-2 rounded-box border border-base-300 p-3"
      data-testid="journal-notable"
    >
      <h4 class="flex items-center gap-1 text-sm font-semibold">
        {{ t('journal.notable.title') }}
        <FieldHelp :label="t('journal.notable.title')" :text="t('journal.help.notable')" />
      </h4>
      <p v-if="notable.length === 0" key="none" class="text-sm text-base-content/70">
        {{ t('journal.notable.none') }}
      </p>
      <ul v-else key="list" class="flex flex-col gap-1">
        <li v-for="row in notable" :key="row.id">
          <button
            type="button"
            class="btn btn-ghost btn-sm h-auto w-full justify-start py-1 text-start font-normal"
            :data-testid="`journal-notable-${row.id}`"
            @click="emit('open', row.id)"
          >
            <span class="badge badge-warning badge-sm font-mono tabular-nums">
              {{ fixed(Math.abs(row.reasons[0]!.z), 1) }}
            </span>
            <span class="text-sm">
              {{
                t('journal.notable.row', {
                  field: row.reasons[0]!.field,
                  value: fixed(
                    row.reasons[0]!.value,
                    Number.isInteger(row.reasons[0]!.value) ? 0 : 2,
                  ),
                  median: fixed(
                    row.reasons[0]!.median,
                    Number.isInteger(row.reasons[0]!.median) ? 0 : 2,
                  ),
                })
              }}
            </span>
          </button>
        </li>
      </ul>
    </div>
  </section>
</template>
