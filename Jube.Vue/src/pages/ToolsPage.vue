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
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import BuilderCoderBench from './tools/BuilderCoderBench.vue'
import CaseFilterBuilderBench from './tools/CaseFilterBuilderBench.vue'
import ExhaustiveFilterBuilderBench from './tools/ExhaustiveFilterBuilderBench.vue'
import CrudBench from './tools/CrudBench.vue'
import GridBench from './tools/GridBench.vue'
import PerformanceBench from './tools/PerformanceBench.vue'
import GridCsvExportBench from './tools/GridCsvExportBench.vue'
import EvidenceTreeBench from './tools/EvidenceTreeBench.vue'
import GridStatisticsTableBench from './tools/GridStatisticsTableBench.vue'
import OverrideBench from './tools/OverrideBench.vue'
import IntegrityBench from './tools/IntegrityBench.vue'
import RuleToolsBench from './tools/RuleToolsBench.vue'
import ModelSampleBench from './tools/ModelSampleBench.vue'
import SanctionsLoaderBench from './tools/SanctionsLoaderBench.vue'
import DatasetUploadBench from './tools/DatasetUploadBench.vue'
import ExhaustiveResultsBench from './tools/ExhaustiveResultsBench.vue'
import AskJoobyAnalyticsBench from './tools/AskJoobyAnalyticsBench.vue'
import WatcherBench from './tools/WatcherBench.vue'
import SanctionSearchBench from './tools/SanctionSearchBench.vue'
import ValueGridBench from './tools/ValueGridBench.vue'
import DownloadButtonBench from './tools/DownloadButtonBench.vue'
import FileUploadBench from './tools/FileUploadBench.vue'
import ApprovalManagerBench from './tools/ApprovalManagerBench.vue'
import RoleAllocationBench from './tools/RoleAllocationBench.vue'
import PermissionAllocationBench from './tools/PermissionAllocationBench.vue'
import DatasourceBuilderBench from './tools/DatasourceBuilderBench.vue'
import VisualisationDashboardBench from './tools/VisualisationDashboardBench.vue'
import StoryBench from './tools/StoryBench.vue'
import ModelStoryBench from './tools/ModelStoryBench.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

interface Tool {
  id: string
  name: string
  source: string
  next: boolean
}

const integrityTools = [
  'integrity',
  'model-picker',
  'integrity-findings',
  'engine-state',
  'model-synchronisation',
  'dependency-diagram',
  'pending-approvals',
  'severity-badge',
]

