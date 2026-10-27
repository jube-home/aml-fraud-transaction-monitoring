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

import type { TagJob } from './types'

export function isActive(job: Pick<TagJob, 'state'>): boolean {
  return job.state === 'Queued' || job.state === 'Running'
}

export function percent(job: Pick<TagJob, 'processed' | 'total'>): number {
  if (job.total <= 0) return 0

  return Math.min(100, Math.floor((job.processed / job.total) * 100))
}

export function pollDelay(job: Pick<TagJob, 'state'> | undefined): number | false {
  return job && isActive(job) ? 1500 : false
}
