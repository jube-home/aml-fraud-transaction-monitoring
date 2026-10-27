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
import { useQueryClient } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import DataGrid from '../components/DataGrid.vue'
import type { GridActionEvent } from '../components/dataGridTypes'
import { useFormat, intlTag } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { filenameFromDisposition, saveBlob } from '../download/file'
import { useToasts } from '../stores/toasts'
import { snapshotApi } from './api'
import { formatBytes, snapshotColumns, snapshotRows } from './state'
import type { Snapshot } from './types'

const props = defineProps<{ snapshots: Snapshot[] }>()

const emit = defineEmits<{ compare: [id: number]; restore: [id: number] }>()

const { t, locale } = useI18n()
const format = useFormat()
const toasts = useToasts()
const queryClient = useQueryClient()
const basePagerLabels = usePagerLabels()

const pagerLabels = computed(() => ({
  ...basePagerLabels.value,
  navigation: `${basePagerLabels.value.navigation}: ${t('preservation.snapshots.title')}`,
}))

const dialog = ref<HTMLDialogElement>()
const removing = ref<number | null>(null)

const direction = computed(() => (locale.value === 'ar' ? 'rtl' : 'ltr'))

const labels = computed(() => ({
  preservationPage: t('preservation.snapshots.sourcePage'),
  modelSync: t('preservation.snapshots.sourceModelSync'),
  askJooby: t('preservation.snapshots.sourceAskJooby'),
  beforeImport: t('preservation.snapshots.sourceBeforeImport'),
  ready: t('preservation.snapshots.ready'),
  failed: t('preservation.snapshots.failed'),
  unnamed: t('preservation.snapshots.unnamed'),
}))

const rows = computed(() =>
  snapshotRows(props.snapshots, labels.value, {
    date: format.dateTime,
    number: format.number,
    bytes: (value) => formatBytes(value, intlTag(locale.value)),
  }),
)

const columns = computed(() =>
  snapshotColumns({
    name: t('preservation.snapshots.name'),
    source: t('preservation.snapshots.source'),
    taken: t('preservation.snapshots.taken'),
    by: t('preservation.snapshots.by'),
    models: t('preservation.snapshots.models'),
    size: t('preservation.snapshots.size'),
    state: t('preservation.snapshots.state'),
    actions: t('preservation.snapshots.actions'),
    compare: t('preservation.snapshots.compare'),
    restore: t('preservation.snapshots.restore'),
    download: t('preservation.snapshots.download'),
    remove: t('preservation.snapshots.remove'),
    failed: t('preservation.snapshots.failed'),
    ready: t('preservation.snapshots.ready'),
  }),
)

const removingName = computed(() => rows.value.find((row) => row.id === removing.value)?.name ?? '')

async function download(id: number): Promise<void> {
  try {
    const { blob, disposition } = await snapshotApi.body(id)

    saveBlob(blob, filenameFromDisposition(disposition, `snapshot-${id}.json`))
  } catch {
    toasts.error(t('preservation.snapshots.downloadFailed'))
  }
}

async function confirmRemove(): Promise<void> {
  const id = removing.value

  removing.value = null

  if (id === null) return

  try {
    await snapshotApi.remove(id)
    await queryClient.invalidateQueries({ queryKey: queryKeys.preservationSnapshots() })
    toasts.success(t('preservation.snapshots.removed'))
  } catch (error) {
    toasts.apiError(error)
  }
}

function act(event: GridActionEvent): void {
  const id = (event.row as { id: number }).id

  if (event.id === 'compare') emit('compare', id)
  else if (event.id === 'restore') emit('restore', id)
  else if (event.id === 'download') void download(id)
  else if (event.id === 'remove') removing.value = id
}

watch(removing, (id) => {
  if (id !== null) dialog.value?.showModal()
  else dialog.value?.close()
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="preservation-snapshots">
    <DataGrid
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('preservation.snapshots.empty')"
      :label="t('preservation.snapshots.title')"
      :page-size="10"
      :pager-labels="pagerLabels"
      auto-height
      @action="act"
    />

    <dialog ref="dialog" class="modal" data-testid="snapshot-confirm" @close="removing = null">
      <div class="modal-box">
        <h3 class="text-lg font-bold">{{ t('preservation.snapshots.confirmTitle') }}</h3>
        <p class="py-4">
          {{ t('preservation.snapshots.confirmBody') }}
          <span class="badge badge-sm font-mono">{{ removingName }}</span>
        </p>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="snapshot-confirm-no"
            @click="removing = null"
          >
            {{ t('preservation.snapshots.keep') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-error"
            data-testid="snapshot-confirm-yes"
            @click="confirmRemove"
          >
            {{ t('preservation.snapshots.remove') }}
          </button>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button type="submit" tabindex="-1">{{ t('preservation.snapshots.keep') }}</button>
      </form>
    </dialog>
  </div>
</template>