const tools: Tool[] = [
  { id: 'builder-coder', name: 'BuilderCoder', source: 'wwwroot/js/BuilderCoder.js', next: true },
  {
    id: 'role-allocation',
    name: 'RoleAllocation',
    source: 'wwwroot/js/RoleAllocation/RoleAllocation.js',
    next: true,
  },
  {
    id: 'permission-allocation',
    name: 'PermissionAllocation',
    source: 'replaces Tree?Page=RoleRegistryPermission',
    next: true,
  },
  {
    id: 'approval-manager',
    name: 'ApprovalManager',
    source: 'wwwroot/js/Approval/ApprovalManager.js',
    next: true,
  },
  {
    id: 'datasource-builder',
    name: 'DatasourceBuilder',
    source: 'wwwroot/js/Administration/Frame/VisualisationRegistryDatasource.js',
    next: false,
  },
  {
    id: 'visualisation-dashboard',
    name: 'VisualisationDashboard',
    source: 'wwwroot/js/Visualisation/Visualisation.js',
    next: false,
  },
  {
    id: 'value-grid',
    name: 'ValueGrid',
    source: 'wwwroot/js/Model/Frame/ValueGrid.js',
    next: false,
  },
  {
    id: 'case-filter-builder',
    name: 'CaseFilterBuilder',
    source: 'wwwroot/js/CaseFilterBuilder.js',
    next: false,
  },
  {
    id: 'exhaustive-filter-builder',
    name: 'ExhaustiveFilterBuilder',
    source: 'wwwroot/js/ExhaustiveFilterBuilder.js',
    next: false,
  },
  { id: 'evidence-tree', name: 'EvidenceTree', source: 'wwwroot/js/EvidenceTree.js', next: false },
  {
    id: 'grid-csv-export',
    name: 'GridExport',
    source: 'wwwroot/js/GridCsvExport.js',
    next: false,
  },
  {
    id: 'grid-statistics-table',
    name: 'GridStatisticsTable',
    source: 'wwwroot/js/GridStatisticsTable.js',
    next: false,
  },
  {
    id: 'model-story',
    name: 'ModelStory',
    source: 'Models journey (new): models table, model hub and the drill down',
    next: true,
  },
  {
    id: 'story',
    name: 'StorySpine',
    source: 'Models journey (new): beats, receipts and the working beat',
    next: true,
  },
  { id: 'crud', name: 'CRUD', source: 'wwwroot/js/CRUD.js and Tree.js', next: false },
  {
    id: 'performance',
    name: 'Performance',
    source: 'wwwroot/js/Administration (performance and monitoring pages)',
    next: false,
  },
  { id: 'grid', name: 'Grid', source: 'wwwroot/js/Tree.js (grid view)', next: false },
  {
    id: 'integrity',
    name: 'Integrity',
    source: 'wwwroot/js/Model/Integrity.js',
    next: false,
  },
  {
    id: 'model-picker',
    name: 'ModelPicker',
    source: 'wwwroot/js/Model/Integrity.js (model picker)',
    next: false,
  },
  {
    id: 'integrity-findings',
    name: 'IntegrityFindings',
    source: 'wwwroot/js/Model/Integrity.js (findings grid)',
    next: false,
  },
  {
    id: 'engine-state',
    name: 'EngineState',
    source: 'wwwroot/js/Model/Integrity.js (engine grid)',
    next: false,
  },
  {
    id: 'model-synchronisation',
    name: 'ModelSynchronisation',
    source: 'wwwroot/js/Model/Integrity.js (synchronisation panel)',
    next: false,
  },
  {
    id: 'dependency-diagram',
    name: 'DependencyDiagram',
    source: 'wwwroot/js/Model/Integrity.js (dependency diagram)',
    next: false,
  },
  {
    id: 'pending-approvals',
    name: 'PendingApprovals',
    source: 'wwwroot/js/Model/Integrity.js and AskJoobyAnalytics.js (pending approvals)',
    next: false,
  },
  {
    id: 'severity-badge',
    name: 'SeverityBadge',
    source: 'wwwroot/js/Model/Integrity.js (severityBadge)',
    next: false,
  },
  {
    id: 'download-button',
    name: 'DownloadButton',
    source: 'wwwroot/js/Administration/Preservation.js and Model/EntityAnalysisModelSample.js',
    next: false,
  },
  {
    id: 'sanctions-loader',
    name: 'SanctionsLoader',
    source: 'wwwroot/js/Administration/SanctionsLoader.js',
    next: false,
  },
  {
    id: 'dataset-upload',
    name: 'DatasetUpload',
    source: 'wwwroot/js/Model/Frame/ExhaustiveDataset.js',
    next: false,
  },
  {
    id: 'advanced-analytics',
    name: 'AskJoobyAnalytics',
    source: 'wwwroot/js/Model/AskJoobyAnalytics.js',
    next: false,
  },
  {
    id: 'exhaustive-results',
    name: 'ExhaustiveResults',
    source: 'wwwroot/js/Model/Frame/Exhaustive.js and ExhaustiveCharts.js',
    next: false,
  },
  {
    id: 'model-sample',
    name: 'ModelSample',
    source: 'wwwroot/js/Model/EntityAnalysisModelSample.js',
    next: false,
  },
  {
    id: 'rule-tools',
    name: 'RuleTools',
    source: 'wwwroot/js/Model/Frame/RuleTools.js',
    next: false,
  },
  {
    id: 'watcher',
    name: 'Watcher',
    source: 'wwwroot/js/Watcher/Activation.js',
    next: false,
  },
  {
    id: 'sanction-search',
    name: 'SanctionSearch',
    source: 'wwwroot/js/Sanction/Search.js',
    next: false,
  },
  { id: 'override', name: 'Override', source: 'wwwroot/js/Override.js', next: false },
  {
    id: 'antiforgery-upload',
    name: 'AntiforgeryUpload',
    source: 'wwwroot/js/AntiforgeryUpload.js',
    next: false,
  },
]

const selected = computed<Tool>(
  () => tools.find((tool) => tool.id === route.query.tool) ?? tools[0]!,
)

function select(tool: Tool): void {
  void router.replace({ query: { ...route.query, tool: tool.id } })
}
</script>

