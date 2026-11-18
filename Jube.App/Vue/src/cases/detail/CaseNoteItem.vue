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
import {Flag} from 'lucide-vue-next'
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useFormat} from '../../composables/useFormat'
import CaseNoteFiles from './CaseNoteFiles.vue'
import NoteAvatar from './NoteAvatar.vue'
import {noteSource, sourceOf} from './noteGroups'
import {plainText} from './text'
import type {CaseNoteEntry} from './types'

const props = defineProps<{ note: CaseNoteEntry }>()

const {t} = useI18n()
const {dateTime} = useFormat()

const source = computed(() => sourceOf(props.note))

const sourceLabel = computed(() =>
  source.value === noteSource.askJooby
    ? t('caseDetail.notes.source.askJooby')
    : source.value === noteSource.system
      ? t('caseDetail.notes.source.system')
      : t('caseDetail.notes.source.user'),
)
</script>

<template>
  <li class="list-row" data-testid="case-note">
    <NoteAvatar :note="note"/>
    <div class="list-col-grow flex min-w-0 flex-col gap-1">
      <span class="text-sm" data-testid="case-note-text">{{ plainText(note.note) }}</span>
      <CaseNoteFiles v-if="(note.files ?? []).length > 0" :files="note.files ?? []"/>
      <span
        class="flex flex-wrap items-center gap-x-3 gap-y-1 font-mono text-xs text-base-content/70"
      >
        <span class="badge badge-ghost badge-sm" data-testid="case-note-source">{{
            sourceLabel
          }}</span>
        <span
          v-if="note.isFinding"
          class="badge badge-soft badge-sm"
          data-testid="case-note-finding"
        >
          <Flag class="size-3" aria-hidden="true"/>
          {{ t('caseDetail.notes.finding') }}
        </span>
        <span>{{ note.createdUser }}</span>
        <span>{{ dateTime(note.createdDate) }}</span>
        <span v-if="note.action">{{ t('caseDetail.notes.action', {name: note.action}) }}</span>
        <span v-if="note.action && note.priority">{{
            t('caseDetail.notes.priority', {name: note.priority})
          }}</span>
      </span>
    </div>
  </li>
</template>
