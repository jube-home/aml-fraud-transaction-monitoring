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

using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;

namespace Jube.AskJooby.Tools.DataCache
{
    public sealed class CachedDataTool(IToolResultCache cache)
    {
        private const int MaxPreviewRows = 20;
        private const int MaxQueryRows = 50;
        private const int CharactersPerToken = 4;

        private const string NotFoundMessage =
            "No cached table matches that pointer -- it may have expired, or never existed.";

        private static readonly string[] operatorsLongestFirst = ["!=", ">=", "<=", "contains", "=", ">", "<"];

        [Description(
            "Returns the column names, types, and null counts for a cached table, without any of its " +
            "rows, together with what the whole table would cost you to read: its size in characters " +
            "and roughly how many tokens that is. Always call this (or already know the schema from " +
            "when the table was created) before PreviewDataRows or QueryCachedData, so you know what " +
            "column names and roughly what value shapes are actually available to filter on, and so " +
            "you can judge whether reading more of it is sensible or absurd before you ask for it.")]
        public async Task<string> InspectDataSchema(
            [Description("The data_pointer returned when the table was cached.")]
            string dataPointer)
        {
            var table = await cache.GetAsync(dataPointer);

            if (table is null)
            {
                return NotFoundMessage;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"{table.Rows.Count} row(s), {table.Columns.Count} column(s).");

            foreach (var column in table.Columns)
            {
                var nullCount = table.Rows.Count(row => !row.TryGetValue(column.Name, out var value) || value is null);
                builder.AppendLine($"- {column.Name} ({column.Type}): {nullCount} null(s)");
            }

            builder.Append(DescribeCost(table));

            return builder.ToString();
        }

        [Description(
            "Returns a window of rows from a cached table, so you can see real formatting and value " +
            "shapes before deciding how to filter with QueryCachedData. Never returns the whole table: " +
            "at most 20 rows per call. Use offset to step through a table a window at a time when it " +
            "does not filter cleanly, but check the token cost InspectDataSchema reports first -- " +
            "walking a large table window by window will cost more than every row of it would, and a " +
            "tighter QueryCachedData filter or a narrower SQL query is nearly always the better move.")]
        public async Task<string> PreviewDataRows(
            [Description("The data_pointer returned when the table was cached.")]
            string dataPointer,
            [Description("How many rows to preview. Capped at 20.")]
            int limit = 5,
            [Description("How many rows to skip first. Defaults to 0, the start of the table.")]
            int offset = 0)
        {
            var table = await cache.GetAsync(dataPointer);

            if (table is null)
            {
                return NotFoundMessage;
            }

            var effectiveLimit = Math.Clamp(limit, 1, MaxPreviewRows);
            var effectiveOffset = Math.Max(offset, 0);

            if (effectiveOffset >= table.Rows.Count)
            {
                return $"Offset {effectiveOffset} is past the end of a {table.Rows.Count} row table.";
            }

            var rows = table.Rows.Skip(effectiveOffset).Take(effectiveLimit).ToList();

            var builder = new StringBuilder();

            builder.AppendLine(
                $"Showing rows {effectiveOffset + 1} to {effectiveOffset + rows.Count} "
                + $"of {table.Rows.Count}.");

            foreach (var row in rows)
            {
                builder.AppendLine(CachedRowText.Format(table.Columns, row));
            }

            return builder.ToString();
        }

        [Description(
            "Runs a simple filter against a cached table entirely on the backend, returning only the " +
            "rows that match -- never pulls the full table into your context. filterExpression is " +
            "\"column op value\" conditions joined with && , e.g. status = \"active\" && amount > 100. " +
            "Supported operators: = != > < >= <= and contains (substring match on a text column). " +
            "String values should be quoted; numbers should not. Column names must match " +
            "InspectDataSchema's output exactly.")]
        public async Task<string> QueryCachedData(
            [Description("The data_pointer returned when the table was cached.")]
            string dataPointer,
            [Description("The filter condition(s) -- see the tool description for the syntax.")]
            string filterExpression,
            [Description("Maximum matching rows to return. Capped at 50.")]
            int limit = 20)
        {
            var table = await cache.GetAsync(dataPointer);

            if (table is null)
            {
                return NotFoundMessage;
            }

            var clauses = ParseFilter(filterExpression, table.Columns);
            var matches = table.Rows.Where(row => Matches(row, clauses)).ToList();
            var effectiveLimit = Math.Clamp(limit, 1, MaxQueryRows);
            var returned = matches.Take(effectiveLimit).ToList();

            if (returned.Count == 0)
            {
                return "No rows matched that filter.";
            }

            var builder = new StringBuilder();
            builder.AppendLine($"{matches.Count} row(s) matched -- showing {returned.Count}.");

            foreach (var row in returned)
            {
                builder.AppendLine(CachedRowText.Format(table.Columns, row));
            }

            return builder.ToString();
        }

