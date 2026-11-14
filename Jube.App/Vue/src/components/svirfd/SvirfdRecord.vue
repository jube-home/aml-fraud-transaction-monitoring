<!--  Copyright (C) 2022-present Jube Holdings Limited.

  This file is part of Jube™ software.

  Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
  as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
  Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
  of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

  You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
  see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useFormat} from '../../composables/useFormat'
import {changedPaths} from '../../svirfd/drift'
import {confidenceTone, toneClass} from '../../svirfd/format'
import {provenance, sectionIds, sectionLabels} from '../../svirfd/sections'
import type {DriftEntry, IpRecord, SvirfdNarrativeGroup} from '../../svirfd/types'
import PanelCard from '../PanelCard.vue'
import SvirfdFacts from './SvirfdFacts.vue'
import SvirfdMap from './SvirfdMap.vue'

const props = withDefaults(
  defineProps<{
    record: IpRecord
    headline?: string | null
    narrative?: SvirfdNarrativeGroup[]
    attribution: string
    licenceUrl?: string
    mode?: string
    stored?: IpRecord | null
    storedAt?: string | null
    drift?: DriftEntry[] | null
    showMap?: boolean
    explainEmpty?: boolean
    rttChartSvg?: string | null
  }>(),
  {showMap: true, narrative: () => []},
)

const {t} = useI18n()
const {dateTime} = useFormat()

const risHost = 'ripe.net/ris'
const routeViewsHost = 'routeviews.org'

const changed = computed(() => changedPaths(props.drift))

const privacyBadges = computed(() => {
  const {privacy, connection} = props.record

  return [
    {key: 'vpn', on: privacy.vpn === true, label: 'VPN'},
    {key: 'tor', on: privacy.tor === true, label: 'Tor'},
    {key: 'relay', on: privacy.relay === true, label: 'Relay'},
    {
      key: 'hosting',
      on: privacy.hosting === true || connection.hosting === true,
      label: 'Hosting',
    },
    {key: 'bogon', on: props.record.bogon, label: 'Bogon'},
  ].filter((badge) => badge.on)
})

const sectionSet: ReadonlySet<string> = new Set(sectionIds)

const orphans = computed(() => props.narrative.filter((group) => !sectionSet.has(group.id)))

function paragraphsOf(section: string): string[] {
  return props.narrative.filter((group) => group.id === section).flatMap((g) => g.paragraphs)
}

