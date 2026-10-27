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
export type FieldType = 'string' | 'integer' | 'number' | 'boolean' | 'datetime' | 'guid' | 'json'

export type ConditionOp =
  'equals' | 'notEquals' | 'in' | 'notIn' | 'greaterThan' | 'isEmpty' | 'isNotEmpty'

export interface FormCondition {
  property: string
  op: ConditionOp
  values: unknown[]
  matchMode: 'all' | 'any'
}

export interface FormLookup {
  source: string
  textField: string
  valueField: string
  parentField: string | null
  upperField?: string | null
  allowEmpty: boolean
}

export interface FormOption {
  value: string | number
  label: string
}

export interface FormField {
  name: string
  label: string
  description: string
  type: FieldType
  nullable: boolean
  required: boolean
  minLength: number | null
  maxLength: number | null
  min: number | null
  max: number | null
  exclusiveMin: boolean
  exclusiveMax: boolean
  patterns: string[]
  options: FormOption[]
  group: string
  order: number
  widget: string
  span: number
  readOnly: boolean
  placeholder: string | null
  help: string | null
  editor: string | null
  lookup: FormLookup | null
  newDefault: unknown
  visibleWhen: FormCondition[]
  requiredWhen: FormCondition[]
  enabledWhen: FormCondition[]
}

export interface FormGroup {
  path: string
  label: string
  order: number
  collapsed: boolean
  widget: string
}

export interface FormListColumn {
  field: string
  title: string
  order: number
  sortable: boolean
  filterable: boolean
  hidden: boolean
}

export interface FormKeys {
  id: string
  parent: string | null
  naturalKey: string | null
}

export type FormOperation = 'list' | 'read' | 'create' | 'update' | 'delete'

export interface FormApproval {
  kind: number
  historyRoute: string
}

export interface FormRoles {
  endpoint: string
  parentKey: string
}

export interface FormValueField {
  field: string
  title: string
  type: 'string' | 'number'
}

export interface FormValues {
  area: string
  endpoint: string
  parentField: string
  parentKind: number
  fields: FormValueField[]
}

export interface FormEditor {
  kind:
    | 'ruleBuilder'
    | 'caseFilter'
    | 'exhaustiveDataset'
    | 'exhaustiveFilter'
    | 'datasource'
    | 'caseSql'
  parseType: number | null
  roles: Record<string, string>
  reads: Record<string, string>
}

export interface FormChild {
  area: string
  label: string
  hub: string
  order: number
  parentKey: string
  parentField: 'id' | 'guid'
  listRoute: string
}

export interface FormModel {
  area: string
  endpoint: string
  operations: FormOperation[]
  keys: FormKeys
  approval: FormApproval | null
  roles: FormRoles | null
  values: FormValues | null
  editors: FormEditor[]
  lockFields: string[]
  groups: FormGroup[]
  fields: FormField[]
  listColumns: FormListColumn[]
  children: FormChild[]
}

export type RecordValues = Record<string, unknown>
