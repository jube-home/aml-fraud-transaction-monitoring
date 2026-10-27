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
import type { Severity } from '../integrity/types'

defineProps<{ severity: Severity }>()

const { t } = useI18n()
</script>

<template>
  <span
    v-if="severity === 'Error'"
    key="error"
    class="badge badge-error badge-soft badge-sm"
    data-testid="severity-badge"
    >{{ t('integrity.severity.error') }}</span
  >
  <span
    v-else-if="severity === 'Warning'"
    key="warning"
    class="badge badge-warning badge-soft badge-sm"
    data-testid="severity-badge"
    >{{ t('integrity.severity.warning') }}</span
  >
  <span
    v-else
    key="info"
    class="badge badge-info badge-soft badge-sm"
    data-testid="severity-badge"
    >{{ t('integrity.severity.info') }}</span
  >
</template>
