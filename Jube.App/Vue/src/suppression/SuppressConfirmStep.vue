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
import type {PickedOverride} from '../overrides/selection'
import {isolate, targetName} from './state'
import type {CandidateKey} from './types'

const props = defineProps<{
  overrideKey: string
  overrideValue: string
  targets: PickedOverride[]
  names: Record<string, string>
  keys: CandidateKey[]
  reason: string
  expiry: string
  drafting: boolean
  expiryInvalid: boolean
}>()

const emit = defineEmits<{ 'update:reason': [text: string]; 'update:expiry': [text: string] }>()

const {t} = useI18n()

const lines = computed(() =>
  props.targets.map((target) => {
    const model = props.names[target.entityAnalysisModelGuid] ?? target.entityAnalysisModelGuid
    const name = targetName(model, target.ruleName)

    return {
      id: `${target.entityAnalysisModelGuid}|${target.ruleName ?? ''}`,
      name,
      kind: target.overrideKind === 1 ? t('override.kind.force') : t('override.kind.suppress'),
    }
  }),
)
</script>

<template>
  <div class="flex flex-col gap-4" data-testid="suppress-confirm">
    <section class="flex flex-col gap-2" :aria-label="t('suppression.confirm.summary')">
      <h4 class="text-sm font-semibold">{{ t('suppression.confirm.summary') }}</h4>
      <p class="text-sm">
        {{ t('suppression.confirm.on', {key: isolate(overrideKey)}) }}
        <span class="font-mono" dir="auto">{{ overrideValue }}</span>
      </p>
      <ul class="flex flex-col gap-1 text-sm" data-testid="suppress-confirm-targets">
        <li v-for="line in lines" :key="line.id" class="flex flex-wrap gap-2">
          <span class="badge badge-ghost badge-sm">{{ line.kind }}</span>
          <span>{{ line.name }}</span>
        </li>
      </ul>
      <p class="text-sm font-medium" data-testid="suppress-confirm-reach">
        {{ t('suppression.confirm.reach') }}
      </p>
    </section>

    <fieldset class="fieldset">
      <label class="label" for="suppress-expiry">{{ t('suppression.confirm.expiry') }}</label>
      <input
        id="suppress-expiry"
        type="datetime-local"
        class="input input-sm font-mono"
        :value="expiry"
        required
        data-testid="suppress-expiry"
        @input="emit('update:expiry', ($event.target as HTMLInputElement).value)"
      />
      <p v-if="expiryInvalid" key="expiryInvalid" class="label text-error" role="alert">
        {{ t('suppression.confirm.expiryInvalid') }}
      </p>
    </fieldset>

    <fieldset class="fieldset">
      <label class="label" for="suppress-reason">{{ t('suppression.confirm.reason') }}</label>
      <textarea
        id="suppress-reason"
        class="textarea min-h-32 w-full"
        dir="auto"
        :value="reason"
        maxlength="2000"
        required
        data-testid="suppress-reason"
        @input="emit('update:reason', ($event.target as HTMLTextAreaElement).value)"
      ></textarea>
      <p class="label">
        {{ drafting ? t('suppression.confirm.drafting') : t('suppression.confirm.reasonHint') }}
      </p>
    </fieldset>
  </div>
</template>
