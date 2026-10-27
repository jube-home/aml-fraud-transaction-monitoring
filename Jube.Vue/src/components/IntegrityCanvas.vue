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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useCanvasSections } from '../canvas/useCanvasSections'
import CanvasIndex from './CanvasIndex.vue'
import PaneToggles from './PaneToggles.vue'
import { usePanes } from '../composables/usePanes'
import CanvasSection from './CanvasSection.vue'
import DependencyDiagram from './DependencyDiagram.vue'
import DependencyEntities from './DependencyEntities.vue'
import EngineState from './EngineState.vue'
import IntegrityFindings from './IntegrityFindings.vue'
import IntegrityStatusBand from './IntegrityStatusBand.vue'
import ModelSynchronisation from './ModelSynchronisation.vue'
import PendingApprovals from './PendingApprovals.vue'

withDefaults(defineProps<{ modelId: number; canSynchronise?: boolean }>(), {
  canSynchronise: false,
})

const { t } = useI18n()
const panes = usePanes('integrity')
const queryClient = useQueryClient()

const sectionIds = ['findings', 'engine', 'dependencies', 'pending'] as const
type SectionId = (typeof sectionIds)[number]

const selectedNode = ref<string | null>(null)

const { opened, pick } = useCanvasSections<SectionId>(
  sectionIds,
  { findings: true, engine: true, dependencies: false, pending: false },
  ref(true),
)

const items = computed(() => [
  { id: 'findings', label: t('integrity.section.findings') },
  { id: 'engine', label: t('integrity.section.engine') },
  { id: 'dependencies', label: t('integrity.section.dependencies') },
  { id: 'pending', label: t('integrity.section.pending') },
])

function check(): void {
  void queryClient.invalidateQueries({ queryKey: ['integrity'] })
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="integrity-canvas">
    <IntegrityStatusBand :model-id="modelId" @check="check" />

    <PaneToggles
      :left="panes.left.value"
      :left-name="t('integrity.panes.index')"
      @toggle-left="panes.toggleLeft"
    />

    <div
      class="grid min-w-0 gap-4"
      :class="panes.left.value ? 'lg:grid-cols-[13rem_minmax(0,1fr)]' : ''"
    >
      <div v-show="panes.left.value" class="lg:sticky lg:top-24 lg:self-start">
        <CanvasIndex :items="items" :label="t('integrity.index')" @pick="pick" />
      </div>

      <div class="flex min-w-0 flex-col gap-3">
        <CanvasSection
          id="findings"
          v-model:open="opened.findings"
          :title="t('integrity.section.findings')"
          :hint="t('integrity.hint.findings')"
        >
          <IntegrityFindings :key="`f${modelId}`" :model-id="modelId" />
        </CanvasSection>

        <CanvasSection
          id="engine"
          v-model:open="opened.engine"
          :title="t('integrity.section.engine')"
          :hint="t('integrity.hint.engine')"
        >
          <ModelSynchronisation
            :key="`s${modelId}`"
            :model-id="modelId"
            :allowed="canSynchronise"
          />
          <EngineState :key="`e${modelId}`" :model-id="modelId" />
        </CanvasSection>

        <CanvasSection
          id="dependencies"
          v-model:open="opened.dependencies"
          :title="t('integrity.section.dependencies')"
          :hint="t('integrity.hint.dependencies')"
        >
          <DependencyDiagram
            :key="`d${modelId}`"
            :model-id="modelId"
            :selected="selectedNode"
            @select="selectedNode = $event"
          />
          <p v-if="selectedNode" key="selected" class="text-sm" data-testid="dependency-selected">
            {{ t('integrity.deps.selected', { id: selectedNode }) }}
          </p>
          <DependencyEntities
            :key="`n${modelId}`"
            :model-id="modelId"
            @select="selectedNode = $event"
          />
        </CanvasSection>

        <CanvasSection
          id="pending"
          v-model:open="opened.pending"
          :title="t('integrity.section.pending')"
          :hint="t('integrity.hint.pending')"
        >
          <PendingApprovals :key="`p${modelId}`" :model-id="modelId" />
        </CanvasSection>
      </div>
    </div>
  </div>
</template>
