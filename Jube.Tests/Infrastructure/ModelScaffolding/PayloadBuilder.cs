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

// ReSharper disable once RedundantUsingDirective

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public sealed class PayloadBuilder
    {
        private readonly List<KeyValuePair<string, string>> fields;

        public PayloadBuilder(IEnumerable<(string Name, string Value)> template)
        {
            fields = [.. template.Select(t => new KeyValuePair<string, string>(t.Name, t.Value))];
        }

        public PayloadBuilder With(string name, string value)
        {
            var index = fields.FindIndex(f => f.Key == name);
            var field = new KeyValuePair<string, string>(name, value);
            if (index >= 0)
            {
                fields[index] = field;
            }
            else
            {
                fields.Add(field);
            }

            return this;
        }

        public PayloadBuilder Without(string name)
        {
            fields.RemoveAll(f => f.Key == name);
            return this;
        }

        public string? Get(string name)
        {
            return fields.Where(f => f.Key == name).Select(f => f.Value).FirstOrDefault();
        }

        private string ToJson()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var (key, value) in fields)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(ToJson());
        }

        public override string ToString()
        {
            return ToJson();
        }
    }
}