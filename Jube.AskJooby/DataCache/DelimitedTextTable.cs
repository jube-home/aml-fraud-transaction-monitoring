/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Text;
using Jube.AskJooby.DataCache.Models;

namespace Jube.AskJooby.DataCache
{
    public static class DelimitedTextTable
    {
        public static bool TryParse(string text, out CachedTable table)
        {
            table = new CachedTable([], []);

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var delimiter = DetectDelimiter(text);
            var records = ReadRecords(text, delimiter);

            if (records.Count < 2 || records[0].Count < 2)
            {
                return false;
            }

            var header = records[0];
            var names = new List<string>(header.Count);

            for (var i = 0; i < header.Count; i++)
            {
                var name = string.IsNullOrWhiteSpace(header[i]) ? $"Column{i + 1}" : header[i].Trim();
                var unique = name;
                var suffix = 2;

                while (names.Contains(unique, StringComparer.Ordinal))
                {
                    unique = $"{name}_{suffix++}";
                }

                names.Add(unique);
            }

            var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count - 1);

            foreach (var record in records.Skip(1))
            {
                var row = new Dictionary<string, object?>(names.Count);

                for (var i = 0; i < names.Count; i++)
                {
                    row[names[i]] = i < record.Count ? Coerce(record[i]) : null;
                }

                rows.Add(row);
            }

            table = new CachedTable(
                names.Select(name => new CachedColumn(name, InferType(rows, name))).ToList(), rows);

            return true;
        }

        private static char DetectDelimiter(string text)
        {
            var sample = text.Length > 4000 ? text[..4000] : text;
            var tabs = sample.Count(c => c == '\t');
            var commas = sample.Count(c => c == ',');
            var semicolons = sample.Count(c => c == ';');

            if (tabs > commas && tabs > semicolons)
            {
                return '\t';
            }

            return semicolons > commas ? ';' : ',';
        }

        private static List<List<string>> ReadRecords(string text, char delimiter)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            var quoted = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"' when field.Length == 0:
                        quoted = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        record.Add(field.ToString());
                        field.Clear();

                        if (record.Count > 1 || record[0].Length > 0)
                        {
                            records.Add(record);
                        }

                        record = [];
                        break;
                    default:
                        if (c == delimiter)
                        {
                            record.Add(field.ToString());
                            field.Clear();
                        }
                        else
                        {
                            field.Append(c);
                        }

                        break;
                }
            }

            if (field.Length > 0 || record.Count > 0)
            {
                record.Add(field.ToString());

                if (record.Count > 1 || record[0].Length > 0)
                {
                    records.Add(record);
                }
            }

            return records;
        }

        private static object? Coerce(string value)
        {
            var trimmed = value.Trim();

            if (trimmed.Length == 0)
            {
                return null;
            }

            if (double.TryParse(trimmed, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            if (bool.TryParse(trimmed, out var flag))
            {
                return flag;
            }

            return trimmed;
        }

        private static string InferType(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, string name)
        {
            foreach (var row in rows)
            {
                if (row.TryGetValue(name, out var value) && value is not null)
                {
                    return value.GetType().Name;
                }
            }

            return "unknown";
        }
    }
}