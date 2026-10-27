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

import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { buildWorkbook, saveBuffer } from '../export/xlsx'
import { casesApi } from './api'
import {
  exportCap,
  exportColumns,
  exportName,
  exportPage,
  flattenRow,
  saveText,
  toCsv,
} from './exportRows'
import { requestFor, type CasesPageState } from './pageState'
import type { CaseRow } from './types'

export function useCasesExport() {
  const { t } = useI18n()
  const busy = ref(false)
  const failed = ref(false)
  const capped = ref(false)

  async function fetchAll(state: CasesPageState): Promise<CaseRow[]> {
    const rows: CaseRow[] = []
    let total = Infinity

    while (rows.length < Math.min(total, exportCap)) {
      const page = await casesApi.page({ ...requestFor(state, rows.length), take: exportPage })

      total = page.total
      rows.push(...page.rows)

      if (page.rows.length === 0) break
    }

    capped.value = total > exportCap

    return rows.slice(0, exportCap)
  }

  async function run(kind: 'csv' | 'excel', state: CasesPageState): Promise<void> {
    if (busy.value) return

    busy.value = true
    failed.value = false

    try {
      const rows = (await fetchAll(state)).map((row) => flattenRow(row, t))
      const columns = exportColumns(t)
      const now = new Date()

      if (kind === 'csv') saveText(toCsv(columns, rows), exportName('csv', now))
      else
        saveBuffer(
          await buildWorkbook(columns, rows, t('cases.export.sheet')),
          exportName('xlsx', now),
        )
    } catch {
      failed.value = true
    } finally {
      busy.value = false
    }
  }

  return { busy, failed, capped, run }
}
