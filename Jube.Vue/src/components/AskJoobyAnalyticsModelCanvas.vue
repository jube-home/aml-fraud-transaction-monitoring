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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useCanvasSections } from '../canvas/useCanvasSections'
import { analyticsApi } from '../askJoobyAnalytics/api'
import { defaultViewedVersion, liveStanding } from '../askJoobyAnalytics/model'
import AnalyticsDiagram from './AnalyticsDiagram.vue'
import AnalyticsEvidence from './AnalyticsEvidence.vue'
import AnalyticsPerformance from './AnalyticsPerformance.vue'
import AnalyticsStatusBand from './AnalyticsStatusBand.vue'
import AnalyticsVersions from './AnalyticsVersions.vue'
import AnalyticsVersionStory from './AnalyticsVersionStory.vue'
import CanvasIndex from './CanvasIndex.vue'
import PaneToggles from './PaneToggles.vue'
import { usePanes } from '../composables/usePanes'
import CanvasSection from './CanvasSection.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number; instanceName?: string }>()

const { t } = useI18n()
const panes = usePanes('analytics')

const sectionIds = ['versions', 'says', 'performs', 'looks', 'evidence'] as const
type SectionId = (typeof sectionIds)[number]

const model = useQuery({
  queryKey: computed(() => queryKeys.analyticsModel(props.modelId)),
  queryFn: () => analyticsApi.model(props.modelId),
  retry: false,
})

const versions = useQuery({
  queryKey: computed(() => queryKeys.analyticsVersions(props.modelId)),
  queryFn: () => analyticsApi.versions(props.modelId),
  retry: false,
})

const list = computed(() => versions.data.value ?? [])
const standing = computed(() => liveStanding(list.value))
const viewedId = ref<number | null>(null)
const viewed = computed(() => list.value.find((version) => version.id === viewedId.value))

watch(
  list,
  (value) => {
    if (viewedId.value === null || !value.some((version) => version.id === viewedId.value)) {
      viewedId.value = defaultViewedVersion(value)
    }
  },
  { immediate: true },
)

const { opened, pick } = useCanvasSections<SectionId>(
  sectionIds,
  { versions: true, says: true, performs: true, looks: false, evidence: false },
  computed(() => model.isSuccess.value),
)

const items = computed(() => [
  { id: 'versions', label: t('analytics.section.versions') },
  { id: 'says', label: t('analytics.section.says') },
  { id: 'performs', label: t('analytics.section.performs') },
  { id: 'looks', label: t('analytics.section.looks') },
  { id: 'evidence', label: t('analytics.section.evidence') },
])

const viewing = computed(() =>
  viewed.value
    ? t('analytics.hint.viewing', { version: viewed.value.versionNumber })
    : t('analytics.hint.noVersion'),
)

function backToLive(): void {
  if (standing.value.liveId !== null) viewedId.value = standing.value.liveId
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="analytics-canvas">
    <ProblemAlert
      v-if="model.isError.value"
      key="failed"
      :reasons="[t('analytics.failed')]"
      data-testid="analytics-failed"
    />
    <div
      v-else-if="model.isPending.value"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('analytics.loading')"
    ></div>
    <template v-else-if="model.data.value">
      <AnalyticsStatusBand
        :model="model.data.value"
        :standing="standing"
        :viewed="viewed"
        :instance-name="instanceName"
        @live="backToLive"
      />

      <PaneToggles
        :left="panes.left.value"
        :left-name="t('analytics.panes.index')"
        @toggle-left="panes.toggleLeft"
      />

      <div
        class="grid min-w-0 gap-4"
        :class="panes.left.value ? 'lg:grid-cols-[13rem_minmax(0,1fr)]' : ''"
      >
        <div v-show="panes.left.value" class="lg:sticky lg:top-24 lg:self-start">
          <CanvasIndex :items="items" :label="t('analytics.index')" @pick="pick" />
        </div>

        <div class="flex min-w-0 flex-col gap-3">
          <CanvasSection
            id="versions"
            v-model:open="opened.versions"
            :title="t('analytics.section.versions')"
            :hint="t('analytics.hint.versions')"
          >
            <AnalyticsVersions
              :model-id="modelId"
              :viewed-id="viewedId"
              @show="viewedId = $event"
            />
          </CanvasSection>

          <CanvasSection
            id="says"
            v-model:open="opened.says"
            :title="t('analytics.section.says')"
            :hint="viewing"
          >
            <AnalyticsVersionStory
              v-if="viewedId !== null"
              :key="viewedId"
              :version-id="viewedId"
            />
            <p v-else key="none" class="text-sm text-secondary">{{ t('analytics.noVersion') }}</p>
          </CanvasSection>

          <CanvasSection
            id="performs"
            v-model:open="opened.performs"
            :title="t('analytics.section.performs')"
            :hint="viewing"
          >
            <AnalyticsPerformance v-if="viewedId !== null" :key="viewedId" :version-id="viewedId" />
            <p v-else key="none" class="text-sm text-secondary">{{ t('analytics.noVersion') }}</p>
          </CanvasSection>

          <CanvasSection
            id="looks"
            v-model:open="opened.looks"
            :title="t('analytics.section.looks')"
            :hint="viewing"
          >
            <AnalyticsDiagram v-if="viewedId !== null" :key="viewedId" :version-id="viewedId" />
            <p v-else key="none" class="text-sm text-secondary">{{ t('analytics.noVersion') }}</p>
          </CanvasSection>

          <CanvasSection
            id="evidence"
            v-model:open="opened.evidence"
            :title="t('analytics.section.evidence')"
            :hint="viewing"
          >
            <AnalyticsEvidence v-if="viewedId !== null" :key="viewedId" :version-id="viewedId" />
            <p v-else key="none" class="text-sm text-secondary">{{ t('analytics.noVersion') }}</p>
          </CanvasSection>
        </div>
      </div>
    </template>
  </div>
</template>
