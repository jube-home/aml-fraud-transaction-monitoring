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
import { computed, onMounted, ref, useSlots, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { decodeAreaAddress, encodeAreaAddress } from '../areaStory/path'
import type { AreaRoot } from '../areaStory/types'
import { formModelApi } from '../formModel/api'
import {
  newLevel,
  parentValueOf,
  recordName,
  type Level,
  type OpenedRecord,
} from '../formModel/hierarchy'
import { useFormModels } from '../formModel/useFormModels'
import type { FormChild, FormModel, RecordValues } from '../formModel/types'
import { recordFacts, type FactLabels } from '../story/facts'
import { whereOf, type Beat } from '../story/model'
import type { StoryStep } from '../story/path'
import AreaRecordBeat from './AreaRecordBeat.vue'
import AreaRootCard from './AreaRootCard.vue'
import ConfirmDialog from './ConfirmDialog.vue'
import CrudPanel from './CrudPanel.vue'
import NewRecordBeat from './NewRecordBeat.vue'
import StorySpine from './StorySpine.vue'

const props = defineProps<{
  path?: string
  title: string
  roots: AreaRoot[]
  views: Record<string, string>
  solo?: boolean
}>()

const emit = defineEmits<{ path: [text: string] }>()

defineSlots<{
  extra?(props: {
    rootId: string
    record: OpenedRecord
    openView: (view: string) => void
    goto: (rootId: string, id: number) => void
  }): unknown
  view?(props: {
    rootId: string
    view: string | undefined
    record: OpenedRecord | undefined
    setDirty: (dirty: boolean) => void
  }): unknown
}>()

const slots = useSlots()
const { t } = useI18n()
const { models } = useFormModels()

const rootId = ref<string>()
const record = ref<OpenedRecord>()
const recordView = ref<string>()
const creating = ref(false)
const levels = ref<Level[]>([])
const pending = ref<StoryStep[]>([])
const wanted = ref<number>()
const wantedView = ref<string>()
const dirty = ref(false)
const leaving = ref<(() => void) | undefined>()

const solo = computed(() => props.solo === true && props.roots.length > 0)
const root = computed(() => props.roots.find((candidate) => candidate.id === rootId.value))
const rootForm = computed(() =>
  models.value.find((candidate) => candidate.area === root.value?.area),
)

const labels = computed<FactLabels>(() => ({
  active: t('models.active'),
  inactive: t('models.inactive'),
  locked: t('models.locked'),
  version: (version) => t('models.version', { version }),
  awaiting: (count) => t('models.awaitingFact', { count }),
}))

const lookup = useQuery({
  queryKey: computed(() => queryKeys.formRecords(root.value?.area ?? '', undefined)),
  queryFn: () => formModelApi.records(rootForm.value!, undefined),
  enabled: computed(() => wanted.value !== undefined && rootForm.value !== undefined),
  retry: false,
})

function modelOf(area: string): FormModel | undefined {
  return models.value.find((candidate) => candidate.area === area)
}

function opened(values: RecordValues): OpenedRecord {
  const id = values.id as number
  const guid = typeof values.guid === 'string' ? values.guid : undefined

  return {
    id,
    guid,
    name: rootForm.value ? recordName(rootForm.value, values, id) : `#${id}`,
    values,
  }
}

function publish(): void {
  emit(
    'path',
    encodeAreaAddress({
      rootId: rootId.value,
      recordId: record.value?.id,
      view: recordView.value,
      steps: levels.value.flatMap((level) =>
        level.record ? [{ area: level.area, id: level.record.id }] : [],
      ),
    }),
  )
}

function recordBeatId(level: Level): string {
  return `${level.key}#record`
}

function newBeatId(level: Level): string {
  return `${level.key}#new`
}

function levelOfBeat(id: string): { index: number; part: 'list' | 'record' | 'new' } | undefined {
  const index = levels.value.findIndex(
    (level) => level.key === id || recordBeatId(level) === id || newBeatId(level) === id,
  )
  const level = levels.value[index]

  if (!level) return undefined

  return {
    index,
    part: recordBeatId(level) === id ? 'record' : newBeatId(level) === id ? 'new' : 'list',
  }
}

function levelIndex(id: string): number {
  return levelOfBeat(id)?.index ?? -1
}

function isRecordBeat(id: string): boolean {
  return levelOfBeat(id)?.part === 'record'
}

function isNewBeat(id: string): boolean {
  return levelOfBeat(id)?.part === 'new'
}

const builtBeats = computed<Beat[]>(() => {
  const list: Beat[] = solo.value ? [] : [{ id: 'entry', title: props.title, facts: [] }]
  const current = root.value

  if (!current) return list

  if (!current.area) {
    list.push({ id: 'view', title: current.title, facts: [] })
    return list
  }

  list.push({ id: 'root', title: current.title, facts: [] })

  if (creating.value) {
    list.push({ id: 'create', title: current.singular, name: t('story.newName'), facts: [] })
    return list
  }

  const opened = record.value

  if (!opened) return list

  list.push({
    id: 'record',
    title: current.singular,
    name: opened.name,
    facts: recordFacts(rootForm.value, opened.values, labels.value),
  })

  if (recordView.value) {
    list.push({
      id: `view:${recordView.value}`,
      title: props.views[recordView.value] ?? recordView.value,
      facts: [],
    })
    return list
  }

  for (const level of levels.value) {
    const form = modelOf(level.area)

    if (level.record) {
      list.push({
        id: recordBeatId(level),
        title: level.label,
        name: level.record.name,
        facts: recordFacts(form, level.record.values, labels.value),
      })
    } else if (level.startNew) {
      list.push({ id: newBeatId(level), title: level.label, name: t('story.newName'), facts: [] })
    } else {
      list.push({ id: level.key, title: level.label, facts: [] })
    }
  }

  return list
})

const beats = computed<Beat[]>(() =>
  builtBeats.value.map((beat, index, all) =>
    index === all.length - 1 && dirty.value ? { ...beat, unsaved: true } : beat,
  ),
)

const leavingFrom = computed(() => {
  const current = beats.value[beats.value.length - 1]

  return current ? whereOf(current) : ''
})

onMounted(() => {
  const address = decodeAreaAddress(
    props.path,
    props.roots.map((candidate) => candidate.id),
  )

  rootId.value = solo.value ? props.roots[0]!.id : address.rootId
  wanted.value = address.recordId
  wantedView.value = address.view
  pending.value = address.steps
})

function levelFor(child: FormChild, startNew: boolean, initialId?: number): Level {
  const parent = child.parentField === 'guid' ? record.value?.guid : record.value?.id

  return newLevel(child.area, child.label, {
    parentValue: parent,
    listRoute: child.listRoute,
    initialId,
    startNew,
  })
}

function settleDeepLink(): void {
  const values = lookup.data.value?.find((row) => row.id === wanted.value)

  wanted.value = undefined

  if (!values) return

  record.value = opened(values)

  if (wantedView.value) {
    recordView.value = wantedView.value
    wantedView.value = undefined
    return
  }

  const next = pending.value[0]
  const child = next && rootForm.value?.children.find((candidate) => candidate.area === next.area)

  if (child && next) {
    pending.value = pending.value.slice(1)
    levels.value = [levelFor(child, false, next.id)]
  }
}

watch(
  () => wanted.value !== undefined && lookup.isSuccess.value,
  (ready) => {
    if (!ready) return

    settleDeepLink()
    publish()
  },
)

function guarded(action: () => void): void {
  if (!dirty.value) {
    action()
    return
  }

  leaving.value = action
}

function keepEditing(): void {
  leaving.value = undefined
}

function discard(): void {
  const action = leaving.value

  leaving.value = undefined
  dirty.value = false
  action?.()
}

function setDirty(value: boolean): void {
  dirty.value = value
}

function clearRecord(): void {
  record.value = undefined
  recordView.value = undefined
  creating.value = false
  levels.value = []
}

function settleReopen(id: string): void {
  if (id === 'entry') {
    clearRecord()
    rootId.value = undefined
  } else if (id === 'root') {
    clearRecord()
  } else if (id === 'record') {
    recordView.value = undefined
    levels.value = []
  } else {
    const found = levelOfBeat(id)

    if (found) levels.value = levels.value.slice(0, found.index + 1)
  }

  publish()
}

function reopen(index: number): void {
  const id = beats.value[index]?.id

  if (id === undefined) return

  if (index < beats.value.length - 1) guarded(() => settleReopen(id))
  else settleReopen(id)
}

function chooseRoot(id: string): void {
  guarded(() => {
    clearRecord()
    rootId.value = id
    publish()
  })
}

function chooseRecord(chosen: OpenedRecord): void {
  record.value = chosen
  recordView.value = undefined
  levels.value = []
  publish()
}

function openView(view: string): void {
  guarded(() => {
    recordView.value = view
    levels.value = []
    publish()
  })
}

function goto(id: string, recordId: number): void {
  guarded(() => {
    clearRecord()
    rootId.value = id
    wanted.value = recordId
    publish()
  })
}

function backToRoot(): void {
  guarded(() => {
    clearRecord()
    publish()
  })
}

function fromHub(child: FormChild, startNew: boolean): void {
  guarded(() => {
    levels.value = [levelFor(child, startNew)]
    publish()
  })
}

function descend(index: number, child: FormChild, startNew: boolean, initialId?: number): void {
  const parent = levels.value[index]
  const form = parent && modelOf(parent.area)

  if (!parent?.record || !form) return

  levels.value = [
    ...levels.value.slice(0, index + 1),
    newLevel(child.area, child.label, {
      parentValue: parentValueOf(child, form, parent.record),
      listRoute: child.listRoute,
      initialId,
      startNew,
    }),
  ]
  publish()
}

function fromRecord(index: number, child: FormChild, startNew: boolean): void {
  guarded(() => descend(index, child, startNew))
}

function levelOpened(index: number, opened: OpenedRecord): void {
  const current = levels.value[index]

  if (!current) return

  const same = current.record?.id === opened.id

  levels.value = [
    ...levels.value.slice(0, index),
    { ...current, record: opened, startNew: false },
    ...(same ? levels.value.slice(index + 1) : []),
  ]

  const next = pending.value[0]
  const child =
    next && modelOf(current.area)?.children.find((candidate) => candidate.area === next.area)

  if (child && next && levels.value.length === index + 1) {
    pending.value = pending.value.slice(1)
    descend(index, child, false, next.id)
    return
  }

  publish()
}

function levelClosed(index: number): void {
  const current = levels.value[index]

  if (!current?.record) return

  levels.value = [
    ...levels.value.slice(0, index),
    { ...current, record: undefined, initialId: undefined },
  ]
  publish()
}

function startLevelNew(index: number): void {
  const current = levels.value[index]

  if (!current || current.record) return

  levels.value = [
    ...levels.value.slice(0, index),
    { ...current, startNew: true, initialId: undefined },
  ]
  publish()
}

function cancelLevelNew(index: number): void {
  guarded(() => {
    const current = levels.value[index]

    if (!current) return

    levels.value = [
      ...levels.value.slice(0, index),
      { ...current, startNew: false, record: undefined, initialId: undefined },
    ]
    publish()
  })
}

function backFromLevel(index: number): void {
  guarded(() => levelClosed(index))
}

function recordUpdated(updated: OpenedRecord): void {
  if (record.value?.id === updated.id) record.value = updated
}

function cancelCreate(): void {
  guarded(() => {
    creating.value = false
    publish()
  })
}

function createdOpened(created: OpenedRecord): void {
  record.value = created
  creating.value = false
  recordView.value = undefined
  levels.value = []
  publish()
}
</script>

<template>
  <StorySpine :beats="beats" @reopen="reopen">
    <template #default="{ beat }">
      <div
        v-if="beat.id === 'entry'"
        key="entry"
        class="grid grid-cols-[repeat(auto-fill,minmax(15rem,1fr))] gap-3"
        data-testid="area-entry"
      >
        <AreaRootCard
          v-for="candidate in roots"
          :key="candidate.id"
          :root="candidate"
          @open="chooseRoot(candidate.id)"
        />
      </div>

      <template v-else-if="beat.id === 'view' && rootId">
        <slot
          name="view"
          :root-id="rootId"
          :view="undefined"
          :record="undefined"
          :set-dirty="setDirty"
        />
      </template>

      <CrudPanel
        v-else-if="beat.id === 'root' && root?.area"
        :key="`list-${root.id}`"
        :area="root.area"
        external-new
        @opened="chooseRecord"
        @create="creating = true"
        @dirty="setDirty"
      />

      <NewRecordBeat
        v-else-if="beat.id === 'create' && root?.area"
        key="create"
        :area="root.area"
        :back-label="root.title"
        @opened="createdOpened"
        @back="cancelCreate"
        @dirty="setDirty"
      />

      <AreaRecordBeat
        v-else-if="beat.id === 'record' && record && root"
        key="record"
        :record="record"
        :model="rootForm"
        :models="models"
        :back-label="root.title"
        @child="fromHub"
        @back="backToRoot"
        @opened="recordUpdated"
        @dirty="setDirty"
      >
        <template v-if="slots.extra && root.extras" #extra>
          <slot
            name="extra"
            :root-id="root.id"
            :record="record"
            :open-view="openView"
            :goto="goto"
          />
        </template>
      </AreaRecordBeat>

      <template v-else-if="beat.id.startsWith('view:') && record && rootId">
        <slot
          name="view"
          :root-id="rootId"
          :view="recordView"
          :record="record"
          :set-dirty="setDirty"
        />
      </template>

      <CrudPanel
        v-else-if="levelIndex(beat.id) >= 0 && !isRecordBeat(beat.id) && !isNewBeat(beat.id)"
        :key="`list-${beat.id}`"
        :area="levels[levelIndex(beat.id)]!.area"
        :parent-id="levels[levelIndex(beat.id)]!.parentValue"
        :list-route="levels[levelIndex(beat.id)]!.listRoute"
        :initial-id="levels[levelIndex(beat.id)]!.initialId"
        external-new
        @opened="(opened) => levelOpened(levelIndex(beat.id), opened)"
        @closed="levelClosed(levelIndex(beat.id))"
        @create="startLevelNew(levelIndex(beat.id))"
        @dirty="setDirty"
      />

      <NewRecordBeat
        v-else-if="levelIndex(beat.id) >= 0 && isNewBeat(beat.id)"
        :key="`new-${beat.id}`"
        :area="levels[levelIndex(beat.id)]!.area"
        :back-label="levels[levelIndex(beat.id)]!.label"
        :parent-id="levels[levelIndex(beat.id)]!.parentValue"
        :list-route="levels[levelIndex(beat.id)]!.listRoute"
        @opened="(opened) => levelOpened(levelIndex(beat.id), opened)"
        @back="cancelLevelNew(levelIndex(beat.id))"
        @dirty="setDirty"
      />

      <AreaRecordBeat
        v-else-if="levelIndex(beat.id) >= 0 && levels[levelIndex(beat.id)]!.record"
        :key="`record-${beat.id}`"
        :record="levels[levelIndex(beat.id)]!.record!"
        :model="modelOf(levels[levelIndex(beat.id)]!.area)"
        :models="models"
        :back-label="levels[levelIndex(beat.id)]!.label"
        :parent-id="levels[levelIndex(beat.id)]!.parentValue"
        :list-route="levels[levelIndex(beat.id)]!.listRoute"
        @child="(child, startNew) => fromRecord(levelIndex(beat.id), child, startNew)"
        @back="backFromLevel(levelIndex(beat.id))"
        @opened="(opened) => levelOpened(levelIndex(beat.id), opened)"
        @dirty="setDirty"
      />
    </template>
  </StorySpine>

  <ConfirmDialog
    id="story-discard"
    :open="leaving !== undefined"
    :title="t('story.discard.title')"
    :body="t('story.discard.body', { where: leavingFrom })"
    :confirm-label="t('story.discard.confirm')"
    :cancel-label="t('story.discard.cancel')"
    @confirm="discard"
    @cancel="keepEditing"
  />
</template>
