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

import {decompressBlock} from 'lz4js'

export const wireCompressionHeader = 'X-Wire-Compression'
export const lz4Name = 'lz4'

const lz4BlockExtension = 99
const int32Marker = 0xd2
const maximumUncompressedBytes = 256 * 1024 * 1024

export function inflateLz4(body: ArrayBuffer): Uint8Array {
  const bytes = new Uint8Array(body)
  const view = new DataView(body)
  const marker = bytes[0]
  let cursor: number
  let length: number

  if (marker === 0xc7) {
    length = view.getUint8(1)
    cursor = 2
  } else if (marker === 0xc8) {
    length = view.getUint16(1)
    cursor = 3
  } else if (marker === 0xc9) {
    length = view.getUint32(1)
    cursor = 5
  } else {
    throw new Error('A compressed MessagePack body must be an extension.')
  }

  if (bytes[cursor] !== lz4BlockExtension)
    throw new Error('A compressed MessagePack body must be an LZ4 block.')

  cursor += 1

  if (cursor + length !== bytes.length || bytes[cursor] !== int32Marker)
    throw new Error('A compressed MessagePack body is malformed.')

  const uncompressed = view.getInt32(cursor + 1)

  if (uncompressed < 0 || uncompressed > maximumUncompressedBytes)
    throw new Error('A compressed MessagePack body declares an unreasonable size.')

  const output = new Uint8Array(uncompressed)
  const start = cursor + 5
  const written = decompressBlock(bytes, output, start, bytes.length - start, 0)

  if (written !== uncompressed)
    throw new Error('A compressed MessagePack body did not inflate to its declared size.')

  return output
}
