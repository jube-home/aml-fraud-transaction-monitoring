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

import {readFileSync} from 'node:fs'
import {resolve} from 'node:path'
import {describe, expect, it} from 'vitest'
import {
  approvalKindLabel,
  approvalKindLabelKey,
  approvalKindNumbers,
  categoriesLabel,
  categoryLabel,
  categoryLabelKeys,
  entityKindLabel,
  entityKindLabelKeys,
  labelKeys,
} from '../labels'
import {approvalKinds} from '../model'

const repository = resolve(process.cwd(), '../..')
const source = (path: string) => readFileSync(resolve(repository, path), 'utf8')

const enumNames = /enum ModelEntityKind\s*\{([^}]*)\}/
  .exec(source('Jube.Parser/Dependency/Models/ModelEntityKind.cs'))![1]!
  .split(',')
  .map((name) => name.trim())
  .filter((name) => name !== '')

const categories = [
  ...source('Jube.Engine/Integrity/ModelIntegrityRules.cs').matchAll(
    /private const string (\w+) = "(\w+)";/g,
  ),
].map((match) => match[2]!)

const present = () => true
const absent = () => false
const translate = (key: string) => `[${key}]`

describe('label keys', () => {
  it('has a key for every entity kind the server can send', () => {
    expect(enumNames.length).toBeGreaterThan(10)
    expect(enumNames.filter((name) => !(name in entityKindLabelKeys))).toEqual([])
  })

  it('has a key for the kinds the page itself invents', () => {
    expect(
      ['Model', 'EngineInstance', 'Missing'].filter((n) => !(n in entityKindLabelKeys)),
    ).toEqual([])
  })

  it('has a key for every finding category the rules use', () => {
    expect(categories).toEqual(['Dependencies', 'Compilation', 'Configuration', 'Engine'])
    expect(categories.filter((name) => !(name in categoryLabelKeys))).toEqual([])
  })

  it('has a key for every approvable kind', () => {
    expect(approvalKinds.map((entry) => entry.kind).sort((a, b) => a - b)).toEqual(
      [...approvalKindNumbers].sort((a, b) => a - b),
    )
  })

  it('never reuses a key and keeps every key under integrity', () => {
    expect(new Set(labelKeys).size).toBe(labelKeys.length)
    expect(labelKeys.filter((key) => !key.startsWith('integrity.'))).toEqual([])
  })
})

describe('labels', () => {
  it('translate through the key when it exists', () => {
    expect(categoryLabel(translate, present, 'Engine')).toBe('[integrity.category.engine]')
    expect(entityKindLabel(translate, present, 'TtlCounter')).toBe(
      '[integrity.entityKind.ttlCounter]',
    )
    expect(approvalKindLabel(translate, present, 6, 'Request XPath')).toBe(
      `[${approvalKindLabelKey(6)}]`,
    )
  })

  it('fall back to the code, never to a raw key, when the locale has no entry', () => {
    expect(categoryLabel(translate, absent, 'Engine')).toBe('Engine')
    expect(entityKindLabel(translate, absent, 'Unknown')).toBe('Unknown')
    expect(approvalKindLabel(translate, absent, 6, 'Request XPath')).toBe('Request XPath')
  })

  it('translate each of a joined list of categories', () => {
    expect(categoriesLabel(translate, present, 'Dependencies, Engine')).toBe(
      '[integrity.category.dependencies], [integrity.category.engine]',
    )
    expect(categoriesLabel(translate, present, '')).toBe('')
  })
})
