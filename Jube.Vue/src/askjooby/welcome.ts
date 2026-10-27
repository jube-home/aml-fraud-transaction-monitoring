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

import { pickOfflineWelcome, type StartupStats } from './offlineWelcome'

const format = (value: number): string => Number(value).toLocaleString('en-US')

export function joobyWelcome(stats: StartupStats): string {
  return (
    "I'm Jooby -- this platform's fraud and AML assistant, not anyone standing in for me. " +
    `${format(stats.bites)} Bites, ${format(stats.trails)} Trails, ${format(stats.saltlicks)} Saltlicks, ` +
    `${format(stats.joobriks)} Joobriks on call, ${format(stats.yokes)} Yokes ready, and counting. ` +
    'What brings you in?'
  )
}

export function welcomeFor(stats: StartupStats | undefined, offline: boolean): string {
  if (offline) return pickOfflineWelcome(stats ?? null)

  return stats ? joobyWelcome(stats) : ''
}
