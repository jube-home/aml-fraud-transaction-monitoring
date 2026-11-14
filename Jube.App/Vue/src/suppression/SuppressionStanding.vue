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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {Check, Clock, Hourglass, Undo2, X, Trash2} from 'lucide-vue-next'
import {standingKey, standingTone} from './state'

const props = defineProps<{ standing: number }>()

const {t} = useI18n()

const key = computed(() => standingKey(props.standing))
const tone = computed(() => standingTone(props.standing))

const label = computed(() => {
  switch (key.value) {
    case 'approved':
      return t('suppression.standing.approved')
    case 'rejected':
      return t('suppression.standing.rejected')
    case 'retracted':
      return t('suppression.standing.retracted')
    case 'expired':
      return t('suppression.standing.expired')
    case 'removed':
      return t('suppression.standing.removed')
    default:
      return t('suppression.standing.pending')
  }
})
</script>

<template>
  <span
    class="badge badge-sm gap-1"
    :class="[
      tone === 'success' ? 'badge-success' : '',
      tone === 'warning' ? 'badge-warning' : '',
      tone === 'error' ? 'badge-error' : '',
      tone === 'neutral' ? 'badge-ghost' : '',
    ]"
    :data-testid="`suppression-standing-${key}`"
  >
    <Check v-if="key === 'approved'" key="approved" class="size-3" aria-hidden="true"/>
    <Clock v-else-if="key === 'pending'" key="pending" class="size-3" aria-hidden="true"/>
    <X v-else-if="key === 'rejected'" key="rejected" class="size-3" aria-hidden="true"/>
    <Undo2 v-else-if="key === 'retracted'" key="retracted" class="size-3" aria-hidden="true"/>
    <Hourglass v-else-if="key === 'expired'" key="expired" class="size-3" aria-hidden="true"/>
    <Trash2 v-else key="removed" class="size-3" aria-hidden="true"/>
    {{ label }}
  </span>
</template>
