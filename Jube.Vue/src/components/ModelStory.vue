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
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { analyticsPermission, type InstanceRow } from '../askJoobyAnalytics/model'
import type { TrainedModel } from '../askJoobyAnalytics/types'
import { stateOf } from '../approval/model'
import { hasPermission, meQueryOptions } from '../auth/me'
import { useApprovalState } from '../composables/useApprovalState'
import { usePendingCounts } from '../composables/usePendingCounts'
import {
  approvalKey,
  useRecordApprovals,
  type ApprovalTarget,
} from '../composables/useRecordApprovals'
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
import { integrityPermission } from '../integrity/model'
import { recordFacts, type FactLabels } from '../story/facts'
import { whereOf, type Beat, type StoryFact } from '../story/model'
import {
  decodeAddress,
  encodeAddress,
  modelArea,
  type Operate,
  type StoryStep,
} from '../story/path'
import AskJoobyAnalyticsInstances from './AskJoobyAnalyticsInstances.vue'
import AskJoobyAnalyticsModelCanvas from './AskJoobyAnalyticsModelCanvas.vue'
import ConfirmDialog from './ConfirmDialog.vue'
import CrudPanel from './CrudPanel.vue'
import IntegrityCanvas from './IntegrityCanvas.vue'
import ModelOperateCards from './ModelOperateCards.vue'
import ModelSample from './ModelSample.vue'
import ModelsTable from './ModelsTable.vue'
import NewRecordBeat from './NewRecordBeat.vue'
import RecordBeat from './RecordBeat.vue'
import StorySpine from './StorySpine.vue'

const props = defineProps<{ path?: string }>()

const emit = defineEmits<{ path: [text: string] }>()

const samplePermission = 40

const { t } = useI18n()
const { models } = useFormModels()
const me = useQuery(meQueryOptions)

const model = ref<OpenedRecord>()
const operate = ref<Operate>()
const analytics = ref<{ model: TrainedModel; instance: InstanceRow }>()
const creating = ref(false)
const levels = ref<Level[]>([])
const pending = ref<StoryStep[]>([])
const wanted = ref<number>()
const wantedOperate = ref<Operate>()
const dirty = ref(false)
const leaving = ref<(() => void) | undefined>()

const modelForm = computed(() => models.value.find((candidate) => candidate.area === modelArea))
const canSample = computed(() => hasPermission(me.data.value, samplePermission))
const canIntegrity = computed(() => hasPermission(me.data.value, integrityPermission.view))
const canSynchronise = computed(() => hasPermission(me.data.value, integrityPermission.synchronise))
const canAnalytics = computed(() => hasPermission(me.data.value, ...analyticsPermission.view))
const canSetActive = computed(() => hasPermission(me.data.value, analyticsPermission.setActive))
const canDeleteAnalytics = computed(() => hasPermission(me.data.value, analyticsPermission.delete))

const labels = computed<FactLabels>(() => ({
  active: t('models.active'),
  inactive: t('models.inactive'),
  locked: t('models.locked'),
  version: (version) => t('models.version', { version }),
  awaiting: (count) => t('models.awaitingFact', { count }),
}))

const modelIds = computed(() => (model.value ? [model.value.id] : []))
const awaiting = usePendingCounts(modelIds)
const approvalState = useApprovalState()

function modelOf(area: string): FormModel | undefined {
  return models.value.find((candidate) => candidate.area === area)
}

const approvalTargets = computed<ApprovalTarget[]>(() => {
  const targets: ApprovalTarget[] = []
  const kind = modelForm.value?.approval?.kind

  if (model.value && kind !== undefined) targets.push({ kind, id: model.value.id })

  for (const level of levels.value) {
    const levelKind = modelOf(level.area)?.approval?.kind

    if (level.record && levelKind !== undefined)
      targets.push({ kind: levelKind, id: level.record.id })
  }

  return targets
})

const statuses = useRecordApprovals(approvalTargets)

const lookup = useQuery({
  queryKey: computed(() => queryKeys.formRecords(modelArea, undefined)),
  queryFn: () => formModelApi.records(modelForm.value!, undefined),
  enabled: computed(() => wanted.value !== undefined && modelForm.value !== undefined),
  retry: false,
})

function opened(values: RecordValues): OpenedRecord {
  const id = values.id as number
  const guid = typeof values.guid === 'string' ? values.guid : undefined

  return {
    id,
    guid,
    name: modelForm.value ? recordName(modelForm.value, values, id) : `#${id}`,
    values,
  }
}