        private static string DescribeCost(CachedTable table)
        {
            int characters;

            try
            {
                characters = JsonSerializer.Serialize(table.Rows).Length;
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }

            var tokens = characters / CharactersPerToken;

            return $"Reading every row would be about {characters.ToString(CultureInfo.InvariantCulture)} "
                   + $"characters, roughly {tokens.ToString(CultureInfo.InvariantCulture)} tokens. "
                   + "Filter with QueryCachedData, or aggregate in SQL, rather than reading a table whose "
                   + "cost is out of proportion to the question being answered.";
        }

        private static List<Clause> ParseFilter(string filterExpression, IReadOnlyList<CachedColumn> columns)
        {
            if (string.IsNullOrWhiteSpace(filterExpression))
            {
                throw new ArgumentException("filterExpression must contain at least one condition.");
            }

            var clauses = new List<Clause>();

            var parts = filterExpression.Split("&&",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var firstQuote = part.IndexOf('"');
                var searchRegion = firstQuote < 0 ? part : part[..firstQuote];

                var matched = false;

                foreach (var op in operatorsLongestFirst)
                {
                    var needle = op == "contains" ? " contains " : op;
                    var index = searchRegion.IndexOf(needle, StringComparison.OrdinalIgnoreCase);

                    if (index < 0)
                    {
                        continue;
                    }

                    var column = part[..index].Trim();
                    var value = part[(index + needle.Length)..].Trim().Trim('"');

                    var actualColumn = columns.FirstOrDefault(c =>
                        string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase));

                    if (actualColumn is null)
                    {
                        throw new ArgumentException($"Unknown column \"{column}\". Known columns: " +
                                                    string.Join(", ", columns.Select(c => c.Name)) + ".");
                    }

                    clauses.Add(new Clause(actualColumn.Name, op, value));
                    matched = true;
                    break;
                }

                if (!matched)
                {
                    throw new ArgumentException(
                        $"Could not parse condition \"{part}\". Use \"column op value\" (op is =, !=, >, " +
                        "<, >=, <=, or contains), joined with && for multiple conditions.");
                }
            }

            return clauses;
        }

        private static bool Matches(IReadOnlyDictionary<string, object?> row, IReadOnlyList<Clause> clauses)
        {
            return clauses.All(clause =>
            {
                row.TryGetValue(clause.Column, out var actual);

                return EvaluateClause(actual, clause);
            });
        }

        private static bool EvaluateClause(object? actual, Clause clause)
        {
            if (clause.Op == "contains")
            {
                return actual is string text && text.Contains(clause.Value, StringComparison.OrdinalIgnoreCase);
            }

            if (actual is double or float or long or int or short or decimal &&
                double.TryParse(clause.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var expected))
            {
                var actualNumber = Convert.ToDouble(actual, CultureInfo.InvariantCulture);

                return clause.Op switch
                {
                    "=" => actualNumber.Equals(expected),
                    "!=" => !actualNumber.Equals(expected),
                    ">" => actualNumber > expected,
                    "<" => actualNumber < expected,
                    ">=" => actualNumber >= expected,
                    "<=" => actualNumber <= expected,
                    _ => false
                };
            }

            var actualText = actual?.ToString() ?? string.Empty;

            return clause.Op switch
            {
                "=" => string.Equals(actualText, clause.Value, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(actualText, clause.Value, StringComparison.OrdinalIgnoreCase),
                ">" => string.Compare(actualText, clause.Value, StringComparison.OrdinalIgnoreCase) > 0,
                "<" => string.Compare(actualText, clause.Value, StringComparison.OrdinalIgnoreCase) < 0,
                ">=" => string.Compare(actualText, clause.Value, StringComparison.OrdinalIgnoreCase) >= 0,
                "<=" => string.Compare(actualText, clause.Value, StringComparison.OrdinalIgnoreCase) <= 0,
                _ => false
            };
        }

        private sealed record Clause(string Column, string Op, string Value);
    }
}