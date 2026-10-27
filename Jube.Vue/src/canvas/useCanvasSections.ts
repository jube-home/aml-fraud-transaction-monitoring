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
import { nextTick, ref, watch, type Ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { scrollToSection } from './scroll'

export function useCanvasSections<T extends string>(
  ids: readonly T[],
  initial: Record<T, boolean>,
  ready: Ref<boolean>,
) {
  const route = useRoute()
  const router = useRouter()
  const opened = ref({ ...initial }) as Ref<Record<T, boolean>>

  function isSection(value: unknown): value is T {
    return typeof value === 'string' && (ids as readonly string[]).includes(value)
  }

  async function reveal(id: T): Promise<void> {
    opened.value = { ...opened.value, [id]: true }
    await nextTick()
    scrollToSection(id)
  }

  function pick(id: string): void {
    if (!isSection(id)) return

    void router.replace({ query: { ...route.query, section: id } })
    void reveal(id)
  }

  let landed = false

  watch(
    ready,
    async (value) => {
      if (!value || landed) return

      landed = true

      const wanted = route.query.section

      if (isSection(wanted)) {
        await nextTick()
        await reveal(wanted)
      }
    },
    { immediate: true },
  )

  return { opened, pick }
}
