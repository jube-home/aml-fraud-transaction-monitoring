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
import {researchHost} from './researchSources'
import {safeAddress} from './noteGroups'
import {plainText} from './text'
import type {CaseResearchEntry} from './types'

const props = defineProps<{ research: CaseResearchEntry }>()

const {t} = useI18n()
const {date, dateTime} = useFormat()

const sourceLabel = () =>
  props.research.sourceId === 2
    ? t('caseDetail.notes.source.askJooby')
    : t('caseDetail.notes.source.user')

const avatarNote = () => ({
  id: props.research.id,
  caseId: props.research.caseId,
  createdDate: props.research.createdDate ?? '',
  createdUser: props.research.createdUser,
  note: null,
  action: null,
  priority: null,
  sourceId: props.research.sourceId === 2 ? 3 : 1,
})
</script>

<template>
  <li class="list-row" data-testid="case-research">
    <NoteAvatar :note="avatarNote()"/>
    <div class="list-col-grow flex min-w-0 flex-col gap-2">
      <h4 class="text-sm font-semibold" data-testid="case-research-topic-text">
        {{ plainText(research.topic) }}
      </h4>
      <p class="whitespace-pre-line text-sm" data-testid="case-research-findings-text">
        {{ plainText(research.findings) }}
      </p>
      <div
        v-if="research.sources.length > 0"
        class="flex flex-col gap-1"
        data-testid="case-research-sources"
      >
        <span class="text-xs font-medium">{{ t('caseDetail.research.sources') }}</span>
        <ul class="flex flex-col gap-0.5">
          <li
            v-for="source in research.sources"
            :key="source.id"
            class="flex flex-wrap items-baseline gap-x-2 text-xs"
            data-testid="case-research-source-row"
          >
            <a
              v-if="safeAddress(source.url)"
              :href="safeAddress(source.url) ?? undefined"
              class="link break-all"
              target="_blank"
              rel="noopener noreferrer nofollow"
            >{{ source.title || source.url }}</a
            >
            <span v-else class="break-all">{{ source.title || source.url }}</span>
            <span class="font-mono text-base-content/70" data-testid="case-research-source-host">{{
                researchHost(source.url)
              }}</span>
            <span v-if="source.retrievedDate" class="font-mono text-base-content/70">{{
                t('caseDetail.research.retrieved', {date: date(source.retrievedDate)})
              }}</span>
          </li>
        </ul>
      </div>
      <CaseNoteFiles v-if="research.files.length > 0" :files="research.files"/>
      <span
        class="flex flex-wrap items-center gap-x-3 gap-y-1 font-mono text-xs text-base-content/70"
      >
        <span class="badge badge-ghost badge-sm" data-testid="case-research-source">{{
            sourceLabel()
          }}</span>
        <span>{{ research.createdUser }}</span>
        <span>{{ dateTime(research.createdDate ?? '') }}</span>
      </span>
    </div>
  </li>
</template>
