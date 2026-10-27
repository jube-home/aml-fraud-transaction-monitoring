/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */
import { computed, ref, type Ref } from 'vue'
import { useMutation, useQueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { refusalReasons } from '../approval/model'
import { formModelApi } from './api'
import { dependentsOf } from './lookups'
import type { ServiceChange } from '../events/appEvents'
import type { FormModel, RecordValues } from './types'
import { changedFields, comparable, isLocked, newRecord, payloadOf, problemsOf } from './values'

export type FormMode = 'idle' | 'new' | 'edit'

export interface ExternalChange {
  kind: 'updated' | 'deleted' | 'conflict'
  actor: string
  at: string | null
  from: number | null
  to: number | null
  pending: boolean
  count: number
}

function camel(name: string): string {
  const last = name.split('.').pop() ?? name

  return `${last.charAt(0).toLowerCase()}${last.slice(1)}`
}

export interface RecordFormOptions {
  model: Ref<FormModel | undefined>
  parentId: Ref<number | string | undefined>
  refused: (action: string) => string
  numberProblem: (label: string) => string
  onSaved: (record: RecordValues, created: boolean) => void
  onDeleted: (id: number) => void
  onFailure: (error: unknown) => void
}

export function useRecordForm(options: RecordFormOptions) {
  const queryClient = useQueryClient()

  const mode = ref<FormMode>('idle')
  const values = ref<RecordValues>({})
  const stored = ref<RecordValues>()
  const baseline = ref('')
  const reasons = ref<string[]>([])
  const fieldErrors = ref<Record<string, string[]>>({})
  const external = ref<ExternalChange>()
  const changed = ref<string[]>([])
  const incomplete = ref<string[]>([])

  const locked = computed(
    () =>
      options.model.value !== undefined &&
      mode.value === 'edit' &&
      isLocked(options.model.value, values.value),
  )
  const dirty = computed(
    () =>
      options.model.value !== undefined &&
      mode.value !== 'idle' &&
      comparable(options.model.value, values.value) !== baseline.value,
  )
  const id = computed(() => {
    const key = options.model.value?.keys.id

    return key && mode.value === 'edit' ? (values.value[key] as number) : undefined
  })

  function load(record: RecordValues, next: FormMode): void {
    const model = options.model.value!

    values.value = { ...record }
    stored.value = next === 'edit' ? { ...record } : undefined
    baseline.value = comparable(model, values.value)
    reasons.value = []
    fieldErrors.value = {}
    external.value = undefined
    changed.value = []
    incomplete.value = []
    mode.value = next
  }

  function startNew(): void {
    const model = options.model.value!
    const record = newRecord(model)
    const parent = model.keys.parent

    if (parent && options.parentId.value !== undefined) record[parent] = options.parentId.value

    load(record, 'new')
  }

  async function open(recordId: number): Promise<void> {
    load(await formModelApi.record(options.model.value!, recordId), 'edit')
  }

  function close(): void {
    external.value = undefined
    changed.value = []
    incomplete.value = []
    mode.value = 'idle'
    values.value = {}
    stored.value = undefined
    reasons.value = []
    fieldErrors.value = {}
  }

  function reportEditor(editor: string, valid: boolean): void {
    const without = incomplete.value.filter((name) => name !== editor)

    incomplete.value = valid ? without : [...without, editor]
  }

  function setValue(name: string, value: unknown): void {
    const next = { ...values.value, [name]: value }
    const model = options.model.value

    if (model) for (const dependent of dependentsOf(model, name)) next[dependent] = null

    values.value = next
  }

  async function refresh(): Promise<string[]> {
    const model = options.model.value!
    const before = stored.value ?? values.value
    const record = await formModelApi.record(model, id.value!)
    const names = changedFields(model, before, record)

    load(record, 'edit')
    changed.value = names

    return names
  }

  function versionNow(): number | null {
    const version = values.value.version

    return typeof version === 'number' ? version : null
  }

  async function receive(change: ServiceChange): Promise<void> {
    if (mode.value !== 'edit' || id.value === undefined || change.entityId !== id.value) return

    const from = versionNow()

    if (change.operation === 'deleted') {
      external.value = {
        kind: 'deleted',
        actor: change.actor,
        at: change.at,
        from,
        to: null,
        pending: false,
        count: 0,
      }

      return
    }

    if (change.operation !== 'updated') return

    if (change.version !== null && change.version === from) return

    if (dirty.value) {
      external.value = {
        kind: 'updated',
        actor: change.actor,
        at: change.at,
        from,
        to: change.version,
        pending: true,
        count: 0,
      }

      return
    }

    const names = await refresh()

    external.value = {
      kind: 'updated',
      actor: change.actor,
      at: change.at,
      from,
      to: change.version,
      pending: false,
      count: names.length,
    }
  }

  async function acceptExternal(): Promise<void> {
    const pending = external.value

    if (!pending) return

    const names = await refresh()

    external.value = { ...pending, kind: 'updated', pending: false, count: names.length }
  }

  function dismissExternal(): void {
    external.value = undefined
  }

  function handle(error: unknown): void {
    if (error instanceof ApiError && error.kind === 'conflict') {
      external.value = {
        kind: 'conflict',
        actor: '',
        at: null,
        from: versionNow(),
        to: null,
        pending: true,
        count: 0,
      }

      return
    }

    if (error instanceof ApiError && error.kind === 'validation') {
      const mapped: Record<string, string[]> = {}

      for (const [key, messages] of Object.entries(error.fieldErrors)) {
        mapped[camel(key)] = [...(mapped[camel(key)] ?? []), ...messages]
      }

      fieldErrors.value = mapped
      reasons.value =
        Object.keys(mapped).length > 0
          ? []
          : refusalReasons(error.body, `${options.refused('save')} (HTTP ${error.status})`)

      return
    }

    options.onFailure(error)
  }

  const save = useMutation({
    mutationFn: async () => {
      const model = options.model.value!
      const problems = problemsOf(model, values.value)

      if (problems.length > 0) {
        const labels = problems.map(
          (problem) => model.fields.find((field) => field.name === problem.field)!.label,
        )

        throw new LocalProblem(labels.map(options.numberProblem))
      }

      const body = payloadOf(model, values.value, stored.value, options.parentId.value)

      return mode.value === 'new'
        ? { saved: await formModelApi.create(model, body), created: true }
        : { saved: await formModelApi.update(model, body), created: false }
    },
    onSuccess: async ({ saved, created }) => {
      load(saved, 'edit')
      await queryClient.invalidateQueries({ queryKey: ['service', options.model.value!.area] })
      options.onSaved(saved, created)
    },
    onError: (error) => {
      if (error instanceof LocalProblem) {
        reasons.value = error.messages
        fieldErrors.value = {}

        return
      }

      handle(error)
    },
  })

  const remove = useMutation({
    mutationFn: async () => {
      const recordId = id.value!

      await formModelApi.remove(options.model.value!, recordId)

      return recordId
    },
    onSuccess: async (recordId) => {
      close()
      await queryClient.invalidateQueries({ queryKey: ['service', options.model.value!.area] })
      options.onDeleted(recordId)
    },
    onError: handle,
  })

  return {
    mode,
    values,
    reasons,
    fieldErrors,
    external,
    changed,
    receive,
    acceptExternal,
    dismissExternal,
    locked,
    dirty,
    incomplete,
    reportEditor,
    id,
    saving: computed(() => save.isPending.value || remove.isPending.value),
    startNew,
    open,
    close,
    setValue,
    save: () => save.mutate(),
    remove: () => remove.mutate(),
  }
}

class LocalProblem extends Error {
  readonly messages: string[]

  constructor(messages: string[]) {
    super(messages.join(' '))
    this.messages = messages
  }
}
