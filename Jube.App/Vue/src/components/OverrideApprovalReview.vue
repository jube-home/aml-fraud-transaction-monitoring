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
import type {ForceApprovalArea} from '../overrides/forceApproval'
import ApprovalPanel from './ApprovalPanel.vue'
import PanelCard from './PanelCard.vue'

defineProps<{ area: ForceApprovalArea; entityId: number; name: string }>()

const emit = defineEmits<{ changed: []; close: [] }>()

const {t} = useI18n()

const isolate = (cell: string) => `⁨${cell}⁩`
</script>

<template>
  <PanelCard :title="t('override.approval.title', { name: isolate(name) })" compact>
    <template #actions>
      <button
        type="button"
        class="btn btn-sm btn-ghost ms-auto"
        data-testid="override-approval-close"
        @click="emit('close')"
      >
        {{ t('override.approval.close') }}
      </button>
    </template>
    <p class="mb-2 text-sm" data-testid="override-approval-note">
      {{ t('override.approval.note') }}
    </p>
    <ApprovalPanel
      :endpoint="area.endpoint"
      :approval="area.approval"
      :entity-id="entityId"
      @changed="emit('changed')"
    />
  </PanelCard>
</template>
