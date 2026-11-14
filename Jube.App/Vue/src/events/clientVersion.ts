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

import {ref} from 'vue'

const reloadKey = 'jube.build-reload'

export const maxReloadAttempts = 5

export const targetBuild = ref('')

export const newVersionAvailable = ref(false)

interface ReloadRecord {
  target: string
  attempts: number
}

export function clientBuildId(): string {
  return document.querySelector<HTMLMetaElement>('meta[name="jube-build-id"]')?.content ?? ''
}

export function isDifferentBuild(target: unknown, current: string = clientBuildId()): boolean {
  const server = Number(target)
  const client = Number(current)

  return Number.isFinite(server) && server > 0 && Number.isFinite(client) && client > 0 && server !== client
}

function readRecord(): ReloadRecord | undefined {
  try {
    const parsed = JSON.parse(sessionStorage.getItem(reloadKey) ?? 'null') as ReloadRecord | null

    return parsed && typeof parsed.target === 'string' && typeof parsed.attempts === 'number' ? parsed : undefined
  } catch {
    return undefined
  }
}

function writeRecord(record: ReloadRecord | undefined): void {
  try {
    if (record) sessionStorage.setItem(reloadKey, JSON.stringify(record))
    else sessionStorage.removeItem(reloadKey)
  } catch {
    return
  }
}

export function reloadAttempts(target: string): number {
  const record = readRecord()

  return record?.target === target ? record.attempts : 0
}

export function canReload(target: string): boolean {
  return reloadAttempts(target) < maxReloadAttempts
}

export function noteReload(target: string): void {
  writeRecord({target, attempts: reloadAttempts(target) + 1})
}

export function applyTarget(target: string, reload: () => void, schedule: typeof setTimeout = setTimeout): void {
  targetBuild.value = target

  if (!isDifferentBuild(target)) {
    newVersionAvailable.value = false
    writeRecord(undefined)

    return
  }

  newVersionAvailable.value = true

  const attempts = reloadAttempts(target)

  if (attempts === 0 || attempts >= maxReloadAttempts) return

  schedule(() => {
    noteReload(target)
    reload()
  }, 1500 * attempts)
}
