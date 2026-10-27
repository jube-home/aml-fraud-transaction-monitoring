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
import { useRoute, useRouter } from 'vue-router'
import { mergeState, parseState, type CasesPageState } from './pageState'

export function useCasesState() {
  const route = useRoute()
  const router = useRouter()

  const state = computed(() => parseState(route.query))

  function go(next: CasesPageState, replace = false): void {
    const location = { query: mergeState(route.query, next) }

    if (replace) void router.replace(location)
    else void router.push(location)
  }

  return { state, go }
}
