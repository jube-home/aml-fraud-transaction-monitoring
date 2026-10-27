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
import { AlertCircle, CheckCircle2, Info, X } from 'lucide-vue-next'
import { useToastStore } from '../stores/toasts'

const { t } = useI18n()
const toasts = useToastStore()
</script>

<template>
  <div class="toast toast-start toast-bottom z-50" data-testid="toasts">
    <div
      v-for="toast in toasts.items"
      :key="toast.id"
      :role="toast.kind === 'error' ? 'alert' : 'status'"
      class="alert"
      :class="{
        'alert-error': toast.kind === 'error',
        'alert-success': toast.kind === 'success',
        'alert-info': toast.kind === 'info',
      }"
    >
      <AlertCircle v-if="toast.kind === 'error'" aria-hidden="true" class="size-5 shrink-0" />
      <CheckCircle2
        v-else-if="toast.kind === 'success'"
        aria-hidden="true"
        class="size-5 shrink-0"
      />
      <Info v-else aria-hidden="true" class="size-5 shrink-0" />
      <span>{{ toast.text }}</span>
      <button
        type="button"
        class="btn btn-ghost btn-xs btn-square"
        :aria-label="t('shell.dismiss')"
        @click="toasts.dismiss(toast.id)"
      >
        <X aria-hidden="true" class="size-4" />
      </button>
    </div>
  </div>
</template>
