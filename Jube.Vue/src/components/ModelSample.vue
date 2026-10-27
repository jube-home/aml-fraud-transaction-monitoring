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
import { ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { ApiError } from '../api/useApi'
import { describeApiError } from '../api/problems'
import { defaultDraft, percent, sampleProblems, type SampleProblem } from '../sample/model'
import { sampleApi } from '../sample/api'
import DownloadButton from './DownloadButton.vue'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(defineProps<{ modelGuid: string | null; allowed?: boolean }>(), {
  allowed: true,
})

const { t } = useI18n()
const id = useId()

const draft = ref(defaultDraft())
const problems = ref<string[]>([])
const saved = ref('')

function describe(code: SampleProblem): string {
  switch (code) {
    case 'model':
      return t('sample.problem.model')
    case 'from':
      return t('sample.problem.from')
    case 'to':
      return t('sample.problem.to')
    case 'order':
      return t('sample.problem.order')
    default:
      return t('sample.problem.sample')
  }
}

function check(): boolean {
  const found = sampleProblems({ ...draft.value, modelGuid: props.modelGuid ?? '' })

  saved.value = ''
  problems.value = found.map(describe)

  return found.length === 0
}

const fetcher = () => sampleApi.download({ ...draft.value, modelGuid: props.modelGuid ?? '' })

function failed(error: unknown): void {
  problems.value =
    error instanceof ApiError
      ? describeApiError(error, { failed: t('sample.failed'), refused: t('sample.refused') })
      : [t('sample.empty')]
}

function done(filename: string): void {
  problems.value = []
  saved.value = filename
}
</script>

<template>
  <PanelCard :title="t('sample.title')" compact>
    <form class="flex min-w-0 flex-col gap-4" data-testid="model-sample" @submit.prevent>
      <p class="text-sm text-secondary">{{ t('sample.hint') }}</p>

      <div class="grid min-w-0 gap-4 @container sm:grid-cols-2">
        <label class="flex flex-col gap-1" :for="`${id}-from`">
          <span class="label text-xs">{{ t('sample.from') }}</span>
          <input
            :id="`${id}-from`"
            v-model="draft.from"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="sample-from"
          />
        </label>
        <label class="flex flex-col gap-1" :for="`${id}-to`">
          <span class="label text-xs">{{ t('sample.to') }}</span>
          <input
            :id="`${id}-to`"
            v-model="draft.to"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="sample-to"
          />
        </label>

        <label class="flex flex-col gap-1 sm:col-span-2" :for="`${id}-rate`">
          <span class="label text-xs">
            {{ t('sample.rate') }}
            <span class="font-mono tabular-nums" data-testid="sample-percent">{{
              percent(draft.sample)
            }}</span>
          </span>
          <input
            :id="`${id}-rate`"
            v-model.number="draft.sample"
            type="range"
            min="0"
            max="1"
            step="0.01"
            class="range range-xs w-full max-w-sm"
            data-testid="sample-rate"
          />
          <span class="text-xs text-secondary">{{ t('sample.rateHint') }}</span>
        </label>
      </div>

      <ProblemAlert
        v-if="problems.length > 0"
        key="problems"
        :reasons="problems"
        data-testid="sample-problems"
      />
      <p v-else-if="saved" key="saved" class="text-sm" role="status" data-testid="sample-saved">
        {{ t('sample.saved', { name: saved }) }}
      </p>

      <div>
        <DownloadButton
          :label="t('sample.download')"
          :fetcher="fetcher"
          :guard="check"
          fallback-name="sample.csv"
          :disabled="!allowed"
          @saved="done"
          @failed="failed"
        />
      </div>
    </form>
  </PanelCard>
</template>
