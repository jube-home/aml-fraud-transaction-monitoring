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
import { computed, onBeforeUnmount, onMounted, ref, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { Lock, RotateCcw, TriangleAlert, WandSparkles } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { ruleBuilderApi } from '../ruleBuilder/api'
import { filtersFromCompletions, showsBuilder } from '../ruleBuilder/filters'
import { formatRuleText } from '../ruleBuilder/format'
import { buildVocabulary, type Vocabulary } from '../ruleBuilder/intellisense/typeResolver'
import { toProblems, type Problem } from '../ruleBuilder/problems'
import { dropUnknownFields, emptyRuleText, ruleText } from '../ruleBuilder/ruleText'
import type {
  BuilderCoderValue,
  BuilderFilter,
  BuilderGroup,
  BuilderOperator,
  Completion,
  CompileState,
  ErrorSpan,
  RuleContext,
  RuleType,
  RuleWord,
} from '../ruleBuilder/types'
import BuilderStatus from './BuilderStatus.vue'
import RuleBuilderIsland from './RuleBuilderIsland.vue'
import RuleCoder from './RuleCoder.vue'

const props = defineProps<{
  parseType: number
  entityAnalysisModelId: number
  initialValue?: BuilderCoderValue
}>()

const emit = defineEmits<{
  change: [value: BuilderCoderValue]
  loadFailed: []
}>()

const settleDelayMs = 1300

const { t } = useI18n()
const queryClient = useQueryClient()

const builderShown = computed(() => showsBuilder(props.parseType))
const loaded = ref(false)
const loadFailed = ref(false)
const completions = ref<Completion[]>([])
const vocabulary = shallowRef<Vocabulary>(buildVocabulary([]))
const operators = ref<BuilderOperator[]>([])
const filters = ref<BuilderFilter[]>([])
const context = ref<RuleContext>({ operators: [], fieldTypes: {} })
const initialRules = ref<BuilderGroup>()
const droppedFields = ref<string[]>([])
const restoreFailed = ref(false)
const locked = ref(false)
const builderText = ref('')
const builderValid = ref(true)
const builderMessages = ref<string[]>([])
const coderText = ref('')
const compileState = ref<CompileState>('returnFalse')
const compileErrors = ref<ErrorSpan[]>([])
const problems = ref<Problem[]>([])
const island = ref<{
  validate: () => boolean
  getRules: (lenient?: boolean) => BuilderGroup | null
  validationMessages: () => string[]
  focusFirstError: () => void
}>()
const coder = ref<{
  focusAt: (line: number, column: number | null, endColumn: number | null) => void
}>()

let changeTimer: ReturnType<typeof setTimeout> | undefined
let compileTimer: ReturnType<typeof setTimeout> | undefined
let compileAbort: AbortController | undefined
let islandReady = false

const warning = computed(() => {
  if (droppedFields.value.length > 0) {
    return t('ruleBuilder.droppedFields', { fields: droppedFields.value.join(', ') })
  }

  return restoreFailed.value ? t('ruleBuilder.restoreFailed') : ''
})

function builderInControl(): boolean {
  return builderShown.value && !locked.value
}

function diverges(): boolean {
  return builderShown.value && formatRuleText(builderText.value) !== formatRuleText(coderText.value)
}

function recomputeBuilderText(): string {
  if (!islandReady || !island.value) return builderText.value

  let text = ''

  try {
    builderValid.value = island.value.validate()
    builderMessages.value = builderValid.value ? [] : island.value.validationMessages()

    const rules = island.value.getRules(true)

    if (rules) text = ruleText(context.value, rules, true)
  } catch {
    text = ''
  }

  builderText.value = text

  return text
}

function currentValue(): BuilderCoderValue {
  if (!builderShown.value) return { ruleTextCoder: coderText.value.trim() }

  const text = locked.value ? builderText.value : recomputeBuilderText()
  const rules = islandReady ? island.value?.getRules() : null
  const ruleType: RuleType = locked.value ? 2 : 1

  return {
    ruleTextBuilder: text === '' ? emptyRuleText : text,
    ruleJsonBuilder: JSON.stringify(
      rules ?? initialRules.value ?? { condition: 'AND', rules: [] },
      null,
      2,
    ),
    ruleTextCoder: coderText.value.trim(),
    ruleType,
  }
}

function emitChange(): void {
  if (!loaded.value) return

  emit('change', currentValue())
}

async function compile(): Promise<void> {
  compileAbort?.abort()

  const text = coderText.value
  const controlled = builderInControl()

  if (text.trim() === '') {
    compileErrors.value = []
    problems.value = []
    compileState.value = controlled ? 'incomplete' : 'returnFalse'

    return
  }

  const abort = new AbortController()

  compileAbort = abort
  compileState.value = 'checking'

  try {
    const result = await ruleBuilderApi.parse(
      props.parseType,
      text,
      props.entityAnalysisModelId,
      abort.signal,
    )

    if (abort.signal.aborted) return

    const spans = result.errorSpans ?? []
    compileErrors.value = spans
    problems.value = toProblems(spans, controlled ? builderMessages.value : [])
    compileState.value =
      spans.length > 0 || (controlled && !builderValid.value) ? 'errors' : 'compiled'
  } catch {
    if (abort.signal.aborted) return

    compileErrors.value = []
    problems.value = []
    compileState.value = 'disconnected'
  }
}

function setCoderText(next: string): void {
  coderText.value = next
  locked.value = diverges()

  clearTimeout(compileTimer)
  void compile()
  emitChange()
}

function onCoderInput(value: string): void {
  if (value === coderText.value) return

  coderText.value = value
  locked.value = diverges()

  clearTimeout(compileTimer)
  compileTimer = setTimeout(() => void compile(), settleDelayMs)
  emitChange()
}

function showProblem(problem: Problem): void {
  if (problem.source === 'builder') {
    island.value?.focusFirstError()

    return
  }

  if (problem.line !== null)
    coder.value?.focusAt(problem.line - 1, problem.column, problem.endColumn)
}

function onBuilderChanged(): void {
  clearTimeout(changeTimer)

  changeTimer = setTimeout(() => {
    if (locked.value) return

    recomputeBuilderText()
    setCoderText(formatRuleText(builderText.value))
  }, settleDelayMs)
}

function onBuilderReady(): void {
  islandReady = true

  if (initialRules.value && initialRules.value.rules.length > 0 && !locked.value) {
    recomputeBuilderText()
    setCoderText(formatRuleText(builderText.value))
  }
}

function reset(): void {
  recomputeBuilderText()
  setCoderText(formatRuleText(builderText.value))
}

function beautify(): void {
  setCoderText(formatRuleText(coderText.value))
}

async function loadVocabulary(): Promise<RuleWord[]> {
  try {
    const words = await queryClient.fetchQuery({
      queryKey: queryKeys.ruleVocabulary(),
      queryFn: () => ruleBuilderApi.vocabulary(),
      staleTime: Infinity,
      retry: false,
    })

    return Array.isArray(words) ? words : []
  } catch {
    return []
  }
}

onMounted(async () => {
  try {
    const [loadedCompletions, loadedOperators] = await Promise.all([
      queryClient.fetchQuery({
        queryKey: queryKeys.ruleCompletions(props.entityAnalysisModelId, props.parseType),
        queryFn: () => ruleBuilderApi.completions(props.entityAnalysisModelId, props.parseType),
      }),
      builderShown.value
        ? queryClient.fetchQuery({
            queryKey: queryKeys.ruleOperators(),
            queryFn: () => ruleBuilderApi.operators(),
            staleTime: 5 * 60 * 1000,
          })
        : Promise.resolve([] as BuilderOperator[]),
    ])

    completions.value = loadedCompletions
    operators.value = loadedOperators

    const built = filtersFromCompletions(loadedCompletions, loadedOperators)

    filters.value = built.filters
    context.value = built.context

    const stored = props.initialValue?.ruleJsonBuilder

    if (builderShown.value && stored) {
      try {
        const parsed = (typeof stored === 'string' ? JSON.parse(stored) : stored) as BuilderGroup
        const known = new Set(built.filters.map((filter) => filter.id))
        const cleaned = dropUnknownFields(parsed, (id) => known.has(id))

        droppedFields.value = cleaned.dropped
        initialRules.value = cleaned.group
      } catch {
        restoreFailed.value = true
      }
    }

    if (builderShown.value && initialRules.value && initialRules.value.rules.length > 0) {
      builderText.value = props.initialValue?.ruleTextBuilder ?? ''
    }

    if (builderShown.value && (props.initialValue?.ruleType ?? 1) === 1) {
      coderText.value = formatRuleText(builderText.value)
    } else {
      coderText.value = props.initialValue?.ruleTextCoder ?? ''
      locked.value = diverges()
    }

    loaded.value = true
    emitChange()

    void loadVocabulary().then((words) => {
      vocabulary.value = buildVocabulary(words)
    })

    void compile()
  } catch {
    loadFailed.value = true
    emit('loadFailed')
  }
})

onBeforeUnmount(() => {
  clearTimeout(changeTimer)
  clearTimeout(compileTimer)
  compileAbort?.abort()
})

function getValue(): BuilderCoderValue {
  return currentValue()
}

defineExpose({ getValue })
</script>

<template>
  <section
    class="flex flex-1 flex-col gap-3"
    :class="builderShown ? 'min-h-[34rem]' : 'min-h-72'"
    data-testid="builder-coder"
  >
    <div
      v-if="loadFailed"
      role="alert"
      class="alert alert-error"
      data-testid="builder-coder-load-failed"
    >
      <TriangleAlert aria-hidden="true" class="size-4" />
      <span>{{ t('ruleBuilder.loadFailed') }}</span>
    </div>

    <p v-else-if="!loaded" class="flex items-center gap-2 text-sm" role="status">
      <span class="loading loading-spinner loading-sm" aria-hidden="true"></span>
      {{ t('ruleBuilder.loading') }}
    </p>

    <template v-else>
      <div class="flex min-h-0 flex-1 flex-col gap-3">
        <section
          v-if="builderShown"
          class="flex max-h-[55%] min-h-0 min-w-0 shrink-0 flex-col gap-2"
          data-testid="builder-section"
        >
          <h3 class="text-sm font-semibold">{{ t('ruleBuilder.builder') }}</h3>

          <div v-if="locked" role="status" class="alert text-sm" data-testid="builder-locked">
            <Lock aria-hidden="true" class="size-4" />
            <span>{{ t('ruleBuilder.diverged') }}</span>
            <button type="button" class="btn btn-sm" data-testid="builder-reset" @click="reset">
              <RotateCcw aria-hidden="true" class="size-4" />
              {{ t('ruleBuilder.reset') }}
            </button>
          </div>

          <div
            :inert="locked"
            :aria-disabled="locked"
            class="flex min-h-0 min-w-0 flex-col gap-2 overflow-auto"
            :class="{ 'opacity-60': locked }"
            data-testid="builder-pane"
          >
            <RuleBuilderIsland
              ref="island"
              :filters="filters"
              :operators="operators"
              :context="context"
              :initial-rules="initialRules"
              @ready="onBuilderReady"
              @change="onBuilderChanged"
              @restore-failed="restoreFailed = true"
              @load-failed="loadFailed = true"
            />
            <p v-if="warning" role="status" class="alert text-sm" data-testid="builder-warning">
              <TriangleAlert aria-hidden="true" class="size-4" />
              <span>{{ warning }}</span>
            </p>
          </div>
        </section>

        <section class="flex min-h-0 min-w-0 flex-1 flex-col gap-2">
          <div class="flex items-center justify-between gap-2">
            <h3 class="text-sm font-semibold">{{ t('ruleBuilder.coder') }}</h3>
            <button type="button" class="btn btn-sm" data-testid="coder-beautify" @click="beautify">
              <WandSparkles aria-hidden="true" class="size-4" />
              {{ t('ruleBuilder.beautify') }}
            </button>
          </div>
          <RuleCoder
            ref="coder"
            :model-value="coderText"
            :completions="completions"
            :vocabulary="vocabulary"
            :errors="compileErrors"
            :label="t('ruleBuilder.coderLabel')"
            @update:model-value="onCoderInput"
          />
        </section>
      </div>
      <BuilderStatus :state="compileState" :problems="problems" @select="showProblem" />
    </template>
  </section>
</template>
