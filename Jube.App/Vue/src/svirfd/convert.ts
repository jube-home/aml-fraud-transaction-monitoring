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

import type {
  IpRecord,
  JournalSvirfd,
  JournalSvirfdDto,
  SvirfdLookup,
  SvirfdLookupDto,
} from './types'

export function parseRecord(json: string | null | undefined): IpRecord | null {
  if (!json) return null

  try {
    return JSON.parse(json) as IpRecord
  } catch {
    return null
  }
}

export function lookupFrom(dto: SvirfdLookupDto): SvirfdLookup {
  const {recordJson, ...rest} = dto
  const record = parseRecord(recordJson)

  if (!record) throw new Error('The Svirfd record could not be read.')

  return {...rest, record}
}

export function journalFrom(dto: JournalSvirfdDto): JournalSvirfd {
  return {
    field: dto.field,
    ip: dto.ip,
    stored: parseRecord(dto.storedJson),
    storedAt: dto.storedAt,
    fresh: dto.fresh ? lookupFrom(dto.fresh) : null,
    drift: dto.drift ?? [],
  }
}
