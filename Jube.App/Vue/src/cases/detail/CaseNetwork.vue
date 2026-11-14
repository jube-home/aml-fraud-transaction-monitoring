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
import {computed, ref} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import GraphExplorer from '../../graph/GraphExplorer.vue'
import {daysOfWindow, nearestDays, windowFromDays} from '../../graph/state'
import type {GraphWindow} from '../../graph/types'
import {mergeJournalState, parseJournalState, windowDays} from '../journal/state'

const props = defineProps<{
  registryGuid: string
  caseId: number
  caseKey: string
  caseKeyValue: string
  workflow: string
}>()

const route = useRoute()
const router = useRouter()

const now = ref(new Date())

const journal = computed(() => parseJournalState(route.query))

const window = computed<GraphWindow>(() => windowFromDays(journal.value.days, now.value))

const selection = computed(() => journal.value.transaction)

function onWindow(next: GraphWindow): void {
  const days = nearestDays(daysOfWindow(next), windowDays)

  if (days === journal.value.days) return

  void router.replace({
    query: mergeJournalState(route.query, {...journal.value, days, transaction: null}),
  })
}

function scrollToJournal(): void {
  document.getElementById('journal')?.scrollIntoView({block: 'start'})
}

function onFilterJournal(archiveIds: number[]): void {
  void router.replace({
    query: mergeJournalState(route.query, {
      ...journal.value,
      archiveIds: archiveIds.slice(0, 500),
      transaction: null,
    }),
  })
  scrollToJournal()
}

function onPivot(target: { key: string; value: string }): void {
  const pivot = target.key === props.caseKey && target.value === props.caseKeyValue ? null : target

  void router.replace({
    query: mergeJournalState(route.query, {...journal.value, pivot, transaction: null}),
  })
  scrollToJournal()
}

function onSelection(archiveId: number | null): void {
  void router.replace({
    query: mergeJournalState(route.query, {...journal.value, transaction: archiveId}),
  })
}

function onOpenCase(caseId: number): void {
  if (caseId === props.caseId) return

  void router.push({name: 'caseDetail', params: {caseId}, query: {section: 'network'}})
}
</script>

<template>
  <div data-testid="case-network">
    <GraphExplorer
      :registry-guid="registryGuid"
      :seed-key="caseKey"
      :seed-value="caseKeyValue"
      embedded
      :case-id="caseId"
      :workflow="workflow"
      :window="window"
      :selection="selection"
      @update:window="onWindow"
      @update:selection="onSelection"
      @filter-journal="onFilterJournal"
      @pivot="onPivot"
      @open-case="onOpenCase"
    />
  </div>
</template>
