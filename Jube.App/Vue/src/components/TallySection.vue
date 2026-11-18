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
import {computed, useId} from 'vue'
import {useI18n} from 'vue-i18n'
import {useFormat} from '../composables/useFormat'

const props = defineProps<{
  label: string
  count: number
  loading: boolean
  failed: boolean
  failedText: string
  testid: string
}>()

const {t} = useI18n()
const {number} = useFormat()
const headingId = useId()

const filled = computed(() => !props.loading && !props.failed && props.count > 0)
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2"
    :class="filled || failed ? 'basis-full' : ''"
    :aria-labelledby="headingId"
    :data-testid="testid"
  >
    <div class="flex min-w-0 items-center gap-2">
      <span class="flex shrink-0 text-base-content/70" aria-hidden="true">
        <slot name="icon"/>
      </span>
      <h4
        :id="headingId"
        class="text-sm font-semibold"
        :class="filled ? '' : 'text-base-content/70'"
      >
        {{ label }}
      </h4>
      <span v-if="loading" key="loading" role="status" class="flex items-center">
        <span class="loading loading-dots loading-xs" aria-hidden="true"></span>
        <span class="sr-only">{{ t('cases.loading') }}</span>
      </span>
      <span
        v-else-if="!failed"
        key="count"
        class="badge badge-ghost badge-sm tabular-nums"
        :data-testid="`${testid}-count`"
      >
        {{ number(count) }}
      </span>
    </div>
    <div v-if="failed" key="failed" role="alert" class="alert alert-error">
      <span>{{ failedText }}</span>
    </div>
    <slot v-else-if="filled" key="body"/>
  </section>
</template>
