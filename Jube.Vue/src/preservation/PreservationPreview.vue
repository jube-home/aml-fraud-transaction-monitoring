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
import { useI18n } from 'vue-i18n'
import PreservationDiffView from './PreservationDiffView.vue'
import type { ImportPreview } from './types'

defineProps<{ preview: ImportPreview }>()

const { t } = useI18n()
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-preview">
    <div
      v-for="blocker in preview.blockers"
      :key="blocker.code"
      role="alert"
      class="alert alert-error items-start text-sm"
      data-testid="preview-blocker"
    >
      <span>{{ blocker.message }}</span>
    </div>

    <div
      v-for="warning in preview.warnings"
      :key="warning.code"
      role="status"
      class="alert alert-warning items-start text-sm"
      :data-testid="`preview-warning-${warning.code}`"
    >
      <span>{{ warning.message }}</span>
    </div>

    <div
      v-if="preview.orphanedUsers.count > 0"
      key="orphans"
      role="status"
      class="alert alert-warning items-start text-sm"
      data-testid="preview-orphans"
    >
      <div class="flex flex-col gap-2">
        <p>
          {{ t('preservation.preview.orphans', { count: preview.orphanedUsers.count }) }}
          {{
            preview.orphanedUsers.importerStaysActive
              ? t('preservation.preview.orphansYou')
              : t('preservation.preview.orphansDeactivated')
          }}
        </p>
        <ul class="flex flex-wrap gap-1">
          <li v-for="name in preview.orphanedUsers.names" :key="name">
            <span class="badge badge-sm font-mono">{{ name }}</span>
          </li>
        </ul>
      </div>
    </div>

    <PreservationDiffView :diff="preview.diff" :caption="t('preservation.preview.title')" />
  </div>
</template>
