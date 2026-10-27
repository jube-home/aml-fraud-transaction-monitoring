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
import type { Problem } from '../ruleBuilder/problems'
import type { CompileState } from '../ruleBuilder/types'

defineProps<{
  state: CompileState
  problems: Problem[]
}>()

const emit = defineEmits<{ select: [problem: Problem] }>()

const { t } = useI18n()
</script>

<template>
  <div class="flex shrink-0 flex-col items-start gap-1.5" data-testid="compile-footer">
    <p class="flex items-center gap-2" role="status" data-testid="compile-status">
      <span
        class="badge badge-sm font-mono"
        :class="{
          'badge-success': state === 'compiled' || state === 'valid',
          'badge-error': state === 'errors' || state === 'disconnected',
        }"
      >
        <Check v-if="state === 'compiled' || state === 'valid'" aria-hidden="true" class="size-3" />
        <CircleX v-else-if="state === 'errors'" aria-hidden="true" class="size-3" />
        <WifiOff v-else-if="state === 'disconnected'" aria-hidden="true" class="size-3" />
        <Loader v-else-if="state === 'checking'" aria-hidden="true" class="size-3" />
        <CircleDashed v-else-if="state === 'incomplete'" aria-hidden="true" class="size-3" />
        <template v-if="state === 'compiled'">{{ t('ruleBuilder.compiled') }}</template>
        <template v-else-if="state === 'valid'">{{ t('ruleBuilder.valid') }}</template>
        <template v-else-if="state === 'errors'">{{ t('ruleBuilder.errors') }}</template>
        <template v-else-if="state === 'disconnected'">{{
          t('ruleBuilder.disconnected')
        }}</template>
        <template v-else-if="state === 'checking'">{{ t('ruleBuilder.checking') }}</template>
        <template v-else-if="state === 'incomplete'">{{ t('ruleBuilder.incomplete') }}</template>
        <template v-else>{{ t('ruleBuilder.returnFalse') }}</template>
      </span>
    </p>

    <ul
      v-if="problems.length > 0"
      :aria-label="t('ruleBuilder.compileMessages')"
      class="flex max-h-40 w-full flex-col gap-1 overflow-auto text-sm"
      data-testid="compile-messages"
    >
      <li v-for="(problem, position) in problems" :key="`${position}-${problem.text}`">
        <button
          type="button"
          class="-mx-1 flex items-center gap-2 rounded-field px-1 py-0.5 text-start hover:bg-base-200 focus-visible:outline-2 focus-visible:outline-primary"
          :data-testid="`problem-${problem.source}`"
          @click="emit('select', problem)"
        >
          <span class="badge badge-sm shrink-0 font-mono badge-soft">
            {{
              problem.source === 'builder'
                ? t('ruleBuilder.sourceBuilder')
                : t('ruleBuilder.sourceCompiler')
            }}
          </span>
          <span class="min-w-0 wrap-break-word">
            {{ problem.text }}
            <span v-if="problem.line !== null" class="font-mono text-secondary"
              >({{ t('ruleBuilder.lineNumber', { line: problem.line }) }})</span
            >
          </span>
        </button>
      </li>
    </ul>
  </div>
</template>
