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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQueryClient } from '@tanstack/vue-query'
import { useFormat } from '../composables/useFormat'
import { useToasts } from '../stores/toasts'
import { diaryApi, isoToLocalInput, localInputToIso } from './diaryApi'
import { relativeText } from './duration'
import { diaryTypeKeys } from './labels'
import type { CaseRow } from './types'

const props = defineProps<{ row: CaseRow }>()

const { t } = useI18n()
const { number, dateTime } = useFormat()
const toasts = useToasts()
const client = useQueryClient()

const type = ref(String(props.row.diary ? props.row.diaryTypeId : 1))
const due = ref(isoToLocalInput(props.row.diary ? props.row.diaryDate : null))

watch(
  () => [props.row.caseId, props.row.diary, props.row.diaryTypeId, props.row.diaryDate] as const,
  () => {
    type.value = String(props.row.diary ? props.row.diaryTypeId : 1)
    due.value = isoToLocalInput(props.row.diary ? props.row.diaryDate : null)
  },
)

const choices = computed(() =>
  Object.entries(diaryTypeKeys).map(([value, key]) => ({ value, label: t(key) })),
)

const iso = computed(() => localInputToIso(due.value))

const current = computed(() => {
  if (!props.row.diary || !props.row.diaryDate) return ''

  const name = t(diaryTypeKeys[props.row.diaryTypeId] ?? 'cases.diary.type.review')
  const when = dateTime(props.row.diaryDate)
  const left = relativeText(t, props.row.diaryDate, Date.now(), number)

  return `${name} · ${when} (${left})`
})

const editable = computed(() => props.row.closedStatusId === 0)

function refresh(): void {
  void client.invalidateQueries({ queryKey: ['service', 'Case'] })
}

const save = useMutation({
  mutationFn: () => diaryApi.set(props.row.caseId, Number(type.value), iso.value!),
  onSuccess: () => {
    toasts.success(t('cases.diary.saved'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const clear = useMutation({
  mutationFn: () => diaryApi.clear(props.row.caseId),
  onSuccess: () => {
    toasts.success(t('cases.diary.cleared'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <section class="flex flex-col gap-2" data-testid="cases-peek-diary">
    <h3 class="text-sm font-semibold">{{ t('cases.diary.title') }}</h3>

    <p
      v-if="row.diary && row.diaryDate"
      key="current"
      class="text-sm"
      data-testid="cases-diary-current"
    >
      {{ current }}
    </p>
    <p v-else key="none" class="text-sm text-base-content/70">{{ t('cases.diary.none') }}</p>

    <p v-if="!editable" key="closed" class="text-xs text-base-content/70">
      {{ t('cases.diary.notOpen') }}
    </p>

    <form
      v-else
      key="form"
      class="flex flex-col gap-2"
      data-testid="cases-diary-form"
      @submit.prevent="save.mutate()"
    >
      <fieldset class="fieldset">
        <label class="label" for="cases-diary-type">{{ t('cases.diary.typeLabel') }}</label>
        <select
          id="cases-diary-type"
          v-model="type"
          class="select select-sm w-full"
          data-testid="cases-diary-type"
        >
          <option v-for="choice in choices" :key="choice.value" :value="choice.value">
            {{ choice.label }}
          </option>
        </select>
      </fieldset>

      <fieldset class="fieldset">
        <label class="label" for="cases-diary-due">{{ t('cases.diary.dueLabel') }}</label>
        <input
          id="cases-diary-due"
          v-model="due"
          type="datetime-local"
          class="input input-sm w-full font-mono"
          data-testid="cases-diary-due"
        />
      </fieldset>

      <div class="flex flex-wrap gap-2">
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="iso === null || save.isPending.value"
          data-testid="cases-diary-save"
        >
          {{ row.diary ? t('cases.diary.update') : t('cases.diary.set') }}
        </button>
        <button
          v-if="row.diary"
          key="clear"
          type="button"
          class="btn btn-sm"
          :disabled="clear.isPending.value"
          data-testid="cases-diary-clear"
          @click="clear.mutate()"
        >
          {{ t('cases.diary.clear') }}
        </button>
      </div>
    </form>
  </section>
</template>
