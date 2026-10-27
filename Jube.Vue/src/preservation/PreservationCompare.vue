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
import { ArrowRight, GitCompare } from 'lucide-vue-next'
import PreservationDiffView from './PreservationDiffView.vue'
import { snapshotApi } from './api'
import { compareReady, liveValue, sideId } from './state'
import type { Diff, Snapshot } from './types'

const props = defineProps<{
  snapshots: Snapshot[]
  left: string
  right: string
  run: number
}>()

const emit = defineEmits<{ 'update:left': [value: string]; 'update:right': [value: string] }>()

const { t } = useI18n()

const diff = ref<Diff>()
const busy = ref(false)
const failed = ref(false)

const ready = computed(() => compareReady(props.left, props.right))

const usable = computed(() => props.snapshots.filter((snapshot) => !snapshot.inError))

function name(snapshot: Snapshot): string {
  return snapshot.name?.trim()
    ? snapshot.name
    : `${t('preservation.snapshots.unnamed')} ${snapshot.id}`
}

async function compare(): Promise<void> {
  if (!ready.value) return

  busy.value = true
  failed.value = false

  try {
    diff.value = await snapshotApi.diff(sideId(props.left), sideId(props.right))
  } catch {
    diff.value = undefined
    failed.value = true
  } finally {
    busy.value = false
  }
}

watch(
  () => props.run,
  (next) => {
    if (next > 0) void compare()
  },
)

watch(
  () => [props.left, props.right],
  () => {
    diff.value = undefined
    failed.value = false
  },
)
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-compare">
    <p class="text-sm text-secondary">{{ t('preservation.compare.intro') }}</p>

    <div class="flex flex-wrap items-end gap-3">
      <label class="flex min-w-0 flex-col gap-1">
        <span class="label text-xs">{{ t('preservation.compare.from') }}</span>
        <select
          class="select select-sm w-64 max-w-full"
          :value="left"
          data-testid="compare-left"
          @change="emit('update:left', ($event.target as HTMLSelectElement).value)"
        >
          <option :value="liveValue">{{ t('preservation.compare.live') }}</option>
          <option v-for="snapshot in usable" :key="snapshot.id" :value="String(snapshot.id)">
            {{ name(snapshot) }}
          </option>
        </select>
      </label>
      <label class="flex min-w-0 flex-col gap-1">
        <span class="label text-xs">{{ t('preservation.compare.to') }}</span>
        <select
          class="select select-sm w-64 max-w-full"
          :value="right"
          data-testid="compare-right"
          @change="emit('update:right', ($event.target as HTMLSelectElement).value)"
        >
          <option :value="liveValue">{{ t('preservation.compare.live') }}</option>
          <option v-for="snapshot in usable" :key="snapshot.id" :value="String(snapshot.id)">
            {{ name(snapshot) }}
          </option>
        </select>
      </label>
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!ready || busy"
        :aria-busy="busy"
        data-testid="compare-run"
        @click="compare"
      >
        <span
          v-if="busy"
          key="busy"
          class="loading loading-spinner loading-xs"
          aria-hidden="true"
        ></span>
        <GitCompare v-else key="icon" aria-hidden="true" class="size-4" />
        {{ t('preservation.compare.run') }}
      </button>
    </div>

    <p v-if="!ready" key="same" class="text-sm text-secondary" data-testid="compare-same">
      {{ t('preservation.compare.same') }}
    </p>

    <p
      v-if="failed"
      key="failed"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="compare-failed"
    >
      {{ t('preservation.compare.failed') }}
    </p>

    <template v-if="diff">
      <p class="text-sm" data-testid="compare-heading">
        <span class="badge badge-sm font-mono">{{ diff.left.label }}</span>
        <ArrowRight aria-hidden="true" class="inline size-4 rtl:rotate-180" />
        <span class="badge badge-sm font-mono">{{ diff.right.label }}</span>
        <span v-if="diff.id > 0" key="recorded" class="ms-2 text-secondary">{{
          t('preservation.compare.recorded', { id: diff.id })
        }}</span>
      </p>
      <PreservationDiffView :diff="diff" :caption="t('preservation.compare.title')" />
    </template>
  </div>
</template>
