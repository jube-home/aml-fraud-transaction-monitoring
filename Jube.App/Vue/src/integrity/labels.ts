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

export type Translate = (key: string) => string
export type HasTranslation = (key: string) => boolean

export const categoryLabelKeys: Record<string, string> = {
  Dependencies: 'integrity.category.dependencies',
  Compilation: 'integrity.category.compilation',
  Configuration: 'integrity.category.configuration',
  Engine: 'integrity.category.engine',
}

export const entityKindLabelKeys: Record<string, string> = {
  RequestXPath: 'integrity.entityKind.requestXPath',
  InlineScriptProperty: 'integrity.entityKind.inlineScriptProperty',
  InlineFunction: 'integrity.entityKind.inlineFunction',
  GatewayRule: 'integrity.entityKind.gatewayRule',
  AbstractionRule: 'integrity.entityKind.abstractionRule',
  AbstractionCalculation: 'integrity.entityKind.abstractionCalculation',
  ActivationRule: 'integrity.entityKind.activationRule',
  TtlCounter: 'integrity.entityKind.ttlCounter',
  Sanction: 'integrity.entityKind.sanction',
  List: 'integrity.entityKind.list',
  Dictionary: 'integrity.entityKind.dictionary',
  HttpAdaptation: 'integrity.entityKind.httpAdaptation',
  GraphRecall: 'integrity.entityKind.graphRecall',
  Svirfd: 'integrity.entityKind.svirfd',
  ExhaustiveAdaptation: 'integrity.entityKind.exhaustiveAdaptation',
  ReprocessingRule: 'integrity.entityKind.reprocessingRule',
  InlineScript: 'integrity.entityKind.inlineScript',
  CaseWorkflow: 'integrity.entityKind.caseWorkflow',
  Model: 'integrity.entityKind.model',
  EngineInstance: 'integrity.entityKind.engineInstance',
  Missing: 'integrity.entityKind.missing',
}

export const approvalKindNumbers = [
  1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20, 21, 22, 23,
]

export function approvalKindLabelKey(kind: number): string {
  return `integrity.approvalKind.${kind}`
}

export const labelKeys: string[] = [
  ...Object.values(categoryLabelKeys),
  ...Object.values(entityKindLabelKeys),
  ...approvalKindNumbers.map(approvalKindLabelKey),
  'integrity.deps.entityLabel',
  'integrity.deps.missingName',
]

function labelled(
  t: Translate,
  te: HasTranslation,
  key: string | undefined,
  fallback: string,
): string {
  return key !== undefined && te(key) ? t(key) : fallback
}

export function categoryLabel(t: Translate, te: HasTranslation, category: string): string {
  return labelled(t, te, categoryLabelKeys[category], category)
}

export function categoriesLabel(t: Translate, te: HasTranslation, categories: string): string {
  return categories
    .split(', ')
    .filter((category) => category !== '')
    .map((category) => categoryLabel(t, te, category))
    .join(', ')
}

export function entityKindLabel(t: Translate, te: HasTranslation, kind: string): string {
  return labelled(t, te, entityKindLabelKeys[kind], kind)
}

export function approvalKindLabel(
  t: Translate,
  te: HasTranslation,
  kind: number,
  fallback: string,
): string {
  return labelled(t, te, approvalKindLabelKey(kind), fallback)
}
