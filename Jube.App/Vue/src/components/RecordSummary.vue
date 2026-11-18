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
import {Check, Minus} from 'lucide-vue-next'
import {useI18n} from 'vue-i18n'
import {useFormat} from '../composables/useFormat'
import {parentValueOf, recordName} from '../formModel/hierarchy'
import {recordFactGroups} from '../formModel/recordFacts'
import type {FormChild, FormModel, RecordValues} from '../formModel/types'
import {withUnsetChoices} from '../formModel/values'
import type {StoryStep} from '../story/path'
import GridRowSummary from './GridRowSummary.vue'
import HubDeck from './HubDeck.vue'
import PanelCard from './PanelCard.vue'
import RecordGlimpse from './RecordGlimpse.vue'

const props = defineProps<{
  model: FormModel
  row: RecordValues
  listed?: string[]
}>()

const emit = defineEmits<{ open: [steps: StoryStep[]] }>()

const {t} = useI18n()
const {dateTime} = useFormat()

const id = computed(() => props.row[props.model.keys.id] as number)

const groups = computed(() =>
  recordFactGroups(
    props.model,
    withUnsetChoices(props.model, props.row),
    {yes: t('crud.yes'), no: t('crud.no'), dateTime},
    props.listed,
  ),
)

const children = computed(() => [...props.model.children].sort((a, b) => a.order - b.order))

function parentOf(child: FormChild): number | string | undefined {
  const guid = props.row.guid

  return parentValueOf(child, props.model, {
    id: id.value,
    guid: typeof guid === 'string' && guid !== '' ? guid : undefined,
    name: recordName(props.model, props.row, id.value),
    values: props.row,
  })
}
</script>

<template>
  <GridRowSummary :testid="`crud-summary-${id}`">
    <p v-if="groups.length === 0 && children.length === 0" key="empty" class="text-secondary">
      {{ t('crud.summaryEmpty') }}
    </p>
    <section
      v-if="children.length > 0"
      key="children"
      class="flex flex-col gap-2"
      :aria-label="t('crud.summaryBelow')"
      :data-testid="`crud-summary-${id}-children`"
    >
      <HubDeck>
        <ul class="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
          <RecordGlimpse
            v-for="child in children"
            :key="child.area"
            :child="child"
            :parent-value="parentOf(child)"
            @open="(below) => emit('open', [{area: model.area, id}, ...below])"
          />
        </ul>
      </HubDeck>
    </section>
    <section
      v-if="groups.length > 0"
      key="facts"
      class="flex flex-col gap-3"
      :aria-label="t('crud.properties')"
      :data-testid="`crud-summary-${id}-facts`"
    >
      <PanelCard
        v-for="group in groups"
        :key="group.path"
        compact
        :bodyless="group.sections.length === 0"
        :title="group.label || t('crud.properties')"
        :data-testid="`crud-summary-${id}-group-${group.path}`"
      >
        <template v-if="group.feature" #actions>
          <span
            :class="[
              'badge badge-sm ms-auto font-mono',
              group.feature === 'on' ? 'badge-accent' : 'badge-soft',
            ]"
            :data-testid="`crud-summary-${id}-feature-${group.path}`"
            >{{ group.feature === 'on' ? t('rule.on') : t('rule.off') }}</span
          >
        </template>
        <div v-for="section in group.sections" :key="section.path" class="flex flex-col gap-1">
          <h4 v-if="section.label" class="text-xs font-semibold text-secondary">
            {{ section.label }}
          </h4>
          <dl class="grid gap-x-8 sm:grid-cols-2">
            <div
              v-for="fact in section.facts"
              :key="fact.label"
              class="grid min-w-0 grid-cols-[minmax(0,16rem)_minmax(0,1fr)] items-center gap-4 py-1"
              :class="fact.value.length > 24 ? 'sm:col-span-2' : ''"
            >
              <dt class="truncate text-secondary" :title="fact.label">{{ fact.label }}</dt>
              <dd
                dir="auto"
                class="flex min-w-0 items-center gap-1.5 font-medium"
                :class="[
                  fact.mono ? 'font-mono tabular-nums' : '',
                  fact.state === 'off' ? 'text-secondary' : '',
                ]"
                :title="fact.value"
              >
                <Check
                  v-if="fact.state === 'on'"
                  key="on"
                  class="size-3.5 shrink-0 text-success"
                  aria-hidden="true"
                />
                <Minus
                  v-else-if="fact.state === 'off'"
                  key="off"
                  class="size-3.5 shrink-0"
                  aria-hidden="true"
                />
                <span class="truncate">{{ fact.value }}</span>
              </dd>
            </div>
          </dl>
        </div>
      </PanelCard>
    </section>
  </GridRowSummary>
</template>
