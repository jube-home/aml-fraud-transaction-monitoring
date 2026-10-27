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

import { useI18n } from 'vue-i18n'

export function intlTag(locale: string): string {
  return locale === 'en' ? 'en-GB' : locale
}

export function useFormat() {
  const { locale } = useI18n()

  function currency(value: number, currencyCode: string): string {
    return new Intl.NumberFormat(intlTag(locale.value), {
      style: 'currency',
      currency: currencyCode,
      currencyDisplay: 'code',
    }).format(value)
  }

  function number(value: number): string {
    return new Intl.NumberFormat(intlTag(locale.value)).format(value)
  }

  function date(value: Date | string): string {
    return new Intl.DateTimeFormat(intlTag(locale.value), {
      dateStyle: 'medium',
      timeZone: 'UTC',
    }).format(typeof value === 'string' ? new Date(value) : value)
  }

  function percent(value: number): string {
    return new Intl.NumberFormat(intlTag(locale.value), {
      style: 'percent',
      maximumFractionDigits: 0,
    }).format(value)
  }

  function evidenceNumber(value: number): string {
    return new Intl.NumberFormat(intlTag(locale.value), { maximumFractionDigits: 4 }).format(value)
  }

  function fixed(value: number | null, digits = 2): string {
    if (value === null || !Number.isFinite(value)) return '–'

    return new Intl.NumberFormat(intlTag(locale.value), {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits,
    }).format(value)
  }

  function dateTime(value: Date | string): string {
    return new Intl.DateTimeFormat(intlTag(locale.value), {
      dateStyle: 'medium',
      timeStyle: 'short',
      hour12: false,
      timeZone: 'UTC',
    }).format(typeof value === 'string' ? new Date(value) : value)
  }

  return { currency, date, dateTime, evidenceNumber, fixed, number, percent }
}
