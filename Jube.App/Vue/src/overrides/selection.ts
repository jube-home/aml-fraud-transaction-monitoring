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

import {computed, ref, type Ref} from 'vue'
import type {OverrideActions} from './useOverrideActions'
import type {OverrideKind} from './types'

export interface PickedOverride {
  entityAnalysisModelGuid: string
  ruleName: string | null
  overrideKind: OverrideKind
}

export interface OverrideSelection {
  targets: Ref<PickedOverride[]>
  actions: OverrideActions
  hasModel: (guid: string) => boolean
  hasRule: (guid: string, name: string) => boolean
  kindOfRule: (guid: string, name: string) => OverrideKind
}

function same(target: PickedOverride, guid: string, name: string | null): boolean {
  return target.entityAnalysisModelGuid === guid && target.ruleName === name
}

export function createOverrideSelection(): OverrideSelection {
  const targets = ref<PickedOverride[]>([])
  const problems = ref<string[]>([])

  function toggle(guid: string, name: string | null, active: boolean): Promise<boolean> {
    const rest = targets.value.filter((target) => !same(target, guid, name))

    targets.value = active
      ? [...rest, {entityAnalysisModelGuid: guid, ruleName: name, overrideKind: 0}]
      : rest

    return Promise.resolve(true)
  }

  return {
    targets,
    actions: {
      problems,
      updating: computed(() => false),
      reportTooEarly: () => undefined,
      setModel: (guid, active) => toggle(guid, null, active),
      setModelExpiry: () => Promise.resolve(true),
      setRule: (guid, name, active) => toggle(guid, name, active),
      setRuleKind: (guid, name, kind) => {
        targets.value = targets.value.map((target) =>
          same(target, guid, name) ? {...target, overrideKind: kind} : target,
        )

        return Promise.resolve(true)
      },
      setRuleExpiry: () => Promise.resolve(true),
    },
    hasModel: (guid) => targets.value.some((target) => same(target, guid, null)),
    hasRule: (guid, name) => targets.value.some((target) => same(target, guid, name)),
    kindOfRule: (guid, name) =>
      targets.value.find((target) => same(target, guid, name))?.overrideKind ?? 0,
  }
}
