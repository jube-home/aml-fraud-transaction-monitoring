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

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public static class DatasetRowJson
    {
        public const string ValuesProperty = "values";

        public static string Key(int index)
        {
            return "c" + index.ToString(CultureInfo.InvariantCulture);
        }

        public static string ValueJsonPath(int index)
        {
            return "$." + ValuesProperty + "." + Key(index);
        }

        public static string Serialise(DatasetCell[] cells)
        {
            var buffer = new ArrayBufferWriter<byte>(cells.Length * 8 + 32);

            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteStartObject(ValuesProperty);

                for (var i = 0; i < cells.Length; i++)
                {
                    var key = Key(i);

                    switch (cells[i].Kind)
                    {
                        case DatasetCellKind.Number:
                            writer.WriteNumber(key, cells[i].Number);
                            break;
                        case DatasetCellKind.Text:
                            writer.WriteString(key, cells[i].Text);
                            break;
                        default:
                            writer.WriteNull(key);
                            break;
                    }
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
    }
}