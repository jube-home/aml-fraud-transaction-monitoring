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
import { FileSpreadsheet, FileText } from 'lucide-vue-next'

defineProps<{ disabled?: boolean }>()

const emit = defineEmits<{ csv: []; excel: [] }>()

const { t } = useI18n()
</script>

<template>
  <div class="join" role="group" :aria-label="t('export.group')" data-testid="grid-export">
    <button
      type="button"
      class="btn btn-sm join-item border-base-300 bg-base-100"
      :disabled="disabled"
      data-testid="export-excel"
      @click="emit('excel')"
    >
      <FileSpreadsheet aria-hidden="true" class="size-4" />
      {{ t('export.excel') }}
    </button>
    <button
      type="button"
      class="btn btn-sm join-item border-base-300 bg-base-100"
      :disabled="disabled"
      data-testid="export-csv"
      @click="emit('csv')"
    >
      <FileText aria-hidden="true" class="size-4" />
      {{ t('export.csv') }}
    </button>
  </div>
</template>
