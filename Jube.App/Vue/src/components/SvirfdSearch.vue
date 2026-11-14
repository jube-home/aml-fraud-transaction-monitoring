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
import {computed, ref, useId, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {describeApiError} from '../api/problems'
import {ApiError} from '../api/useApi'
import {queryKeys} from '../api/queryKeys'
import {svirfdApi} from '../svirfd/api'
import {canLookup} from '../svirfd/validate'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'
import SvirfdRecord from './svirfd/SvirfdRecord.vue'

const props = withDefaults(
  defineProps<{ ip?: string; autoRun?: boolean; remoteBlocked?: boolean }>(),
  {
    ip: '',
    autoRun: false,
    remoteBlocked: false,
  },
)

const emit = defineEmits<{ search: [ip: string] }>()

const {t} = useI18n()
const uid = useId()

const draft = ref(props.ip)
const submitted = ref<string | undefined>(
  props.autoRun && canLookup(props.ip) ? props.ip.trim() : undefined,
)

watch(
  () => props.ip,
  (value) => {
    draft.value = value

    if (props.autoRun && canLookup(value)) submitted.value = value.trim()
  },
)

function commit(): void {
  if (!canLookup(draft.value)) return

  submitted.value = draft.value.trim()
  emit('search', submitted.value)
}

const lookup = useQuery({
  queryKey: computed(() => queryKeys.svirfdLookup(submitted.value ?? null)),
  queryFn: () => svirfdApi.lookup(submitted.value!),
  enabled: computed(() => submitted.value !== undefined && !props.remoteBlocked),
  retry: false,
})

const running = computed(() => lookup.isFetching.value)
const invalid = computed(() => draft.value.trim() !== '' && !canLookup(draft.value))

const reasons = computed(() => {
  const error = lookup.error.value

  if (!error) return []

  if (error instanceof ApiError && error.status === 429) return [t('svirfd.quota')]
  if (error instanceof ApiError && error.status === 503) return [t('svirfd.indexLoading')]
  if (error instanceof ApiError && error.status === 404) return [t('svirfd.notFound')]
  if (error instanceof ApiError && error.status === 400) return [t('svirfd.invalidIp')]

  return describeApiError(error, {failed: t('svirfd.failed'), refused: t('svirfd.forbidden')})
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="svirfd-search">
    <PanelCard :title="t('svirfd.title')" :level="2">
      <form class="flex flex-col gap-3" @submit.prevent="commit">
        <fieldset class="fieldset">
          <label class="label" :for="`${uid}-ip`">{{ t('svirfd.ipLabel') }}</label>
          <input
            :id="`${uid}-ip`"
            v-model="draft"
            type="text"
            inputmode="text"
            class="input input-sm w-full max-w-xl font-mono"
            autocomplete="off"
            spellcheck="false"
            :placeholder="t('svirfd.ipPlaceholder')"
            :aria-invalid="invalid"
            data-testid="svirfd-text"
          />
          <p :class="['label', invalid ? 'text-error' : '']">
            {{ invalid ? t('svirfd.invalidIp') : t('svirfd.ipHint') }}
          </p>
        </fieldset>
        <div>
          <button
            type="submit"
            class="btn btn-sm btn-primary"
            :disabled="!canLookup(draft) || running || remoteBlocked"
            data-testid="svirfd-check"
          >
            <span v-if="running" key="busy" class="loading loading-spinner loading-xs"></span>
            {{ running ? t('svirfd.checking') : t('svirfd.check') }}
          </button>
        </div>
      </form>
    </PanelCard>

    <div class="flex min-w-0 flex-col gap-4" aria-live="polite" data-testid="svirfd-results">
      <ProblemAlert
        v-if="lookup.isError.value"
        key="failed"
        :reasons="reasons"
        data-testid="svirfd-error"
      />
      <p
        v-else-if="!lookup.data.value && !running"
        key="prompt"
        class="text-sm text-secondary"
        data-testid="svirfd-prompt"
      >
        {{ t('svirfd.prompt') }}
      </p>
      <SvirfdRecord
        v-else-if="lookup.data.value"
        key="record"
        :record="lookup.data.value.record"
        :rtt-chart-svg="lookup.data.value.rttChartSvg"
        :headline="lookup.data.value.headline"
        :narrative="lookup.data.value.narrative"
        :attribution="lookup.data.value.attribution"
        :licence-url="lookup.data.value.licenceUrl"
        :mode="lookup.data.value.mode"
        explain-empty
      />
    </div>
  </div>
</template>
