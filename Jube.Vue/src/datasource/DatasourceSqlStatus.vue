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
import { Check, CircleDashed, CircleX, Loader, WifiOff } from 'lucide-vue-next'
import type { SqlState } from './types'

defineProps<{
  state: SqlState
  error: string | null
  line?: number | null
  column?: number | null
}>()

const { t } = useI18n()
</script>

<template>
  <div class="flex flex-col items-start gap-1.5" data-testid="datasource-status">
    <p role="status">
      <span
        class="badge badge-sm font-mono"
        :class="{
          'badge-success': state === 'valid',
          'badge-error': state === 'errors' || state === 'disconnected',
        }"
      >
        <Check v-if="state === 'valid'" aria-hidden="true" class="size-3" />
        <CircleX v-else-if="state === 'errors'" aria-hidden="true" class="size-3" />
        <WifiOff v-else-if="state === 'disconnected'" aria-hidden="true" class="size-3" />
        <Loader v-else-if="state === 'checking'" aria-hidden="true" class="size-3" />
        <CircleDashed v-else aria-hidden="true" class="size-3" />
        <template v-if="state === 'valid'">{{ t('datasource.status.valid') }}</template>
        <template v-else-if="state === 'errors'">{{ t('datasource.status.errors') }}</template>
        <template v-else-if="state === 'disconnected'">{{
          t('datasource.status.disconnected')
        }}</template>
        <template v-else-if="state === 'checking'">{{ t('datasource.status.checking') }}</template>
        <template v-else>{{ t('datasource.status.incomplete') }}</template>
      </span>
    </p>
    <p
      v-if="state === 'errors' && error"
      key="error"
      class="text-sm wrap-break-word text-error"
      data-testid="datasource-sql-error"
    >
      {{ error }}
      <span v-if="line" key="where" class="font-mono" data-testid="datasource-sql-error-position">
        {{ t('datasource.status.position', { line, column: column ?? 1 }) }}
      </span>
    </p>
  </div>
</template>