function publish(): void {
  emit(
    'path',
    encodeAddress({
      modelId: model.value?.id,
      operate: operate.value,
      steps: levels.value.flatMap((level) =>
        level.record ? [{ area: level.area, id: level.record.id }] : [],
      ),
    }),
  )
}

function approvalFacts(form: FormModel | undefined, id: number): StoryFact[] {
  const kind = form?.approval?.kind
  const status = kind === undefined ? undefined : statuses.value.get(approvalKey(kind, id))

  if (!status) return []

  const state = stateOf(status)

  return [{ text: approvalState.label(state), tone: approvalState.tone(state) }]
}

function operateTitle(which: Operate): string {
  return which === 'sample'
    ? t('models.sample.title')
    : which === 'integrity'
      ? t('models.integrity.title')
      : t('models.analytics.title')
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
  if (creating.value) {
    return [
      { id: 'models', title: t('models.title'), name: t('models.newTitle'), facts: [] },
      { id: 'create', title: t('models.newTitle'), facts: [] },
    ]
  }

  const current = model.value

  if (!current) return [{ id: 'models', title: t('models.title'), facts: [] }]

  const list: Beat[] = [
    {
      id: 'model',
      title: t('models.title'),
      name: current.name,
      facts: [
        ...recordFacts(
          modelForm.value,
          current.values,
          labels.value,
          awaiting.value.get(current.id),
        ),
        ...approvalFacts(modelForm.value, current.id),
      ],
    },
  ]

  if (operate.value) {
    list.push({
      id: `operate-${operate.value}`,
      title: operateTitle(operate.value),
      name: operate.value === 'analytics' ? analytics.value?.instance.name : undefined,
      facts: [],
    })

    if (operate.value === 'analytics' && analytics.value) {
      list.push({
        id: 'operate-analytics#model',
        title: analytics.value.model.name ?? '',
        facts: [],
      })
    }

    return list
  }

  for (const level of levels.value) {
    const form = modelOf(level.area)

    if (level.record) {
      list.push({
        id: recordBeatId(level),
        title: level.label,
        name: level.record.name,
        facts: [
          ...recordFacts(form, level.record.values, labels.value),
          ...approvalFacts(form, level.record.id),
        ],
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
  const address = decodeAddress(props.path)

  wanted.value = address.modelId
  wantedOperate.value = address.operate
  pending.value = address.steps
})

function levelFor(child: FormChild, startNew: boolean, initialId?: number): Level {
  const parent = child.parentField === 'guid' ? model.value?.guid : model.value?.id

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

  model.value = opened(values)

  if (wantedOperate.value) {
    operate.value = wantedOperate.value
    return
  }

  const next = pending.value[0]
  const child = next && modelForm.value?.children.find((candidate) => candidate.area === next.area)

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

function settleReopen(id: string): void {
  if (id === 'models') {
    creating.value = false
  } else if (id === 'model') {
    operate.value = undefined
    analytics.value = undefined
    levels.value = []
  } else if (id === 'operate-analytics') {
    analytics.value = undefined
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

function goOperate(which: Operate): void {
  guarded(() => {
    operate.value = which
    publish()
  })
}

function openAnalytics(trained: TrainedModel, instance: InstanceRow): void {
  analytics.value = { model: trained, instance }
}

function chooseModel(values: RecordValues): void {
  model.value = opened(values)
  operate.value = undefined
  analytics.value = undefined
  levels.value = []
  publish()
}

function backToModels(): void {
  guarded(() => {
    model.value = undefined
    operate.value = undefined
    analytics.value = undefined
    levels.value = []
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

function fromRecord(index: number, child: FormChild, startNew: boolean, initialId?: number): void {
  guarded(() => descend(index, child, startNew, initialId))
}

function levelOpened(index: number, record: OpenedRecord): void {
  const current = levels.value[index]

  if (!current) return

  const same = current.record?.id === record.id

  levels.value = [
    ...levels.value.slice(0, index),
    { ...current, record, startNew: false },
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

function modelOpened(record: OpenedRecord): void {
  if (model.value?.id === record.id) model.value = record
}

function cancelCreate(): void {
  guarded(() => {
    creating.value = false
  })
}

function createdOpened(record: OpenedRecord): void {
  model.value = record
  creating.value = false
  operate.value = undefined
  levels.value = []
  publish()
}
</script>

<template>
  <StorySpine :beats="beats" @reopen="reopen">
    <template #default="{ beat }">
      <ModelsTable
        v-if="beat.id === 'models'"
        key="models"
        @open="chooseModel"
        @create="creating = true"
      />

      <NewRecordBeat
        v-else-if="beat.id === 'create'"
        key="create"
        :area="modelArea"
        :back-label="t('models.title')"
        @opened="createdOpened"
        @back="cancelCreate"
        @dirty="(value) => (dirty = value)"
      />

      <RecordBeat
        v-else-if="beat.id === 'model' && model"
        key="model"
        :record="model"
        :model="modelForm"
        :models="models"
        :model-id="model.id"
        root
        :back-label="t('models.title')"
        @child="fromHub"
        @back="backToModels"
        @opened="modelOpened"
        @dirty="(value) => (dirty = value)"
      >
        <template #extra>
          <ModelOperateCards
            :model-id="model.id"
            :can-sample="canSample"
            :can-integrity="canIntegrity"
            :can-analytics="canAnalytics"
            @operate="goOperate"
          />
        </template>
      </RecordBeat>

      <ModelSample
        v-else-if="beat.id === 'operate-sample' && model"
        key="sample"
        :model-guid="model.guid ?? null"
        :allowed="canSample"
      />

      <IntegrityCanvas
        v-else-if="beat.id === 'operate-integrity' && model"
        key="integrity"
        :model-id="model.id"
        :can-synchronise="canSynchronise"
      />

      <AskJoobyAnalyticsInstances
        v-else-if="beat.id === 'operate-analytics' && model"
        key="analytics"
        :model-id="model.id"
        :can-set-active="canSetActive"
        :can-delete="canDeleteAnalytics"
        :can-delete-allocated="canDeleteAnalytics && canSetActive"
        @open="openAnalytics"
      />

      <AskJoobyAnalyticsModelCanvas
        v-else-if="beat.id === 'operate-analytics#model' && analytics"
        key="analytics-model"
        :model-id="analytics.model.id"
        :instance-name="analytics.instance.name"
      />

      <CrudPanel
        v-else-if="levelIndex(beat.id) >= 0 && !isRecordBeat(beat.id) && !isNewBeat(beat.id)"
        :key="`list-${beat.id}`"
        :area="levels[levelIndex(beat.id)]!.area"
        :parent-id="levels[levelIndex(beat.id)]!.parentValue"
        :list-route="levels[levelIndex(beat.id)]!.listRoute"
        :initial-id="levels[levelIndex(beat.id)]!.initialId"
        external-new
        @opened="(record) => levelOpened(levelIndex(beat.id), record)"
        @closed="levelClosed(levelIndex(beat.id))"
        @create="startLevelNew(levelIndex(beat.id))"
        @dirty="(value) => (dirty = value)"
      />

      <NewRecordBeat
        v-else-if="levelIndex(beat.id) >= 0 && isNewBeat(beat.id)"
        :key="`new-${beat.id}`"
        :area="levels[levelIndex(beat.id)]!.area"
        :back-label="levels[levelIndex(beat.id)]!.label"
        :parent-id="levels[levelIndex(beat.id)]!.parentValue"
        :list-route="levels[levelIndex(beat.id)]!.listRoute"
        @opened="(record) => levelOpened(levelIndex(beat.id), record)"
        @back="cancelLevelNew(levelIndex(beat.id))"
        @dirty="(value) => (dirty = value)"
      />

      <RecordBeat
        v-else-if="levelIndex(beat.id) >= 0 && model && levels[levelIndex(beat.id)]!.record"
        :key="`record-${beat.id}`"
        :record="levels[levelIndex(beat.id)]!.record!"
        :model="modelOf(levels[levelIndex(beat.id)]!.area)"
        :models="models"
        :model-id="model.id"
        :root="false"
        :back-label="levels[levelIndex(beat.id)]!.label"
        :parent-id="levels[levelIndex(beat.id)]!.parentValue"
        :list-route="levels[levelIndex(beat.id)]!.listRoute"
        @child="(child, startNew) => fromRecord(levelIndex(beat.id), child, startNew)"
        @back="backFromLevel(levelIndex(beat.id))"
        @opened="(record) => levelOpened(levelIndex(beat.id), record)"
        @dirty="(value) => (dirty = value)"
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
