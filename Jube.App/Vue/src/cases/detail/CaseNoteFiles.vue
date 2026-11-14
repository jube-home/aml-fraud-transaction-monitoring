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
import {Download, Paperclip} from 'lucide-vue-next'
import {useI18n} from 'vue-i18n'
import {useMutation} from '@tanstack/vue-query'
import {useApiDownload} from '../../api/useApi'
import {useFormat} from '../../composables/useFormat'
import {filenameFromDisposition, saveBlob} from '../../download/file'
import {useToasts} from '../../stores/toasts'
import type {CaseFileEntry} from './types'

defineProps<{ files: CaseFileEntry[] }>()

const {t} = useI18n()
const toasts = useToasts()
const {dateTime} = useFormat()

const download = useMutation({
  mutationFn: async (file: CaseFileEntry) => {
    const result = await useApiDownload('/api/CaseFile', {query: {id: file.id}})

    saveBlob(result.blob, filenameFromDisposition(result.disposition, file.name ?? 'file'))
  },
  onError: (error) => toasts.apiError(error),
})

function kilobytes(size: number): string {
  return t('caseDetail.notes.fileSize', {n: Math.max(1, Math.round(size / 1024))})
}
</script>

<template>
  <ul class="flex flex-col gap-1" data-testid="case-note-files">
    <li
      v-for="file in files"
      :key="file.id"
      class="flex min-w-0 flex-wrap items-center gap-2 text-sm"
    >
      <Paperclip class="size-4 shrink-0" aria-hidden="true"/>
      <span class="min-w-0 truncate" data-testid="case-note-file-name">{{ file.name }}</span>
      <span class="font-mono text-xs text-base-content/70">{{ kilobytes(file.size) }}</span>
      <span class="font-mono text-xs text-base-content/70" data-testid="case-note-file-type">{{
          file.contentType
        }}</span>
      <span class="font-mono text-xs text-base-content/70">{{ file.createdUser }}</span>
      <span class="font-mono text-xs text-base-content/70">{{
          dateTime(file.createdDate ?? '')
        }}</span>
      <button
        type="button"
        class="btn btn-ghost btn-xs btn-square"
        :aria-label="t('caseDetail.notes.download', { name: file.name ?? '' })"
        :disabled="download.isPending.value"
        data-testid="case-note-file-download"
        @click="download.mutate(file)"
      >
        <Download class="size-4" aria-hidden="true"/>
      </button>
    </li>
  </ul>
</template>
