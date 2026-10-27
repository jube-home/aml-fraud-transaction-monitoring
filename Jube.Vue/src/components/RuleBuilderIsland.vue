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
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import '../assets/ruleBuilder.css'
import { inputHtml, isCustomised, type InputText } from '../ruleBuilder/customInputs'
import {
  loadQueryBuilder,
  type QueryBuilderEvent,
  type QueryBuilderPlugin,
  type QueryBuilderRule,
  type QueryBuilderSettings,
} from '../ruleBuilder/queryBuilderPlugin'
import { operatorOf, validateRuleValue, type ArgumentFailure } from '../ruleBuilder/ruleText'
import type {
  BuilderFilter,
  BuilderGroup,
  BuilderOperator,
  OperatorArgument,
  RuleContext,
} from '../ruleBuilder/types'

const props = defineProps<{
  filters: BuilderFilter[]
  operators: BuilderOperator[]
  context: RuleContext
  initialRules?: BuilderGroup
}>()

const emit = defineEmits<{
  change: []
  ready: []
  restoreFailed: []
  loadFailed: []
}>()

const { t } = useI18n()
const host = ref<HTMLElement>()
const failed = ref(false)
const loaded = ref(false)

let plugin: JQuery & QueryBuilderPlugin
let instance: JQuery | undefined

const inputText: InputText = {
  placeholder: (argument: OperatorArgument, isDate: boolean) => {
    const base = argument.many
      ? t('ruleBuilder.placeholderMany', { name: argument.name })
      : argument.name

    return isDate ? t('ruleBuilder.placeholderDate', { text: base }) : base
  },
  title: (placeholder: string, referable: boolean) =>
    referable ? t('ruleBuilder.orAField', { text: placeholder }) : placeholder,
}

function failureMessage(failure: ArgumentFailure): string {
  const argument = failure.argument

  switch (failure.code) {
    case 'needed':
      return t('ruleBuilder.error.needed', { argument })
    case 'integer':
      return t('ruleBuilder.error.integer', { argument })
    case 'number':
      return t('ruleBuilder.error.number', { argument })
    case 'date':
      return t('ruleBuilder.error.date', { argument })
    case 'list':
      return t('ruleBuilder.error.list', { argument })
    case 'field':
      return t('ruleBuilder.error.field', { argument })
    default:
      return t('ruleBuilder.error.controlCharacters', { argument })
  }
}

function settings(): QueryBuilderSettings {
  const operatorLabels: Record<string, string> = {}
  const groupLabels: Record<string, string> = {}

  const operators = props.operators.map((operator) => {
    operatorLabels[operator.type] = operator.label
    groupLabels[operator.group] = operator.group

    return {
      type: operator.type,
      nb_inputs: operator.arguments.length,
      multiple: false,
      apply_to: ['string', 'number', 'datetime', 'boolean'],
      optgroup: operator.group,
    }
  })

  return {
    plugins: ['not-group'],
    filters: props.filters,
    operators,
    icons: {
      add_group: '',
      add_rule: '',
      remove_group: '',
      remove_rule: '',
      error: '',
    },
    lang: {
      operators: operatorLabels,
      optgroups: groupLabels,
      add_rule: t('ruleBuilder.addRule'),
      add_group: t('ruleBuilder.addGroup'),
      delete_rule: t('ruleBuilder.deleteRule'),
      delete_group: t('ruleBuilder.deleteGroup'),
      invert: t('ruleBuilder.not'),
      conditions: { AND: t('ruleBuilder.and'), OR: t('ruleBuilder.or') },
    },
  }
}

function bindInputs(element: JQuery): void {
  element
    .off('.builderInputs')
    .on(
      'getRuleInput.queryBuilder.filter.builderInputs',
      (event: JQuery.Event, rule: QueryBuilderRule, name: string) => {
        if (!isCustomised(props.context, rule)) return

        const index = Number(name.substring(name.lastIndexOf('_') + 1))

        ;(event as QueryBuilderEvent).value = inputHtml(
          props.context,
          inputText,
          rule.id,
          rule.filter.id,
          rule.operator.type,
          index,
        )
      },
    )
    .on(
      'getRuleValue.queryBuilder.filter.builderInputs',
      (event: JQuery.Event, rule: QueryBuilderRule) => {
        if (!isCustomised(props.context, rule)) return

        const container = rule.$el.find('.rule-value-container')
        const values: unknown[] = []

        for (let index = 0; index < rule.operator.nb_inputs; index++) {
          values.push(container.find(`[name="${rule.id}_value_${index}"]`).val())
        }

        ;(event as QueryBuilderEvent).value = values.length === 1 ? values[0] : values
      },
    )
    .on(
      'validateValue.queryBuilder.filter.builderInputs',
      (event: JQuery.Event, value: unknown, rule: QueryBuilderRule) => {
        if (!isCustomised(props.context, rule)) return

        const operator = operatorOf(props.context, rule.operator.type)

        if (!operator) return

        const failure = validateRuleValue(props.context, operator, rule.filter.id, value)

        ;(event as QueryBuilderEvent).value = failure ? [failureMessage(failure)] : true
      },
    )
}

