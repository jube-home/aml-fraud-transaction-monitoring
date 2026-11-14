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
import {useI18n} from 'vue-i18n'
import {useFormat} from '../../composables/useFormat'
import CaseNoteFiles from './CaseNoteFiles.vue'
import NoteAvatar from './NoteAvatar.vue'
import type {NarrativeChain} from './noteGroups'
import {plainText} from './text'

defineProps<{ chain: NarrativeChain }>()

const {t} = useI18n()
const {dateTime} = useFormat()
</script>

<template>
  <article
    class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-3"
    data-testid="case-narrative"
  >
    <header class="flex flex-wrap items-center gap-2">
      <NoteAvatar :note="chain.latest"/>
      <h4 class="text-sm font-semibold">{{ t('caseDetail.notes.narrative.title') }}</h4>
      <span class="badge badge-ghost badge-sm" data-testid="case-narrative-version">{{
          t('caseDetail.notes.narrative.version', {n: chain.latest.version ?? 1})
        }}</span>
      <span class="font-mono text-xs text-base-content/70">{{
          t('caseDetail.notes.narrative.written', {date: dateTime(chain.latest.createdDate)})
        }}</span>
      <span
        v-if="chain.latest.askJoobyConversationId"
        class="font-mono text-xs text-base-content/70"
        data-testid="case-narrative-run"
      >{{ t('caseDetail.notes.narrative.run', {n: chain.latest.askJoobyConversationId}) }}</span
      >
    </header>
    <p class="whitespace-pre-line text-sm" data-testid="case-narrative-text">
      {{ plainText(chain.latest.note) }}
    </p>
    <CaseNoteFiles v-if="(chain.latest.files ?? []).length > 0" :files="chain.latest.files ?? []"/>
    <p class="text-xs text-base-content/70">{{ t('caseDetail.notes.narrative.readOnly') }}</p>
    <details v-if="chain.earlier.length > 0" class="collapse border border-base-300 bg-base-100">
      <summary class="collapse-title min-h-0 py-2 text-sm font-medium">
        {{ t('caseDetail.notes.narrative.earlier', {n: chain.earlier.length}) }}
      </summary>
      <div class="collapse-content flex flex-col gap-3">
        <section
          v-for="note in chain.earlier"
          :key="note.id"
          class="flex flex-col gap-1 border-t border-base-300 pt-2"
          data-testid="case-narrative-earlier"
        >
          <span class="flex gap-3 font-mono text-xs text-base-content/70">
            <span>{{ t('caseDetail.notes.narrative.version', {n: note.version ?? 1}) }}</span>
            <span>{{ dateTime(note.createdDate) }}</span>
          </span>
          <p class="whitespace-pre-line text-sm">{{ plainText(note.note) }}</p>
        </section>
      </div>
    </details>
  </article>
</template>
