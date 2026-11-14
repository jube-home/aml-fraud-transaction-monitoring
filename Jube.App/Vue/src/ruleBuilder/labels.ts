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

export interface Translate {
  t: (key: string) => string
  te: (key: string) => boolean
}

export const groupKeys: Readonly<Record<string, string>> = {
  Basic: 'basic',
  Lists: 'lists',
  'Compare to another field': 'compareToField',
  'Text: comparison': 'textComparison',
  'Text: contains and patterns': 'textContains',
  'Text: model lists': 'textLists',
  'Text: shape and length': 'textShape',
  'Text: validation': 'textValidation',
  'Text: email': 'textEmail',
  'Text: network': 'textNetwork',
  'Text: fuzzy similarity': 'textFuzzy',
  'Number: comparison and value': 'numberComparison',
  'Number: thresholds and limits': 'numberThresholds',
  'Number: change and growth': 'numberChange',
  'Number: ratios and shares': 'numberRatios',
  'Number: statistics': 'numberStatistics',
  'Number: geography': 'numberGeography',
  'Date: comparison and windows': 'dateComparison',
  'Date: calendar': 'dateCalendar',
  'Date: time of day and business hours (UTC)': 'dateTimeOfDay',
  'Date: time zones': 'dateZones',
  'Date: relative to when the rule runs': 'dateRelative',
  Boolean: 'boolean',
}

export const argumentKeys: Readonly<Record<string, string>> = {
  value: 'value',
  values: 'values',
  from: 'from',
  to: 'to',
  field: 'field',
  'CIDR range': 'cidrRange',
  'JSON path': 'jsonPath',
}

function translated(translate: Translate, key: string, fallback: string): string {
  return translate.te(key) ? translate.t(key) : fallback
}

export function operatorLabel(translate: Translate, type: string, fallback: string): string {
  return translated(translate, `ruleBuilder.operators.${type}`, fallback)
}

export function groupLabel(translate: Translate, group: string): string {
  const key = groupKeys[group]

  return key ? translated(translate, `ruleBuilder.groups.${key}`, group) : group
}

export function argumentLabel(translate: Translate, name: string): string {
  const key = argumentKeys[name]

  return key ? translated(translate, `ruleBuilder.arguments.${key}`, name) : name
}
