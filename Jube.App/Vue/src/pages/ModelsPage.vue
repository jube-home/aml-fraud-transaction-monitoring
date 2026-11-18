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
import ModelStory from '../components/ModelStory.vue'
import {useStoryPath} from '../composables/useStoryPath'

const {t} = useI18n()
const {start, generation, remember} = useStoryPath()
</script>

<template>
  <section class="flex flex-col gap-4" data-testid="models-page">
    <h1 class="sr-only">{{ t('nav.models') }}</h1>
    <ModelStory :key="generation" :path="start" @path="remember"/>
  </section>
</template>
