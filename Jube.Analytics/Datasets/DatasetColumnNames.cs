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

using System.Globalization;
using System.Text;
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public static class DatasetColumnNames
    {
        public static List<DatasetColumnDefinition> Build(IReadOnlyList<string> fields, DatasetLimits limits,
            DatasetParseReport report)
        {
            var columns = new List<DatasetColumnDefinition>();

            if (fields.Count > limits.MaxColumns)
            {
                report.Errors.Add(new DatasetParseError("TooManyColumns",
                    $"The file has {fields.Count} columns; the permitted maximum is {limits.MaxColumns}."));

                return columns;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < fields.Count; i++)
            {
                if (!TryValidateHeader(fields[i], i, limits, seen, report, out var header))
                {
                    continue;
                }

                columns.Add(NewColumn(header, i, taken));
            }

            return columns;
        }

        public static DatasetColumnDefinition NewColumn(string header, int index, ISet<string> taken)
        {
            return new DatasetColumnDefinition
            {
                Index = index,
                Header = header,
                Name = SuggestName(header, index, taken),
                ProcessingTypeId = DatasetProcessingTypes.Abstraction
            };
        }

        public static bool TryValidateHeader(string raw, int i, DatasetLimits limits, ISet<string> seen,
            DatasetParseReport report, out string header)
        {
            header = raw.Trim();

            if (header.Length == 0)
            {
                report.Errors.Add(new DatasetParseError("EmptyHeader", $"Column {i + 1} has an empty header.", null,
                    i));

                return false;
            }

            if (header.Length > limits.MaxHeaderLength)
            {
                report.Errors.Add(new DatasetParseError("HeaderTooLong",
                    $"The header of column {i + 1} exceeds {limits.MaxHeaderLength} characters.", null, i));

                return false;
            }

            if (header.Any(c => c is '<' or '>' || char.IsControl(c)))
            {
                report.Errors.Add(new DatasetParseError("HeaderMarkup",
                    $"The header of column {i + 1} contains markup or control characters.", null, i));

                return false;
            }

            if (!seen.Add(header))
            {
                report.Errors.Add(new DatasetParseError("DuplicateHeader",
                    $"The header '{header}' appears more than once (column {i + 1}); header names are compared without regard to case.",
                    null, i));

                return false;
            }

            return true;
        }

        public static string SuggestName(string header, int index, ISet<string> taken)
        {
            var builder = new StringBuilder();

            foreach (var c in header.Trim())
            {
                if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9')
                {
                    builder.Append(c);
                }
                else if (builder.Length > 0 && builder[^1] != '_')
                {
                    builder.Append('_');
                }
            }

            var name = builder.ToString().Trim('_');

            if (name.Length == 0)
            {
                name = "Column" + index.ToString(CultureInfo.InvariantCulture);
            }
            else if (!(name[0] is >= 'a' and <= 'z' or >= 'A' and <= 'Z'))
            {
                name = "V" + name;
            }

            if (name.Length > DatasetMappingRules.MaxNameLength)
            {
                name = name[..DatasetMappingRules.MaxNameLength];
            }

            var candidate = name;
            var suffix = 2;

            while (!taken.Add(candidate))
            {
                var tail = "_" + suffix.ToString(CultureInfo.InvariantCulture);

                candidate = (name.Length + tail.Length > DatasetMappingRules.MaxNameLength
                    ? name[..(DatasetMappingRules.MaxNameLength - tail.Length)]
                    : name) + tail;

                suffix++;
            }

            return candidate;
        }
    }
}