const chipClass: Record<string, string> = {
  measured: 'badge-primary badge-soft',
  prior: 'badge-accent badge-soft',
  plain: 'badge-ghost',
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="svirfd-record">
    <header class="flex flex-col gap-2">
      <h2 class="font-mono text-lg font-semibold" data-testid="svirfd-ip">{{ record.ip }}</h2>
      <p v-if="headline" class="text-sm" data-testid="svirfd-headline">{{ headline }}</p>
      <p class="flex flex-wrap items-center gap-2">
        <span
          v-if="record.connection.networkType"
          class="badge badge-sm badge-soft badge-primary"
        >{{ record.connection.networkType }}</span
        >
        <span
          v-for="badge in privacyBadges"
          :key="badge.key"
          class="badge badge-sm badge-warning"
        >{{ badge.label }}</span
        >
        <span v-if="record.cloud" class="badge badge-sm badge-info badge-soft">{{
            record.cloud.provider
          }}</span>
        <span
          :class="['badge badge-sm', toneClass(confidenceTone(record.location.cityConfidence))]"
        >{{ record.location.cityConfidence }}</span
        >
        <span v-if="mode" class="badge badge-sm badge-ghost">{{
            t('svirfd.answered', {mode})
          }}</span>
      </p>
    </header>

    <PanelCard v-if="drift" key="drift" :title="t('svirfd.driftTitle')" :level="2" compact>
      <template #actions>
        <span class="badge ms-auto" data-testid="svirfd-drift-count">{{
            t('svirfd.driftCount', {n: drift.length})
          }}</span>
      </template>
      <p v-if="storedAt" class="mb-2 text-xs text-secondary">
        {{ t('svirfd.storedAt', {date: dateTime(storedAt)}) }}
      </p>
      <p v-if="drift.length === 0" class="text-sm" data-testid="svirfd-drift-none">
        {{ t('svirfd.driftNone') }}
      </p>
      <table v-else class="table table-sm" data-testid="svirfd-drift">
        <thead>
        <tr>
          <th>{{ t('svirfd.driftPath') }}</th>
          <th>{{ t('svirfd.driftStored') }}</th>
          <th>{{ t('svirfd.driftFresh') }}</th>
        </tr>
        </thead>
        <tbody>
        <tr v-for="entry in drift" :key="entry.path">
          <td class="font-mono">{{ entry.path }}</td>
          <td>{{ entry.stored ?? '–' }}</td>
          <td>{{ entry.fresh ?? '–' }}</td>
        </tr>
        </tbody>
      </table>
    </PanelCard>

    <PanelCard v-if="showMap" key="map" :title="t('svirfd.map')" :level="2" compact>
      <SvirfdMap :record="record" :stored="stored ?? null"/>
    </PanelCard>

    <PanelCard
      v-if="showMap && rttChartSvg"
      key="rtt"
      :title="t('svirfd.rttChart')"
      :level="2"
      compact
    >
      <img
        :src="`data:image/svg+xml;charset=utf-8,${encodeURIComponent(rttChartSvg)}`"
        :alt="t('svirfd.rttChart')"
        class="h-auto w-full max-w-3xl"
        data-testid="svirfd-rtt"
      />
    </PanelCard>

    <PanelCard
      v-if="orphans.length > 0"
      key="narrative"
      :title="t('svirfd.narrative')"
      :level="2"
      compact
    >
      <div class="flex flex-col gap-3 text-sm" data-testid="svirfd-narrative">
        <section v-for="group in orphans" :key="group.id">
          <h3 class="font-semibold">{{ group.title }}</h3>
          <p v-for="paragraph in group.paragraphs" :key="paragraph" class="mt-1">{{ paragraph }}</p>
        </section>
      </div>
    </PanelCard>

    <div class="grid min-w-0 gap-4 @3xl:grid-cols-2">
      <template v-for="section in sectionIds" :key="section">
        <PanelCard
          :title="t(sectionLabels[section] ?? 'svirfd.section.identity')"
          :level="2"
          compact
        >
          <template v-if="provenance(record, section)" #actions>
            <span
              :class="['badge badge-sm', chipClass[provenance(record, section)!.tone]]"
              :data-testid="`svirfd-chip-${section}`"
            >{{ provenance(record, section)!.label }}</span
            >
          </template>
          <SvirfdFacts :record="record" :section="section" :changed="changed"/>
          <p
            v-if="explainEmpty && section === 'cloud' && !record.cloud"
            class="text-sm text-secondary"
            data-testid="svirfd-no-cloud-signal"
          >
            {{ t('svirfd.noCloudSignal') }}
          </p>
          <div
            v-if="paragraphsOf(section).length > 0"
            class="mt-2 flex flex-col gap-1 border-t border-base-300 pt-2 text-sm"
            :data-testid="`svirfd-narrative-${section}`"
          >
            <p v-for="paragraph in paragraphsOf(section)" :key="paragraph">{{ paragraph }}</p>
          </div>
        </PanelCard>
      </template>
    </div>

    <PanelCard
      v-if="record.location.evidence && record.location.evidence.length > 0"
      key="evidence"
      :title="t('svirfd.evidence')"
      :level="2"
      compact
    >
      <ul class="flex flex-col gap-1 text-sm" data-testid="svirfd-evidence">
        <li v-for="item in record.location.evidence" :key="item.source + item.detail">
          <span class="badge badge-ghost badge-sm me-2">{{ item.source }}</span
          >{{ item.detail }}
        </li>
      </ul>
    </PanelCard>

    <footer class="flex flex-col gap-2 text-xs text-secondary" data-testid="svirfd-attribution">
      <p>{{ attribution }}</p>
      <p class="flex flex-wrap gap-x-4">
        <a class="link" href="https://svirfd.com/sources" target="_blank" rel="noopener">{{
            t('svirfd.sources')
          }}</a>
        <a
          class="link"
          :href="licenceUrl ?? 'https://svirfd.com/licence'"
          target="_blank"
          rel="noopener"
        >{{ t('svirfd.licence') }}</a
        >
      </p>
      <details>
        <summary class="cursor-pointer">{{ t('svirfd.acknowledgements') }}</summary>
        <p class="mt-1">
          {{ t('svirfd.risText') }}
          <a class="link" href="https://www.ripe.net/ris" target="_blank" rel="noopener">{{
              risHost
            }}</a>
        </p>
        <p class="mt-1">
          {{ t('svirfd.routeViewsText') }}
          <a class="link" href="https://www.routeviews.org" target="_blank" rel="noopener">{{
              routeViewsHost
            }}</a>
        </p>
      </details>
    </footer>
  </div>
</template>
