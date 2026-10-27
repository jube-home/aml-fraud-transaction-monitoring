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
import type { SqlFilter, SqlOperator } from '../filterBuilder/filters'
import {
  loadQueryBuilder,
  type QueryBuilderNode,
  type QueryBuilderPlugin,
  type QueryBuilderRule,
  type QueryBuilderSettings,
  type QueryBuilderSql,
} from '../ruleBuilder/queryBuilderPlugin'
import type { BuilderGroup } from '../ruleBuilder/types'

const props = withDefaults(
  defineProps<{
    filters: SqlFilter[]
    operators: SqlOperator[]
    initialRules?: BuilderGroup
    plugins?: string[]
    columns?: boolean
    addLabel?: string
    testId?: string
  }>(),
  { plugins: () => [], columns: false, addLabel: undefined, testId: undefined },
)

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

function settings(): QueryBuilderSettings {
  const operatorLabels: Record<string, string> = {
    equal: t('filterBuilder.operator.equal'),
    not_equal: t('filterBuilder.operator.notEqual'),
    less: t('filterBuilder.operator.less'),
    less_or_equal: t('filterBuilder.operator.lessOrEqual'),
    greater: t('filterBuilder.operator.greater'),
    greater_or_equal: t('filterBuilder.operator.greaterOrEqual'),
    like: t('filterBuilder.operator.like'),
    not_like: t('filterBuilder.operator.notLike'),
    begins_with: t('filterBuilder.operator.beginsWith'),
    contains: t('filterBuilder.operator.contains'),
    ends_with: t('filterBuilder.operator.endsWith'),
    order: t('filterBuilder.operator.order'),
    not_contains: t('filterBuilder.operator.notContains'),
    is_empty: t('filterBuilder.operator.isEmpty'),
    is_not_empty: t('filterBuilder.operator.isNotEmpty'),
    is_null: t('filterBuilder.operator.isNull'),
    is_not_null: t('filterBuilder.operator.isNotNull'),
  }

  return {
    plugins: props.plugins,
    filters: props.filters,
    operators: props.operators,
    icons: { add_group: '', add_rule: '', remove_group: '', remove_rule: '', error: '' },
    lang: {
      operators: operatorLabels,
      optgroups: { basic: t('filterBuilder.operatorGroup') },
      add_rule: props.addLabel ?? t('ruleBuilder.addRule'),
      add_group: t('ruleBuilder.addGroup'),
      delete_rule: t('ruleBuilder.deleteRule'),
      delete_group: t('ruleBuilder.deleteGroup'),
      invert: t('ruleBuilder.not'),
      conditions: { AND: t('ruleBuilder.and'), OR: t('ruleBuilder.or') },
      errors: {
        no_filter: t('filterBuilder.error.noFilter'),
        empty_group: t('filterBuilder.error.emptyGroup'),
        radio_empty: t('filterBuilder.error.radioEmpty'),
        select_empty: t('filterBuilder.error.selectEmpty'),
        string_empty: t('filterBuilder.error.stringEmpty'),
        number_nan: t('filterBuilder.error.numberNaN'),
        number_not_integer: t('filterBuilder.error.numberNotInteger'),
        number_not_double: t('filterBuilder.error.numberNotDouble'),
        datetime_empty: t('filterBuilder.error.datetimeEmpty'),
        datetime_invalid: t('filterBuilder.error.datetimeInvalid', { format: '{0}' }),
      },
    },
  }
}

function definedRules(node: QueryBuilderNode): number {
  if (node.rules) return node.rules.reduce((total, child) => total + definedRules(child), 0)

  return node.filter ? 1 : 0
}

function hasRules(): boolean {
  if (!instance) return false

  return definedRules(plugin.queryBuilder('getModel')) > 0
}

function getRules(): BuilderGroup | null {
  if (!instance) return null

  if (!hasRules()) return { condition: 'AND', rules: [] }

  return plugin.queryBuilder('getRules', { allow_invalid: true, skip_empty: true })
}

function validate(): boolean {
  if (!instance || !hasRules()) return false

  return Boolean(plugin.queryBuilder('validate'))
}

function getSql(): QueryBuilderSql | null {
  if (!instance || !validate()) return null

  return plugin.queryBuilder('getSQL', 'numbered(@)')
}

function validationMessages(): string[] {
  const messages: string[] = []

  host.value?.querySelectorAll('.rule-container.has-error .error-container').forEach((element) => {
    const text = element.getAttribute('title')

    if (text && !messages.includes(text)) messages.push(text)
  })

  return messages
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

onMounted(async () => {
  try {
    const jquery = await loadQueryBuilder()

    if (!host.value) return

    const element = jquery(host.value)

    element.on('input.filterBuilder', '.rule-value-container :input', (event) => {
      jquery(event.target).trigger('change')
    })
    element.on(
      'afterCreateRuleFilters.queryBuilder',
      (_event: JQuery.Event, rule: QueryBuilderRule) => {
        rule.$el.find('.rule-filter-container select').attr('aria-label', t('ruleBuilder.field'))
      },
    )
    element.on(
      'afterCreateRuleInput.queryBuilder',
      (_event: JQuery.Event, rule: QueryBuilderRule) => {
        rule.$el
          .find('.rule-value-container :input')
          .not('[type=radio]')
          .attr('aria-label', t('ruleBuilder.value'))
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
  instance.off('.filterBuilder')

  try {
    plugin.queryBuilder('destroy')
  } catch {
    instance.empty()
  }

  instance = undefined
})

defineExpose({ getRules, getSql, hasRules, validate, validationMessages, focusFirstError })
</script>

<template>
  <div
    ref="host"
    class="rule-builder-island"
    :data-testid="testId"
    :data-columns="columns ? '' : undefined"
    :aria-busy="!failed && !loaded"
  ></div>
</template>
