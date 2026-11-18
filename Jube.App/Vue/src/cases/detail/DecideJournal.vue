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
import {onBeforeUnmount, onMounted, useTemplateRef} from 'vue'
import {useI18n} from 'vue-i18n'
import {ScrollText} from 'lucide-vue-next'
import {journalLive, journalTagsHost, journalViewsHost} from '../journal/decideHost'
import DecideCard from './DecideCard.vue'

const {t} = useI18n()

const tags = useTemplateRef<HTMLElement>('tags')
const views = useTemplateRef<HTMLElement>('views')

onMounted(() => {
  journalTagsHost.value = tags.value
  journalViewsHost.value = views.value
})

onBeforeUnmount(() => {
  if (journalTagsHost.value === tags.value) journalTagsHost.value = null
  if (journalViewsHost.value === views.value) journalViewsHost.value = null
})
</script>

<template>
  <div v-show="journalLive" class="min-w-0" data-testid="case-decide-journal-shell">
    <DecideCard :title="t('caseDetail.section.journal')" testid="case-decide-journal">
      <template #icon>
        <ScrollText class="size-5"/>
      </template>
      <div
        ref="tags"
        class="flex min-w-0 flex-wrap items-center gap-2"
        data-testid="case-decide-journal-tags"
      ></div>
      <div
        ref="views"
        class="flex min-w-0 flex-wrap items-center gap-2"
        data-testid="case-decide-journal-views"
      ></div>
    </DecideCard>
  </div>
</template>
