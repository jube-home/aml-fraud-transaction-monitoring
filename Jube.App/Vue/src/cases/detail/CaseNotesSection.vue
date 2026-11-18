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
import {StickyNote} from 'lucide-vue-next'
import TallySection from '../../components/TallySection.vue'
import CaseNarrativeCard from './CaseNarrativeCard.vue'
import CaseNoteItem from './CaseNoteItem.vue'
import {narrativeChains, timelineNotes} from './noteGroups'
import type {CaseNoteEntry} from './types'

const props = defineProps<{
  notes: CaseNoteEntry[]
  loading: boolean
  failed: boolean
}>()

const {t} = useI18n()

const chains = computed(() => narrativeChains(props.notes))
const timeline = computed(() => timelineNotes(props.notes))
</script>

<template>
  <TallySection
    :label="t('caseDetail.notes.heading')"
    :count="chains.length + timeline.length"
    :loading="loading"
    :failed="failed"
    :failed-text="t('caseDetail.notes.failed')"
    testid="case-notes"
  >
    <template #icon>
      <StickyNote class="size-4"/>
    </template>
    <CaseNarrativeCard v-for="chain in chains" :key="chain.caseId" :chain="chain"/>
    <ul
      v-if="timeline.length > 0"
      key="list"
      class="list rounded-box border border-base-300 bg-base-100"
    >
      <CaseNoteItem v-for="note in timeline" :key="note.id" :note="note"/>
    </ul>
  </TallySection>
</template>
