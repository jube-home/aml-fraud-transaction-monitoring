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
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {Flag} from 'lucide-vue-next'
import {useToasts} from '../../stores/toasts'
import {caseDetailApi} from './api'
import DecideCard from './DecideCard.vue'
import {isOwnNote, timelineNotes} from './noteGroups'
import {operationsApi} from './operationsApi'
import {plainText} from './text'
import type {CaseDetail, CaseNoteEntry} from './types'

const props = defineProps<{ detail: CaseDetail; userName: string }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const key = computed(() => props.detail.caseKey ?? '')
const keyValue = computed(() => props.detail.caseKeyValue ?? '')

const notes = useQuery({
  queryKey: computed(() => ['service', 'Case', 'notes', key.value, keyValue.value] as const),
  queryFn: () => caseDetailApi.notes(key.value, keyValue.value),
  enabled: computed(() => key.value !== '' && keyValue.value !== ''),
  staleTime: 30_000,
  retry: false,
})

const own = computed(() =>
  timelineNotes(notes.data.value ?? []).filter((note) => isOwnNote(note, props.userName)),
)

const finding = useMutation({
  mutationFn: (note: CaseNoteEntry) => operationsApi.setFinding(note.id, !note.isFinding),
  onSuccess: () => client.invalidateQueries({queryKey: ['service', 'Case', 'notes']}),
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <DecideCard
    v-if="own.length > 0"
    :title="t('caseDetail.decide.researchFindings')"
    testid="case-decide-findings"
  >
    <template #icon>
      <Flag class="size-5"/>
    </template>
    <template #status>
      <span class="badge badge-ghost badge-sm tabular-nums">
        {{ own.filter((note) => note.isFinding).length }}
      </span>
    </template>
    <ul class="flex min-w-0 flex-col gap-2">
      <li
        v-for="note in own"
        :key="note.id"
        class="flex min-w-0 items-start gap-2 rounded-field bg-base-100 p-2"
        data-testid="case-decide-finding"
      >
        <span class="line-clamp-2 min-w-0 grow text-sm break-words">
          {{ plainText(note.note) }}
        </span>
        <button
          type="button"
          class="btn btn-xs shrink-0"
          :aria-pressed="note.isFinding === true"
          :disabled="finding.isPending.value"
          data-testid="case-note-finding-toggle"
          @click="finding.mutate(note)"
        >
          {{
            note.isFinding ? t('caseDetail.notes.unmarkFinding') : t('caseDetail.notes.markFinding')
          }}
        </button>
      </li>
    </ul>
  </DecideCard>
</template>
