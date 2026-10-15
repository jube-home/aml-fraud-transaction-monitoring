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
using System.Text.RegularExpressions;
using Jube.AskJooby.SqlQuery.Models;

namespace Jube.Service.DynamicSql
{
    public static partial class SqlPlanParser
    {
        public static SqlPlanSummary Parse(IReadOnlyList<string> lines)
        {
            var startupCost = 0d;
            var totalCost = 0d;
            var estimatedRows = 0d;

            if (lines.Count > 0)
            {
                var root = CostPattern().Match(lines[0]);

                if (root.Success)
                {
                    startupCost = ParseNumber(root.Groups[1].Value);
                    totalCost = ParseNumber(root.Groups[2].Value);
                    estimatedRows = ParseNumber(root.Groups[3].Value);
                }
            }

            var sequential = new List<string>();
            var indexed = new List<string>();

            foreach (var line in lines)
            {
                foreach (Match match in SequentialScanPattern().Matches(line))
                {
                    Add(sequential, match.Groups[1].Value);
                }

                foreach (Match match in IndexScanPattern().Matches(line))
                {
                    Add(indexed, match.Groups[1].Value);
                }

                foreach (Match match in BitmapIndexScanPattern().Matches(line))
                {
                    Add(indexed, match.Groups[1].Value);
                }
            }

            return new SqlPlanSummary(totalCost, startupCost, estimatedRows, sequential, indexed, lines);
        }

        private static void Add(ICollection<string> names, string name)
        {
            var trimmed = name.Trim().Trim('"');

            if (trimmed.Length > 0 && !names.Contains(trimmed, StringComparer.Ordinal))
            {
                names.Add(trimmed);
            }
        }

        private static double ParseNumber(string value)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0d;
        }

        [GeneratedRegex(@"\(cost=([0-9.]+)\.\.([0-9.]+)\s+rows=([0-9.]+)")]
        private static partial Regex CostPattern();

        [GeneratedRegex(@"Seq Scan on (""[^""]+""|[A-Za-z0-9_]+)")]
        private static partial Regex SequentialScanPattern();

        [GeneratedRegex(@"Index(?: Only)? Scan(?: Backward)? using [^ ]+ on (""[^""]+""|[A-Za-z0-9_]+)")]
        private static partial Regex IndexScanPattern();

        [GeneratedRegex(@"Bitmap Index Scan on (""[^""]+""|[A-Za-z0-9_]+)")]
        private static partial Regex BitmapIndexScanPattern();
    }
}