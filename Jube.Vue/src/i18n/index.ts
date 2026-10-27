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

import { createI18n, type PluralizationRule } from 'vue-i18n'
import { setApiLanguage } from '../api/language'
import en from '../locales/en.json'

export type MessageSchema = typeof en

export const supportedLocales = ['en', 'pl', 'ar'] as const

const rightToLeft = new Set<string>(['ar', 'he', 'fa', 'ur'])

const lazyLocales = import.meta.glob<{ default: MessageSchema }>('../locales/*.json')

const categoryOrder = ['zero', 'one', 'two', 'few', 'many', 'other']

export function createPluralRule(locale: string): PluralizationRule {
  const rules = new Intl.PluralRules(locale)
  const categories = categoryOrder.filter(
    (category) =>
      category !== 'zero' &&
      rules.resolvedOptions().pluralCategories.includes(category as Intl.LDMLPluralRule),
  )

  return (choice) => {
    if (choice === 0) return 0

    return categories.indexOf(rules.select(choice)) + 1
  }
}

export function createI18nInstance() {
  return createI18n<[MessageSchema], string, false>({
    legacy: false,
    locale: 'en',
    fallbackLocale: 'en',
    messages: { en },
    pluralRules: Object.fromEntries(
      supportedLocales.map((locale) => [locale, createPluralRule(locale)]),
    ),
  })
}

export function directionFor(locale: string): 'ltr' | 'rtl' {
  return rightToLeft.has(locale.split('-')[0]!) ? 'rtl' : 'ltr'
}

export interface LocaleTarget {
  locale: { value: string }
  availableLocales: string[]
  setLocaleMessage: (locale: string, messages: MessageSchema) => void
}

export async function loadLocale(target: LocaleTarget, locale: string): Promise<void> {
  const loader = lazyLocales[`../locales/${locale}.json`]

  if (loader && !target.availableLocales.includes(locale)) {
    const messages = await loader()
    target.setLocaleMessage(locale, messages.default)
  }

  target.locale.value = loader ? locale : 'en'

  setApiLanguage(target.locale.value)

  document.documentElement.lang = target.locale.value === 'en' ? 'en-GB' : target.locale.value
  document.documentElement.dir = directionFor(target.locale.value)
}
