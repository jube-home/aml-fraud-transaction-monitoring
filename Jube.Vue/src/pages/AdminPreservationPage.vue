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
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import TabStrip from '../components/TabStrip.vue'
import PreservationMove from '../preservation/PreservationMove.vue'
import PreservationSafeguard from '../preservation/PreservationSafeguard.vue'

const keys = ['snapshots', 'move'] as const

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const tabs = computed(() => [
  { key: keys[0], label: t('nav.preservationSnapshots') },
  { key: keys[1], label: t('nav.preservationMove') },
])

const current = computed(() => keys.find((key) => key === route.query.tab) ?? keys[0])

function choose(key: string): void {
  void router.replace({ query: { tab: key } })
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="adminPreservation-page">
    <h1 class="sr-only">{{ t('nav.adminPreservation') }}</h1>
    <TabStrip
      :label="t('nav.adminPreservation')"
      :current="current"
      :tabs="tabs"
      @choose="choose"
    />
    <PreservationSafeguard v-if="current === 'snapshots'" key="snapshots" />
    <PreservationMove v-else key="move" />
  </section>
</template>
