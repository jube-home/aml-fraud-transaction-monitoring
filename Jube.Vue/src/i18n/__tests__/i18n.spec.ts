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

import { beforeEach, describe, expect, it } from 'vitest'
import { createI18nInstance, createPluralRule, directionFor, loadLocale } from '..'

const noOriginalRule = () => 0

describe('plural rules', () => {
  it('uses the first form for zero and the CLDR categories after it in English', () => {
    const rule = createPluralRule('en')

    expect([0, 1, 2, 5].map((n) => rule(n, 3, noOriginalRule))).toEqual([0, 1, 2, 2])
  })

  it('distinguishes one, few, many and other in Polish', () => {
    const rule = createPluralRule('pl')

    expect([0, 1, 2, 5, 12, 22, 25].map((n) => rule(n, 5, noOriginalRule))).toEqual([
      0, 1, 2, 3, 3, 2, 3,
    ])
  })

  it('uses all six Arabic categories after the exact-zero form', () => {
    const rule = createPluralRule('ar')

    expect([0, 1, 2, 3, 10, 11, 99, 100].map((n) => rule(n, 6, noOriginalRule))).toEqual([
      0, 1, 2, 3, 3, 4, 4, 5,
    ])
  })
})

describe('locales', () => {
  beforeEach(() => {
    document.documentElement.lang = ''
    document.documentElement.dir = ''
  })

  it('pluralises English messages', () => {
    const { global } = createI18nInstance()

    expect(global.t('grid.rows', 0)).toBe('No rows')
    expect(global.t('grid.rows', 1)).toBe('1 row')
    expect(global.t('grid.rows', 4)).toBe('4 rows')
  })

  it('lazy-loads Polish, pluralises it and sets the document language', async () => {
    const { global } = createI18nInstance()

    expect(global.availableLocales).toEqual(['en'])

    await loadLocale(global, 'pl')

    expect(global.availableLocales).toContain('pl')
    expect(global.t('grid.rows', 2)).toBe('2 wiersze')
    expect(global.t('grid.rows', 5)).toBe('5 wierszy')
    expect(global.t('grid.rows', 22)).toBe('22 wiersze')
    expect(document.documentElement.lang).toBe('pl')
    expect(document.documentElement.dir).toBe('ltr')
  })

  it('loads Arabic, pluralises it and sets right-to-left on the document', async () => {
    const { global } = createI18nInstance()

    await loadLocale(global, 'ar')

    expect(global.t('grid.rows', 0)).toBe('لا صفوف')
    expect(global.t('grid.rows', 1)).toBe('صف واحد')
    expect(global.t('grid.rows', 2)).toBe('صفان')
    expect(global.t('grid.rows', 5)).toBe('5 صفوف')
    expect(global.t('grid.rows', 11)).toBe('11 صفًا')
    expect(global.t('grid.rows', 100)).toBe('100 صف')
    expect(document.documentElement.dir).toBe('rtl')
  })

  it('falls back to English for an unknown locale', async () => {
    const { global } = createI18nInstance()

    await loadLocale(global, 'xx')

    expect(global.locale.value).toBe('en')
    expect(document.documentElement.lang).toBe('en-GB')
  })

  it('knows which languages run right to left', () => {
    expect(directionFor('ar')).toBe('rtl')
    expect(directionFor('he-IL')).toBe('rtl')
    expect(directionFor('en')).toBe('ltr')
    expect(directionFor('pl')).toBe('ltr')
  })
})
