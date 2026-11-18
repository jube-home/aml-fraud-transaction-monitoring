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
import {Telescope} from 'lucide-vue-next'
import TallySection from '../../components/TallySection.vue'
import CaseResearchItem from './CaseResearchItem.vue'
import type {CaseResearchEntry} from './types'

defineProps<{ research: CaseResearchEntry[]; loading: boolean; failed: boolean }>()

const {t} = useI18n()
</script>

<template>
  <TallySection
    :label="t('caseDetail.research.heading')"
    :count="research.length"
    :loading="loading"
    :failed="failed"
    :failed-text="t('caseDetail.research.failed')"
    testid="case-research-section"
  >
    <template #icon>
      <Telescope class="size-4"/>
    </template>
    <ul class="list rounded-box border border-base-300 bg-base-100">
      <CaseResearchItem v-for="entry in research" :key="entry.id" :research="entry"/>
    </ul>
  </TallySection>
</template>
