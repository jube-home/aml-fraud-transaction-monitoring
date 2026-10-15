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

using System.Buffers.Binary;

namespace Jube.Analytics.Datasets
{
    public static class AdvancedAnalyticsPacking
    {
        public const int CurrentVersion = 1;

        public static byte[] Pack(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            var bytes = new byte[values.Length * sizeof(double)];

            for (var i = 0; i < values.Length; i++)
            {
                BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(i * sizeof(double), sizeof(double)),
                    BitConverter.DoubleToInt64Bits(values[i]));
            }

            return bytes;
        }

        public static double[] Unpack(byte[] bytes, int packingVersion)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            if (packingVersion != CurrentVersion)
            {
                throw new NotSupportedException($"Unsupported packing version {packingVersion}.");
            }

            if (bytes.Length % sizeof(double) != 0)
            {
                throw new ArgumentException("Packed length is not a multiple of eight bytes.", nameof(bytes));
            }

            var values = new double[bytes.Length / sizeof(double)];

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = BitConverter.Int64BitsToDouble(
                    BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(i * sizeof(double), sizeof(double))));
            }

            return values;
        }
    }
}