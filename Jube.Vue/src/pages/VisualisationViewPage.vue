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
import { useRoute } from 'vue-router'
import VisualisationStory from '../dashboard/VisualisationStory.vue'

const { t } = useI18n()
const route = useRoute()

const guid = computed(() => String(route.params.guid ?? ''))
</script>

<template>
  <section class="flex flex-col gap-4" data-testid="visualisation-page">
    <h1 class="sr-only">{{ t('nav.visualisation') }}</h1>
    <VisualisationStory :key="guid" :guid="guid" />
  </section>
</template>
