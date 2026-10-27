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
import type { BuilderCoderValue, BuilderGroup } from '../ruleBuilder/types'
import { parseReference, reference } from '../dataset/model'
import type { ExhaustiveSource } from '../filterBuilder/api'
import type { ExhaustiveFilterValue } from '../components/ExhaustiveFilterBuilder.vue'
import type { DatasourceValue } from '../datasource/types'
import type { FormEditor, FormModel, RecordValues } from './types'

export function claimedNames(model: FormModel): Set<string> {
  return new Set(model.editors.flatMap((editor) => Object.values(editor.roles)))
}

export function editorOf(model: FormModel, fieldName: string): FormEditor | undefined {
  return model.editors.find((editor) => Object.values(editor.roles).includes(fieldName))
}

export function firstClaimed(editor: FormEditor, model: FormModel): string | undefined {
  const names = new Set(Object.values(editor.roles))

  return model.fields.find((field) => names.has(field.name))?.name
}

function text(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

export function ruleEditorInitial(
  editor: FormEditor,
  values: RecordValues,
): BuilderCoderValue | undefined {
  const { roles } = editor
  const coder = text(values[roles.coder!])
  const builder = roles.builder ? text(values[roles.builder]) : ''
  const json = roles.builderJson ? text(values[roles.builderJson]) : ''

  if (coder === '' && builder === '' && json === '') return undefined

  const type = roles.scriptType ? Number(values[roles.scriptType]) : undefined

  return {
    ruleTextBuilder: builder,
    ruleJsonBuilder: json,
    ruleTextCoder: coder,
    ruleType: type === 1 || type === 2 ? type : undefined,
  }
}

export function ruleEditorChanges(
  editor: FormEditor,
  value: BuilderCoderValue,
): [string, unknown][] {
  const { roles } = editor
  const changes: [string, unknown][] = [[roles.coder!, value.ruleTextCoder]]

  if (roles.builder) changes.push([roles.builder, value.ruleTextBuilder ?? ''])

  if (roles.builderJson) {
    const json = value.ruleJsonBuilder as string | BuilderGroup | undefined

    changes.push([
      roles.builderJson,
      typeof json === 'string' ? json : json ? JSON.stringify(json) : '',
    ])
  }

  if (roles.scriptType && value.ruleType !== undefined)
    changes.push([roles.scriptType, value.ruleType])

  return changes
}

export function caseFilterInitial(
  editor: FormEditor,
  values: RecordValues,
): { selectJson?: string; filterJson?: string } | undefined {
  const select = text(values[editor.roles.select!])
  const filter = text(values[editor.roles.filter!])

  return select === '' && filter === '' ? undefined : { selectJson: select, filterJson: filter }
}

export function caseFilterChanges(
  editor: FormEditor,
  value: { selectJson: string; filterJson: string },
): [string, unknown][] {
  return [
    [editor.roles.select!, value.selectJson],
    [editor.roles.filter!, value.filterJson],
  ]
}

const dataSourceUploaded = 2
const editableStatuses = [0, 20]

function whole(value: unknown): number {
  const number = Number(value)

  return Number.isInteger(number) && number > 0 ? number : 0
}

export function datasetReference(editor: FormEditor, values: RecordValues): string {
  const csv = whole(values[editor.roles.csv!])
  const json = whole(values[editor.roles.json!])

  if (csv > 0) return reference({ kind: 'Csv', id: csv })

  return json > 0 ? reference({ kind: 'Json', id: json }) : ''
}

export function datasetChanges(editor: FormEditor, picked: string): [string, unknown][] {
  const parsed = parseReference(picked)
  const changes: [string, unknown][] = [
    [editor.roles.csv!, parsed?.kind === 'Csv' ? parsed.id : null],
    [editor.roles.json!, parsed?.kind === 'Json' ? parsed.id : null],
  ]

  if (parsed && editor.reads.source) changes.push([editor.reads.source, dataSourceUploaded])

  return changes
}

export function datasetShown(editor: FormEditor, values: RecordValues): boolean {
  return Number(values[editor.reads.source!]) === dataSourceUploaded
}

export function datasetLocked(editor: FormEditor, values: RecordValues): boolean {
  const raw = values[editor.reads.status!]

  return raw !== undefined && raw !== null && !editableStatuses.includes(Number(raw))
}

export function exhaustiveSource(
  editor: FormEditor,
  values: RecordValues,
  modelId: number,
): ExhaustiveSource {
  const picked =
    Number(values[editor.reads.source!]) === dataSourceUploaded
      ? parseReference(
          datasetReference(
            { ...editor, roles: { csv: editor.reads.csv!, json: editor.reads.json! } },
            values,
          ),
        )
      : null

  return picked
    ? { kind: 'dataset', datasetKind: picked.kind, id: picked.id }
    : { kind: 'model', modelId }
}

export function exhaustiveFilterInitial(
  editor: FormEditor,
  values: RecordValues,
): { filterJson?: string } | undefined {
  const json = text(values[editor.roles.filterJson!])

  return json === '' ? undefined : { filterJson: json }
}

export function exhaustiveFilterChanges(
  editor: FormEditor,
  value: ExhaustiveFilterValue,
): [string, unknown][] {
  return [
    [editor.roles.filterJson!, value.filterJson],
    [editor.roles.filterSql!, value.filterSql],
    [editor.roles.filterTokens!, value.filterTokens],
  ]
}

export function datasourceInitial(editor: FormEditor, values: RecordValues): DatasourceValue {
  const { roles } = editor
  const mapping = values[roles.mapping!]

  return {
    command: text(values[roles.command!]),
    displayKindId: whole(values[roles.displayKind!]),
    includeGrid: values[roles.grid!] === true,
    mapping: Array.isArray(mapping) ? (mapping as DatasourceValue['mapping']) : [],
    templateText: text(values[roles.template!]),
    valid: false,
  }
}

export function datasourceChanges(
  editor: FormEditor,
  value: DatasourceValue,
  values: RecordValues,
): [string, unknown][] {
  const { roles } = editor
  const current = datasourceInitial(editor, values)
  const changes: [string, unknown][] = []

  if (value.command !== current.command) changes.push([roles.command!, value.command])

  if (value.displayKindId !== current.displayKindId)
    changes.push([roles.displayKind!, value.displayKindId])

  if (value.includeGrid !== current.includeGrid) changes.push([roles.grid!, value.includeGrid])

  if (value.templateText !== current.templateText)
    changes.push([roles.template!, value.templateText])

  if (JSON.stringify(value.mapping) !== JSON.stringify(current.mapping))
    changes.push([roles.mapping!, value.mapping])

  return changes
}
