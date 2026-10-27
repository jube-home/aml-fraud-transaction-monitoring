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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleDot } from 'lucide-vue-next'
import { useFormat } from '../composables/useFormat'
import { durationText } from './duration'
import { enterReasonKeys, exitReasonKeys } from './labels'
import { layoutStays, orderStays } from './stayBar'
import type { CaseStay } from './types'

const props = defineProps<{ stays: CaseStay[] }>()

const { t } = useI18n()
const { number, dateTime } = useFormat()

const active = ref<number | null>(null)

const rows = computed(() => {
  const ordered = orderStays(props.stays)

  return layoutStays(ordered).map((segment, index) => ({
    stay: ordered[index]!,
    segment,
  }))
})

function minutesText(minutes: number): string {
  return durationText(t, minutes, number)
}

function nameText(stay: CaseStay): string {
  return stay.statusName ?? t('cases.stay.unnamed')
}

function reasonsText(stay: CaseStay): string {
  const entered = t(enterReasonKeys[stay.enterReason] ?? 'cases.stay.created')

  return stay.exitReason === null
    ? entered
    : `${entered} · ${t(exitReasonKeys[stay.exitReason] ?? 'cases.stay.movedOn')}`
}

function enteredText(stay: CaseStay): string {
  return stay.enteredUser
    ? t('cases.stay.enteredBy', { date: dateTime(stay.enteredDate), user: stay.enteredUser })
    : t('cases.stay.enteredAt', { date: dateTime(stay.enteredDate) })
}

function overText(minutes: number): string {
  return t('cases.stay.overBy', { duration: minutesText(minutes) })
}

function segmentLabel(entry: (typeof rows.value)[number]): string {
  return [
    t('cases.stay.segmentLabel', {
      name: nameText(entry.stay),
      duration: minutesText(entry.segment.minutes),
    }),
    entry.segment.current ? t('cases.stay.current') : '',
    entry.segment.breach ? overText(entry.segment.overMinutes) : '',
  ]
    .filter((part) => part)
    .join(', ')
}

function limitText(entry: (typeof rows.value)[number]): string {
  return t('cases.stay.limitUsed', {
    used: minutesText(entry.stay.minutes),
    limit: minutesText(entry.stay.maxMinutes ?? 0),
  })
}

function show(index: number) {
  active.value = index
}

function clear(index: number) {
  if (active.value === index) active.value = null
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="stay-bar">
    <ol
      role="list"
      class="m-0 flex w-full gap-0.5 p-0"
      :aria-label="t('cases.stay.barLabel', { count: number(rows.length) })"
      data-testid="stay-bar-track"
    >
      <li
        v-for="(entry, index) in rows"
        :key="index"
        class="flex min-w-4 flex-col"
        :style="{ flex: `${entry.segment.percent} 1 0%` }"
        data-testid="stay-segment"
        :data-percent="entry.segment.percent"
        :data-current="entry.segment.current"
        :data-breach="entry.segment.breach"
      >
        <button
          type="button"
          :aria-label="segmentLabel(entry)"
          :aria-current="entry.segment.current ? 'step' : undefined"
          :class="[
            'flex h-10 w-full min-w-0 items-center gap-1 overflow-hidden px-1.5 text-xs focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
            entry.segment.breach ? 'bg-error text-error-content' : 'bg-base-300 text-base-content',
            entry.segment.current
              ? 'border-b-4 border-base-content'
              : 'border-b-4 border-transparent',
            index === 0 ? 'rounded-s-box' : '',
            index === rows.length - 1 ? 'rounded-e-box' : '',
          ]"
          @mouseenter="show(index)"
          @mouseleave="clear(index)"
          @focus="show(index)"
          @blur="clear(index)"
        >
          <CircleDot v-if="entry.segment.current" :size="12" class="shrink-0" aria-hidden="true" />
          <span class="truncate" aria-hidden="true">{{ nameText(entry.stay) }}</span>
        </button>
      </li>
    </ol>
    <ol role="list" class="m-0 flex flex-col gap-1 p-0" data-testid="stay-list">
      <li
        v-for="(entry, index) in rows"
        :key="index"
        :class="[
          'flex min-w-0 flex-col gap-1 rounded-box border px-2 py-1',
          active === index ? 'border-primary ring-2 ring-primary' : 'border-base-300',
        ]"
        data-testid="stay-row"
      >
        <div class="flex flex-wrap items-center justify-between gap-x-2 gap-y-1">
          <div class="flex min-w-0 flex-wrap items-center gap-1">
            <span class="text-sm font-medium break-words">{{ nameText(entry.stay) }}</span>
            <span v-if="entry.segment.current" key="current" class="badge badge-sm badge-soft">
              {{ t('cases.stay.current') }}
            </span>
          </div>
          <div class="flex flex-wrap items-center gap-1">
            <span class="font-mono text-sm tabular-nums">{{
              minutesText(entry.segment.minutes)
            }}</span>
            <span v-if="entry.segment.breach" key="over" class="badge badge-sm badge-error">
              {{ overText(entry.segment.overMinutes) }}
            </span>
          </div>
        </div>
        <div class="text-xs text-base-content/70">
          {{ enteredText(entry.stay) }}
        </div>
        <div class="text-xs text-base-content/70">{{ reasonsText(entry.stay) }}</div>
        <div v-if="entry.segment.visits > 1" key="visits" class="text-xs text-base-content/70">
          {{
            t('cases.stay.totalVisits', {
              duration: minutesText(entry.segment.totalMinutes),
              count: number(entry.segment.visits),
            })
          }}
        </div>
        <div v-if="entry.segment.used !== null" key="limit" class="flex flex-col gap-0.5">
          <progress
            :class="['progress w-full', entry.segment.breach ? 'progress-error' : '']"
            :value="entry.segment.used"
            :max="entry.stay.maxMinutes ?? 0"
            :aria-label="limitText(entry)"
          ></progress>
          <span class="font-mono text-xs text-base-content/70">{{ limitText(entry) }}</span>
        </div>
      </li>
    </ol>
  </div>
</template>
