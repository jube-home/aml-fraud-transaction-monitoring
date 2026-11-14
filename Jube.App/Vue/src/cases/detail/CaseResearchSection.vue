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
import CaseResearchItem from './CaseResearchItem.vue'
import type {CaseResearchEntry} from './types'

defineProps<{ research: CaseResearchEntry[]; loading: boolean; failed: boolean }>()

const {t} = useI18n()
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2"
    aria-labelledby="case-research-heading"
    data-testid="case-research-section"
  >
    <h3 id="case-research-heading" class="text-sm font-semibold">
      {{ t('caseDetail.research.heading') }}
    </h3>
    <div v-if="loading" key="loading" class="skeleton h-16 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <div v-else-if="failed" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.research.failed') }}</span>
    </div>
    <p
      v-else-if="research.length === 0"
      key="empty"
      class="text-sm text-base-content/70"
      role="status"
    >
      {{ t('caseDetail.research.empty') }}
    </p>
    <ul v-else key="list" class="list rounded-box border border-base-300 bg-base-100">
      <CaseResearchItem v-for="entry in research" :key="entry.id" :research="entry"/>
    </ul>
  </section>
</template>
