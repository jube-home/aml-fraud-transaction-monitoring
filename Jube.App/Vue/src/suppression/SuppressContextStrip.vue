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
import {Eye, EyeOff} from 'lucide-vue-next'
import {useFormat} from '../composables/useFormat'
import {isolate, otherKeys, widerKnown, widerNumber} from './state'

const props = defineProps<{
  caseCode: string
  caseKey: string
  caseKeyValue: string
  overrideKey: string | null
  overrideValue: string | null
  inCase: number | null
  first: string | null
  last: string | null
  widerCountStateId: number | null
  widerKeys: number | null
  widerTransactions: number | null
  widerCapped: boolean
  evidenceOpen: boolean
  evidenceCount: number
}>()

const emit = defineEmits<{ toggle: [] }>()

const {t, locale} = useI18n()
const {date} = useFormat()

const wider = computed(() =>
  widerKnown({
    widerCountStateId: props.widerCountStateId ?? 0,
    widerKeys: props.widerKeys,
    widerTransactions: props.widerTransactions,
  }),
)

const reach = computed(() => otherKeys({widerKeys: props.widerKeys}))
const range = computed(() =>
  props.first && props.last
    ? t('suppression.context.range', {first: date(props.first), last: date(props.last)})
    : '',
)
</script>

<template>
  <section
    class="sticky top-0 z-10 flex flex-col gap-2 rounded-box border border-base-300 bg-base-200 p-3 text-sm text-base-content"
    :aria-label="t('suppression.context.label')"
    data-testid="suppress-context"
  >
    <div class="flex flex-wrap items-center gap-x-4 gap-y-1">
      <span class="font-mono" data-testid="suppress-context-case">
        {{
          t('suppression.context.case', {
            code: caseCode,
            key: isolate(caseKey),
            value: isolate(caseKeyValue),
          })
        }}
      </span>
      <span v-if="overrideKey" key="key" data-testid="suppress-context-key">
        {{ t('suppression.context.key', {key: isolate(overrideKey)}) }}
        <span class="font-mono" dir="auto">{{ overrideValue ?? '' }}</span>
      </span>
      <span v-if="inCase !== null" key="incase" data-testid="suppress-context-count">
        {{ t('suppression.context.inCase', {n: inCase}, inCase) }}{{ range }}
      </span>
      <span v-if="inCase !== null && wider" key="wider" data-testid="suppress-context-wider">
        {{
          t(
            'suppression.context.wider',
            {
              keys: widerNumber(reach, widerCapped, locale),
              transactions: widerNumber(widerTransactions, widerCapped, locale),
            },
            reach,
          )
        }}
      </span>
      <span
        v-else-if="inCase !== null"
        key="unknown"
        class="font-medium"
        data-testid="suppress-context-unknown"
      >
        {{ t('suppression.context.widerUnknown') }}
      </span>
    </div>
    <div>
      <button
        type="button"
        class="btn btn-xs"
        :aria-expanded="evidenceOpen"
        data-testid="suppress-show-evidence"
        @click="emit('toggle')"
      >
        <EyeOff v-if="evidenceOpen" key="hide" class="size-3" aria-hidden="true"/>
        <Eye v-else key="show" class="size-3" aria-hidden="true"/>
        {{
          evidenceOpen
            ? t('suppression.context.hide')
            : t('suppression.context.show', {n: evidenceCount}, evidenceCount)
        }}
      </button>
    </div>
  </section>
</template>
