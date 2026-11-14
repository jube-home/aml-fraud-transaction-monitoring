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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import CaseNarrativeCard from './CaseNarrativeCard.vue'
import CaseNoteItem from './CaseNoteItem.vue'
import {isOwnNote, narrativeChains, timelineNotes} from './noteGroups'
import type {CaseNoteEntry} from './types'

const props = defineProps<{
  notes: CaseNoteEntry[]
  loading: boolean
  failed: boolean
  userName: string | undefined
}>()

const {t} = useI18n()

const chains = computed(() => narrativeChains(props.notes))
const timeline = computed(() => timelineNotes(props.notes))
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="case-notes">
    <div v-if="loading" key="loading" class="skeleton h-24 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <div v-else-if="failed" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.notes.failed') }}</span>
    </div>
    <template v-else>
      <h3 class="text-sm font-semibold">{{ t('caseDetail.notes.heading') }}</h3>
      <CaseNarrativeCard v-for="chain in chains" :key="chain.caseId" :chain="chain"/>
      <p
        v-if="timeline.length === 0"
        key="empty"
        class="text-sm text-base-content/70"
        role="status"
      >
        {{ t('caseDetail.notes.empty') }}
      </p>
      <ul v-else key="list" class="list rounded-box border border-base-300 bg-base-100">
        <CaseNoteItem
          v-for="note in timeline"
          :key="note.id"
          :note="note"
          :own="isOwnNote(note, userName)"
        />
      </ul>
    </template>
  </section>
</template>
