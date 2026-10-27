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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { GridPagerLabels } from '../components/dataGridTypes'

export function usePagerLabels() {
  const { t } = useI18n()

  return computed<GridPagerLabels>(() => ({
    navigation: t('grid.pager.navigation'),
    first: t('grid.pager.first'),
    previous: t('grid.pager.previous'),
    next: t('grid.pager.next'),
    last: t('grid.pager.last'),
    pageSize: t('grid.pager.pageSize'),
    summary: (from, to, total) => t('grid.pager.summary', { from, to, total }),
    page: (page, pages) => t('grid.pager.page', { page, pages }),
  }))
}
