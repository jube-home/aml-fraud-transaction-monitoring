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
import { computed, ref, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { Filter, TriangleAlert } from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import FilterBuilderIsland from '../../components/FilterBuilderIsland.vue'
import { filterBuilderApi } from '../../filterBuilder/api'
import {
  caseWhereOperators,
  exhaustiveFilters,
  type FilterIslandApi,
} from '../../filterBuilder/filters'
import { prepareInitialRules } from '../../filterBuilder/initial'
import type { BuilderGroup } from '../../ruleBuilder/types'

const props = withDefaults(
  defineProps<{
    workflow: string
    filter: BuilderGroup | null
    name?: string
    help?: string
  }>(),
  { name: 'journal' },
)

const emit = defineEmits<{ apply: [filter: BuilderGroup | null] }>()

const { t } = useI18n()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const island = useTemplateRef<FilterIslandApi>('island')
const open = ref(false)
const requested = ref(false)
const problems = ref<string[]>([])

const completions = useQuery({
  queryKey: computed(() => ['service', 'Case', 'keyjournal', 'fields', props.workflow] as const),
  queryFn: () => filterBuilderApi.caseCompletions({ kind: 'guid', value: props.workflow }),
  enabled: requested,
  staleTime: 5 * 60_000,
  retry: false,
})

const filters = computed(() => exhaustiveFilters(completions.data.value ?? []))

const restored = computed(() =>
  prepareInitialRules(props.filter ?? undefined, new Set(filters.value.map((item) => item.id))),
)

function show(): void {
  problems.value = []
  requested.value = true
  open.value = true
  dialog.value?.showModal()
}

function hide(): void {
  dialog.value?.close?.()
  open.value = false
}

function apply(): void {
  const current = island.value

  if (!current) return

  if (!current.hasRules()) {
    emit('apply', null)
    hide()

    return
  }

  if (!current.validate()) {
    problems.value = current.validationMessages()
    current.focusFirstError()

    return
  }

  emit('apply', current.getRules())
  hide()
}

function clear(): void {
  emit('apply', null)
  hide()
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-1" :data-testid="`${name}-filter`">
    <button type="button" class="btn btn-sm" :data-testid="`${name}-filter-open`" @click="show">
      <Filter class="size-4" aria-hidden="true" />
      {{ t('journal.filter.addRule') }}
    </button>
    <FieldHelp :label="t('journal.filter.label')" :text="help ?? t('journal.help.filter')" />

    <dialog
      ref="dialog"
      class="modal"
      :data-testid="`${name}-filter-dialog`"
      @cancel.prevent="hide"
    >
      <div class="modal-box w-11/12 max-w-4xl">
        <h3 class="text-lg font-bold">{{ t('journal.filter.title') }}</h3>
        <p class="mt-1 text-sm">{{ t('journal.filter.hint') }}</p>

        <div class="mt-3 max-h-[60vh] overflow-y-auto" :data-testid="`${name}-filter-builder`">
          <p
            v-if="completions.isPending.value"
            key="loading"
            class="flex items-center gap-2 text-sm"
            role="status"
          >
            <span class="loading loading-spinner loading-sm" aria-hidden="true"></span>
            {{ t('journal.filter.loading') }}
          </p>
          <div
            v-else-if="completions.isError.value"
            key="failed"
            role="alert"
            class="alert alert-error"
            :data-testid="`${name}-filter-failed`"
          >
            <TriangleAlert aria-hidden="true" class="size-4" />
            <span>{{ t('journal.filter.failed') }}</span>
          </div>
          <p v-else-if="filters.length === 0" key="none" class="text-sm" role="status">
            {{ t('journal.filter.noFields') }}
          </p>
          <FilterBuilderIsland
            v-else-if="open"
            key="builder"
            ref="island"
            :filters="filters"
            :operators="caseWhereOperators"
            :initial-rules="restored.group"
            :add-label="t('journal.filter.addRule')"
          />
          <p
            v-if="restored.dropped.length > 0 && open"
            key="dropped"
            role="status"
            class="mt-2 text-sm"
            :data-testid="`${name}-filter-dropped`"
          >
            {{ t('filterBuilder.droppedFields', { fields: restored.dropped.join(', ') }) }}
          </p>
        </div>

        <ul v-if="problems.length > 0" key="problems" class="mt-2 text-sm" role="alert">
          <li v-for="problem in problems" :key="problem">{{ problem }}</li>
        </ul>

        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            :data-testid="`${name}-filter-cancel`"
            @click="hide"
          >
            {{ t('journal.filter.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-sm"
            :data-testid="`${name}-filter-clear`"
            @click="clear"
          >
            {{ t('journal.filter.clear') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-primary"
            :data-testid="`${name}-filter-apply`"
            @click="apply"
          >
            {{ t('journal.filter.apply') }}
          </button>
        </div>
      </div>
      <div class="modal-backdrop">
        <button
          type="button"
          tabindex="-1"
          :aria-label="t('journal.filter.cancel')"
          @click="hide"
        ></button>
      </div>
    </dialog>
  </div>
</template>