function syncNotButtons(): void {
  host.value?.querySelectorAll('[data-not]').forEach((button) => {
    button.setAttribute('aria-pressed', String(button.classList.contains('active')))
  })
}

function focusFirstError(): void {
  const rule = host.value?.querySelector('.rule-container.has-error')

  if (!rule) return

  const target =
    rule.querySelector<HTMLElement>('.rule-value-container :is(input, select)') ??
    rule.querySelector<HTMLElement>('select')

  rule.scrollIntoView({ block: 'nearest' })
  target?.focus()
}

function getRules(lenient = false): BuilderGroup | null {
  if (!instance) return null

  return lenient
    ? plugin.queryBuilder('getRules', { allow_invalid: true, skip_empty: true })
    : plugin.queryBuilder('getRules')
}

function validationMessages(): string[] {
  const messages: string[] = []

  host.value?.querySelectorAll('.rule-container.has-error .error-container').forEach((element) => {
    const text = element.getAttribute('title')

    if (text && !messages.includes(text)) messages.push(text)
  })

  return messages
}

function validate(): boolean {
  if (!instance) return false

  const state = (instance as unknown as QueryBuilderPlugin).data('queryBuilder')

  if (!state?.filters || state.filters.length === 0) return false

  return Boolean(plugin.queryBuilder('validate'))
}

onMounted(async () => {
  try {
    const jquery = await loadQueryBuilder()

    if (!host.value) return

    const element = jquery(host.value)

    bindInputs(element)
    element.on('input.builderInputs', '.rule-value-container :input', (event) => {
      jquery(event.target).trigger('change')
    })
    element.on('afterUpdateGroupNot.queryBuilder afterAddGroup.queryBuilder', () =>
      queueMicrotask(syncNotButtons),
    )
    element.on(
      'afterCreateRuleFilters.queryBuilder',
      (_event: JQuery.Event, rule: QueryBuilderRule) => {
        rule.$el.find('.rule-filter-container select').attr('aria-label', t('ruleBuilder.field'))
      },
    )
    element.on(
      'afterCreateRuleOperators.queryBuilder',
      (_event: JQuery.Event, rule: QueryBuilderRule) => {
        rule.$el
          .find('.rule-operator-container select')
          .attr('aria-label', t('ruleBuilder.operator'))
      },
    )

    plugin = (element as unknown as QueryBuilderPlugin).queryBuilder(settings()) as JQuery &
      QueryBuilderPlugin
    instance = element

    if (props.initialRules && props.initialRules.rules.length > 0) {
      try {
        plugin.queryBuilder('setRules', props.initialRules)
      } catch {
        emit('restoreFailed')
      }
    }

    element.on(
      'rulesChanged.queryBuilder afterUpdateGroupNot.queryBuilder afterUpdateRuleValue.queryBuilder afterUpdateRuleFilter.queryBuilder afterUpdateRuleOperator.queryBuilder afterUpdateGroupCondition.queryBuilder',
      () => emit('change'),
    )

    syncNotButtons()
    loaded.value = true
    emit('ready')
  } catch {
    failed.value = true
    emit('loadFailed')
  }
})

onBeforeUnmount(() => {
  if (!instance) return

  instance.off('.queryBuilder')
  instance.off('.builderInputs')

  try {
    plugin.queryBuilder('destroy')
  } catch {
    instance.empty()
  }

  instance = undefined
})

defineExpose({ getRules, validate, validationMessages, focusFirstError })
</script>

<template>
  <div
    ref="host"
    class="rule-builder-island"
    data-testid="rule-builder"
    :aria-busy="!failed && !loaded"
  ></div>
</template>
