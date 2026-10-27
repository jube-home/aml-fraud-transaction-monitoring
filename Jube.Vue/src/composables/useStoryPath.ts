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
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

export function useStoryPath() {
  const route = useRoute()
  const router = useRouter()

  function queryPath(): string | undefined {
    return typeof route.query.path === 'string' ? route.query.path : undefined
  }

  const start = ref(queryPath())
  const generation = ref(0)
  let written = queryPath()

  function remember(text: string): void {
    written = text === '' ? undefined : text
    void router.replace({ query: { ...route.query, path: written } })
  }

  watch(
    () => queryPath(),
    (now) => {
      if (now === written) return

      written = now
      start.value = now
      generation.value += 1
    },
  )

  return { start, generation, remember }
}