<template>
  <section class="flex h-full min-h-0 flex-col gap-3">
    <h1 class="text-xl font-semibold text-primary">{{ t('tools.title') }}</h1>
    <p>{{ t('tools.intro') }}</p>
    <div class="grid min-h-0 flex-1 gap-4 lg:grid-cols-[20rem_minmax(0,1fr)]">
      <nav
        :aria-label="t('tools.list')"
        class="min-h-0 overflow-y-auto rounded-box border border-base-300 bg-base-100 p-2"
      >
        <ul class="menu w-full p-0">
          <li v-for="tool in tools" :key="tool.id">
            <button
              type="button"
              :class="{
                'menu-active bg-base-200! font-semibold text-base-content!':
                  tool.id === selected.id,
              }"
              :aria-current="tool.id === selected.id ? 'true' : undefined"
              :data-testid="`tool-${tool.id}`"
              @click="select(tool)"
            >
              <span class="flex-1">{{ tool.name }}</span>
              <span
                class="badge badge-sm font-mono"
                :class="tool.next ? 'badge-primary' : 'badge-neutral'"
              >
                {{ tool.next ? t('tools.statusNext') : t('tools.statusPending') }}
              </span>
            </button>
          </li>
        </ul>
      </nav>
      <article
        class="flex min-h-0 flex-col gap-3 rounded-box border border-base-300 bg-base-100 p-4"
      >
        <header class="flex flex-col gap-1">
          <h2 class="text-lg font-semibold" data-testid="tool-title">{{ selected.name }}</h2>
          <p class="text-sm">
            {{ t('tools.source') }}:
            <code class="font-mono">{{ selected.source }}</code>
          </p>
        </header>
        <BuilderCoderBench v-if="selected.id === 'builder-coder'" />
        <CaseFilterBuilderBench v-else-if="selected.id === 'case-filter-builder'" />
        <ExhaustiveFilterBuilderBench v-else-if="selected.id === 'exhaustive-filter-builder'" />
        <ModelStoryBench v-else-if="selected.id === 'model-story'" />
        <StoryBench v-else-if="selected.id === 'story'" />
        <CrudBench v-else-if="selected.id === 'crud'" />
        <GridBench v-else-if="selected.id === 'grid'" />
        <PerformanceBench v-else-if="selected.id === 'performance'" />
        <OverrideBench v-else-if="selected.id === 'override'" />
        <AskJoobyAnalyticsBench v-else-if="selected.id === 'advanced-analytics'" />
        <ExhaustiveResultsBench v-else-if="selected.id === 'exhaustive-results'" />
        <DatasetUploadBench v-else-if="selected.id === 'dataset-upload'" />
        <SanctionsLoaderBench v-else-if="selected.id === 'sanctions-loader'" />
        <ModelSampleBench v-else-if="selected.id === 'model-sample'" />
        <RuleToolsBench v-else-if="selected.id === 'rule-tools'" />
        <IntegrityBench v-else-if="integrityTools.includes(selected.id)" />
        <WatcherBench v-else-if="selected.id === 'watcher'" />
        <SanctionSearchBench v-else-if="selected.id === 'sanction-search'" />
        <GridCsvExportBench v-else-if="selected.id === 'grid-csv-export'" />
        <EvidenceTreeBench v-else-if="selected.id === 'evidence-tree'" />
        <GridStatisticsTableBench v-else-if="selected.id === 'grid-statistics-table'" />
        <ValueGridBench v-else-if="selected.id === 'value-grid'" />
        <DownloadButtonBench v-else-if="selected.id === 'download-button'" />
        <FileUploadBench v-else-if="selected.id === 'antiforgery-upload'" />
        <ApprovalManagerBench v-else-if="selected.id === 'approval-manager'" />
        <RoleAllocationBench v-else-if="selected.id === 'role-allocation'" />
        <PermissionAllocationBench v-else-if="selected.id === 'permission-allocation'" />
        <DatasourceBuilderBench v-else-if="selected.id === 'datasource-builder'" />
        <VisualisationDashboardBench v-else-if="selected.id === 'visualisation-dashboard'" />
        <section
          v-else
          :aria-label="t('tools.workbench')"
          class="flex min-h-0 flex-1 items-center justify-center rounded-field border border-dashed border-base-300 bg-base-200 p-6 text-center"
          data-testid="workbench"
        >
          <p class="max-w-md text-sm">{{ t('tools.empty', { tool: selected.name }) }}</p>
        </section>
      </article>
    </div>
  </section>
</template>
