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
import { PanelLeftClose, PanelLeftOpen, PanelRightClose, PanelRightOpen } from 'lucide-vue-next'

defineProps<{
  left?: boolean
  right?: boolean
  leftName?: string
  rightName?: string
}>()

const emit = defineEmits<{ toggleLeft: []; toggleRight: [] }>()

const { t } = useI18n()
</script>

<template>
  <div class="flex items-center justify-between gap-2" data-testid="pane-toggles">
    <button
      v-if="leftName !== undefined"
      key="left"
      type="button"
      class="btn btn-ghost btn-sm gap-2"
      :aria-expanded="left"
      :data-testid="'pane-toggle-left'"
      @click="emit('toggleLeft')"
    >
      <PanelLeftClose v-if="left" key="close" class="size-4" aria-hidden="true" />
      <PanelLeftOpen v-else key="open" class="size-4" aria-hidden="true" />
      {{ left ? t('panes.hide', { name: leftName }) : t('panes.show', { name: leftName }) }}
    </button>
    <span v-else key="spacer"></span>
    <button
      v-if="rightName !== undefined"
      key="right"
      type="button"
      class="btn btn-ghost btn-sm gap-2"
      :aria-expanded="right"
      :data-testid="'pane-toggle-right'"
      @click="emit('toggleRight')"
    >
      {{ right ? t('panes.hide', { name: rightName }) : t('panes.show', { name: rightName }) }}
      <PanelRightClose v-if="right" key="close" class="size-4" aria-hidden="true" />
      <PanelRightOpen v-else key="open" class="size-4" aria-hidden="true" />
    </button>
  </div>
</template>
