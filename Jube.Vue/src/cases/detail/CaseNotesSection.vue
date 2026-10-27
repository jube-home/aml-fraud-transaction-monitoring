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
import { useI18n } from 'vue-i18n'
import { useFormat } from '../../composables/useFormat'
import { plainText } from './text'
import type { CaseNoteEntry } from './types'

defineProps<{ notes: CaseNoteEntry[]; loading: boolean; failed: boolean }>()

const { t } = useI18n()
const { dateTime } = useFormat()
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="case-notes">
    <div v-if="loading" key="loading" class="skeleton h-24 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <div v-else-if="failed" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.notes.failed') }}</span>
    </div>
    <p
      v-else-if="notes.length === 0"
      key="empty"
      class="text-sm text-base-content/70"
      role="status"
    >
      {{ t('caseDetail.notes.empty') }}
    </p>
    <ul v-else key="list" class="list rounded-box border border-base-300 bg-base-100">
      <li v-for="note in notes" :key="note.id" class="list-row">
        <div class="list-col-grow flex flex-col gap-1">
          <span class="text-sm" data-testid="case-note-text">{{ plainText(note.note) }}</span>
          <span class="flex flex-wrap gap-x-3 font-mono text-xs text-base-content/70">
            <span>{{ note.createdUser }}</span>
            <span>{{ dateTime(note.createdDate) }}</span>
            <span v-if="note.action">{{
              t('caseDetail.notes.action', { name: note.action })
            }}</span>
            <span v-if="note.priority">
              {{ t('caseDetail.notes.priority', { name: note.priority }) }}
            </span>
          </span>
        </div>
      </li>
    </ul>
  </section>
</template>
