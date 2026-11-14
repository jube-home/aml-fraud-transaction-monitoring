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

using System.Collections.Generic;
using Jube.Dto.Wire;
using MessagePack;
using MessagePack.Formatters;

namespace Jube.App.Wire
{
    public sealed class ColumnarRowsFormatter<TValue> : IMessagePackFormatter<List<Dictionary<string, TValue>>>
    {
        private const int Parts = 5;
        private const int MaximumRows = 1_000_000;

        public void Serialize(ref MessagePackWriter writer, List<Dictionary<string, TValue>> value,
            MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();

                return;
            }

            var keys = new List<string>();
            var seen = new HashSet<string>();
            List<int> nullRows = null;

            for (var row = 0; row < value.Count; row++)
            {
                if (value[row] is null)
                {
                    (nullRows ??= []).Add(row);

                    continue;
                }

                foreach (var key in value[row].Keys)
                {
                    if (seen.Add(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            var formatter = options.Resolver.GetFormatterWithVerify<TValue>();
            Dictionary<int, List<int>> sparse = null;

            writer.WriteArrayHeader(Parts);
            writer.Write(value.Count);
            writer.WriteArrayHeader(keys.Count);

            foreach (var key in keys)
            {
                writer.Write(key);
            }

            writer.WriteArrayHeader(keys.Count);

            for (var column = 0; column < keys.Count; column++)
            {
                writer.WriteArrayHeader(value.Count);

                for (var row = 0; row < value.Count; row++)
                {
                    if (value[row] is not null && value[row].TryGetValue(keys[column], out var cell))
                    {
                        formatter.Serialize(ref writer, cell, options);

                        continue;
                    }

                    writer.WriteNil();

                    if (value[row] is not null)
                    {
                        ((sparse ??= new Dictionary<int, List<int>>()).TryGetValue(column, out var missing)
                            ? missing
                            : sparse[column] = []).Add(row);
                    }
                }
            }

            if (sparse is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(sparse.Count);

                foreach (var (column, rows) in sparse)
                {
                    writer.WriteArrayHeader(2);
                    writer.Write(column);
                    writer.WriteArrayHeader(rows.Count);

                    foreach (var row in rows)
                    {
                        writer.Write(row);
                    }
                }
            }

            if (nullRows is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(nullRows.Count);

                foreach (var row in nullRows)
                {
                    writer.Write(row);
                }
            }
        }

        public List<Dictionary<string, TValue>> Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);

            try
            {
                if (reader.ReadArrayHeader() != Parts)
                {
                    throw new MessagePackSerializationException("A columnar row set has five parts.");
                }

                var rowCount = reader.ReadInt32();

                if (rowCount is < 0 or > MaximumRows)
                {
                    throw new MessagePackSerializationException("The row count is outside the accepted range.");
                }

                var keyCount = reader.ReadArrayHeader();
                var keys = new string[keyCount];

                for (var column = 0; column < keyCount; column++)
                {
                    keys[column] = StrictUtf8String.Read(ref reader)
                                   ?? throw new MessagePackSerializationException("A column name cannot be nil.");
                }

                if (reader.ReadArrayHeader() != keyCount)
                {
                    throw new MessagePackSerializationException("The columns do not match the column names.");
                }

                var formatter = options.Resolver.GetFormatterWithVerify<TValue>();
                var columns = new TValue[keyCount][];

                for (var column = 0; column < keyCount; column++)
                {
                    if (reader.ReadArrayHeader() != rowCount)
                    {
                        throw new MessagePackSerializationException("A column does not hold one cell per row.");
                    }

                    columns[column] = new TValue[rowCount];

                    for (var row = 0; row < rowCount; row++)
                    {
                        columns[column][row] = formatter.Deserialize(ref reader, options);
                    }
                }

                var missing = ReadSparse(ref reader, keyCount);
                var nullRows = ReadRowSet(ref reader);
                var result = new List<Dictionary<string, TValue>>(rowCount);

                for (var row = 0; row < rowCount; row++)
                {
                    if (nullRows?.Contains(row) == true)
                    {
                        result.Add(null);

                        continue;
                    }

                    var item = new Dictionary<string, TValue>(keyCount);

                    for (var column = 0; column < keyCount; column++)
                    {
                        if (missing != null && missing.TryGetValue(column, out var absent) && absent.Contains(row))
                        {
                            continue;
                        }

                        item[keys[column]] = columns[column][row];
                    }

                    result.Add(item);
                }

                return result;
            }
            finally
            {
                reader.Depth--;
            }
        }

        private static Dictionary<int, HashSet<int>> ReadSparse(ref MessagePackReader reader, int keyCount)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var entries = reader.ReadArrayHeader();
            var result = new Dictionary<int, HashSet<int>>(entries);

            for (var i = 0; i < entries; i++)
            {
                if (reader.ReadArrayHeader() != 2)
                {
                    throw new MessagePackSerializationException("A sparse entry is a column and its missing rows.");
                }

                var column = reader.ReadInt32();

                if (column < 0 || column >= keyCount)
                {
                    throw new MessagePackSerializationException("A sparse entry names a column that does not exist.");
                }

                result[column] = ReadRowSet(ref reader) ?? [];
            }

            return result;
        }

        private static HashSet<int> ReadRowSet(ref MessagePackReader reader)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var count = reader.ReadArrayHeader();
            var rows = new HashSet<int>();

            for (var i = 0; i < count; i++)
            {
                rows.Add(reader.ReadInt32());
            }

            return rows;
        }
    }
}
