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
import {useFormat} from '../composables/useFormat'
import {otherKeys, widerKnown, widerNumber} from './state'
import type {SuppressionCounts} from './types'

const props = defineProps<{ heading: string; counts: SuppressionCounts; testid: string }>()

const {t, locale} = useI18n()
const {date} = useFormat()

const wider = computed(() => widerKnown(props.counts))
const reach = computed(() => otherKeys(props.counts))
</script>

<template>
  <div class="flex flex-col gap-0.5 text-sm" :data-testid="testid">
    <span class="font-medium">{{ heading }}</span>
    <span>
      {{ t('suppression.context.inCase', {n: counts.countInCase}, counts.countInCase) }}
      <template v-if="counts.firstSeenDate && counts.lastSeenDate">
        {{
          t('suppression.context.range', {
            first: date(counts.firstSeenDate),
            last: date(counts.lastSeenDate),
          })
        }}
      </template>
    </span>
    <span v-if="wider" key="wider">
      {{
        t(
          'suppression.context.wider',
          {
            keys: widerNumber(reach, counts.widerCapped, locale),
            transactions: widerNumber(counts.widerTransactions, counts.widerCapped, locale),
          },
          reach,
        )
      }}
    </span>
    <span v-else key="unknown">{{ t('suppression.context.widerUnknown') }}</span>
  </div>
</template>
