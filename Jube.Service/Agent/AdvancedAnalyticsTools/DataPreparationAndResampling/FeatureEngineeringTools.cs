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

using System.ComponentModel;
using System.Globalization;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    public enum PairOperation
    {
        Product = 0,
        Ratio = 1,
        Sum = 2,
        Difference = 3,
        AbsoluteDifference = 4,
        Minimum = 5,
        Maximum = 6
    }

    public enum ElapsedUnit
    {
        Seconds = 0,
        Minutes = 1,
        Hours = 2,
        Days = 3
    }

    public enum EpochUnit
    {
        Seconds = 0,
        Milliseconds = 1
    }

    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class FeatureEngineeringTools
    {
        private static readonly string[] datePartNames =
        {
            "year", "quarter", "month", "day", "dayOfWeek", "dayOfYear", "isoWeek", "hour", "minute", "second",
            "secondsOfDay", "isWeekend", "isMonthStart", "isMonthEnd"
        };

        [AdvancedAnalyticsTool("GeneratePolynomialFeatures")]
        [Description(
            "Creates polynomial and interaction terms from numeric columns: every product of up to 'degree' columns, such as a, b, a^2, a*b, b^2 for degree 2. " +
            "Use it to let a linear model capture curvature and pairwise effects. Set interactionOnly to drop the pure powers (a^2) and keep only products of different columns, and includeBias to prepend a column of ones. " +
            "Returns the new table with a name per column. A product involving a missing cell is NaN. The width grows quickly with columns and degree, so a limit is enforced.")]
        public FeatureTableResult GeneratePolynomialFeatures(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Highest total power of any term, 1 to 5. Default 2.")]
            int degree = 2,
            [Description(
                "When true, keep only products of distinct columns (no powers of a single column). Default false.")]
            bool interactionOnly = false,
            [Description("When true, add a constant 1 column named bias first. Default false.")]
            bool includeBias = false,
            [Description("Refuse to build more than this many output columns. Default 500.")]
            int maximumColumns = 500)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (degree < 1 || degree > 5)
            {
                throw new ArgumentException("degree must be between 1 and 5.", nameof(degree));
            }

            var terms = new List<int[]>();

            for (var d = 1; d <= degree; d++)
            {
                Combinations(columns, d, interactionOnly, 0, new List<int>(), terms);
            }

            if (terms.Count + (includeBias ? 1 : 0) > maximumColumns)
            {
                throw new ArgumentException(
                    $"The request would create {terms.Count + (includeBias ? 1 : 0)} columns, above the limit of {maximumColumns}. Lower the degree, use interactionOnly or select fewer columns.",
                    nameof(maximumColumns));
            }

            var outputNames = new List<string>();
            var offset = includeBias ? 1 : 0;

            if (includeBias)
            {
                outputNames.Add("bias");
            }

            outputNames.AddRange(terms.Select(t => string.Join("*", t.GroupBy(x => x).OrderBy(g => g.Key)
                .Select(g => g.Count() == 1 ? names[g.Key] : names[g.Key] + "^" + g.Count()))));

            var table = DataPreparationGuard.Build(data.Length, terms.Count + offset, (i, c) =>
            {
                if (c < offset)
                {
                    return 1;
                }

                double product = 1;

                foreach (var column in terms[c - offset])
                {
                    product *= DataPreparationGuard.IsMissing(data[i][column]) ? double.NaN : data[i][column];
                }

                return product;
            });

            return new FeatureTableResult(outputNames.ToArray(), table);
        }

        [AdvancedAnalyticsTool("GenerateColumnPairFeatures")]
        [Description(
            "Creates one new column for each requested pair of existing columns by combining them with product, ratio, sum, difference, absolute difference, minimum or maximum. " +
            "Use it for domain features such as amount / balance (ratio), credit - debit (difference) or amount * velocity (product). Give the two column name lists in parallel: firstColumns[i] and secondColumns[i] make output column i, named like 'first/second'. " +
            "A ratio with a zero denominator, or any operation with a missing cell, yields NaN; the count of such cells per output column is returned so you can see how often that happens.")]
        public PairFeatureResult GenerateColumnPairFeatures(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "The column names of the table, one per column in column order. Required so pairs can be given by name.")]
            string[] columnNames,
            [Description("Name of the first column of each pair, one entry per new feature.")]
            string[] firstColumns,
            [Description("Name of the second column of each pair, same length as firstColumns.")]
            string[] secondColumns,
            [Description(
                "Product, Ratio (first / second), Sum, Difference (first - second), AbsoluteDifference, Minimum or Maximum. Default Ratio.")]
            PairOperation operation = PairOperation.Ratio)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (firstColumns == null || secondColumns == null || firstColumns.Length == 0 ||
                firstColumns.Length != secondColumns.Length)
            {
                throw new ArgumentException(
                    "Give at least one pair, with firstColumns and secondColumns the same length.",
                    nameof(firstColumns));
            }

            var first = firstColumns.Select(n => DataPreparationGuard.ColumnIndex(names, n, nameof(firstColumns)))
                .ToArray();
            var second = secondColumns.Select(n => DataPreparationGuard.ColumnIndex(names, n, nameof(secondColumns)))
                .ToArray();

            var symbol = operation switch
            {
                PairOperation.Product => "*",
                PairOperation.Ratio => "/",
                PairOperation.Sum => "+",
                PairOperation.Difference => "-",
                PairOperation.AbsoluteDifference => "|-|",
                PairOperation.Minimum => "min",
                _ => "max"
            };

            var undefined = new int[first.Length];

            var table = DataPreparationGuard.Build(data.Length, first.Length, (i, c) =>
            {
                var a = data[i][first[c]];
                var b = data[i][second[c]];
                double value;

                if (DataPreparationGuard.IsMissing(a) || DataPreparationGuard.IsMissing(b))
                {
                    value = double.NaN;
                }
                else
                {
                    value = operation switch
                    {
                        PairOperation.Product => a * b,
                        PairOperation.Ratio => b == 0 ? double.NaN : a / b,
                        PairOperation.Sum => a + b,
                        PairOperation.Difference => a - b,
                        PairOperation.AbsoluteDifference => Math.Abs(a - b),
                        PairOperation.Minimum => Math.Min(a, b),
                        _ => Math.Max(a, b)
                    };
                }

                if (double.IsNaN(value))
                {
                    undefined[c]++;
                }

                return value;
            });

            return new PairFeatureResult(
                firstColumns.Select((f, c) => f + symbol + secondColumns[c]).ToArray(), table, undefined);
        }

        [AdvancedAnalyticsTool("GenerateLagFeatures")]
        [Description(
            "Builds lag features for rows that are already in time order: for each lag k, the value k rows earlier, and optionally the change from it, plus trailing window mean, standard deviation, minimum and maximum of the previous rows (the current row is excluded so there is no leakage). " +
            "Give group keys (for example an account id) to lag within each group only, taking rows in the order they appear. The first k rows of a group have no lag k and get NaN. " +
            "Returns the table with a name per column such as lag_1, diff_1, rollMean_3.")]
        public FeatureTableResult GenerateLagFeatures(
            [Description("The values in chronological order, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("The lags to create, each at least 1, for example [1, 2, 7].")]
            int[] lags,
            [Description(
                "Optional group key per row (same length as values) so lags never cross groups. Leave empty for a single series.")]
            string[]? groupKeys = null,
            [Description("When true, also add diff_k = value - lag_k for each lag. Default true.")]
            bool includeDifferences = true,
            [Description(
                "Trailing window size in rows for mean, standard deviation, min and max of previous values; 0 for none. Default 0.")]
            int rollingWindow = 0)
        {
            DataPreparationGuard.Vector(values, nameof(values));

            if (lags == null || lags.Length == 0 || lags.Any(l => l < 1))
            {
                throw new ArgumentException("Give at least one lag and make every lag 1 or more.", nameof(lags));
            }

            if (rollingWindow < 0)
            {
                throw new ArgumentException("rollingWindow must be 0 or more.", nameof(rollingWindow));
            }

            if (groupKeys is { Length: > 0 })
            {
                DataPreparationGuard.SameLength(values.Length, groupKeys.Length, nameof(groupKeys), nameof(values));
            }

            var groups = groupKeys is { Length: > 0 } ? groupKeys : new string[values.Length];
            var positions = new int[values.Length];
            var members = new Dictionary<string, List<int>>(StringComparer.Ordinal);

            for (var i = 0; i < values.Length; i++)
            {
                var key = groups[i];

                if (!members.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    members[key] = list;
                }

                positions[i] = list.Count;
                list.Add(i);
            }

            var names = new List<string>();
            names.AddRange(lags.Select(l => "lag_" + l));

            if (includeDifferences)
            {
                names.AddRange(lags.Select(l => "diff_" + l));
            }

            if (rollingWindow > 0)
            {
                names.AddRange(
                    new[] { "rollMean_", "rollStd_", "rollMin_", "rollMax_" }.Select(n => n + rollingWindow));
            }

            var table = new double[values.Length][];

            for (var i = 0; i < values.Length; i++)
            {
                var list = members[groups[i]];
                var p = positions[i];
                var row = new List<double>();

                foreach (var lag in lags)
                {
                    row.Add(p >= lag ? values[list[p - lag]] : double.NaN);
                }

                if (includeDifferences)
                {
                    foreach (var lag in lags)
                    {
                        row.Add(p >= lag ? values[i] - values[list[p - lag]] : double.NaN);
                    }
                }

                if (rollingWindow > 0)
                {
                    var window = Enumerable.Range(Math.Max(0, p - rollingWindow), p - Math.Max(0, p - rollingWindow))
                        .Select(q => values[list[q]]).Where(v => !DataPreparationGuard.IsMissing(v)).ToArray();

                    if (window.Length == 0)
                    {
                        row.AddRange(new[] { double.NaN, double.NaN, double.NaN, double.NaN });
                    }
                    else
                    {
                        var mean = window.Average();
                        row.Add(mean);

                        row.Add(window.Length > 1
                            ? Math.Sqrt(window.Sum(v => (v - mean) * (v - mean)) / (window.Length - 1))
                            : double.NaN);

                        row.Add(window.Min());
                        row.Add(window.Max());
                    }
                }

                table[i] = row.ToArray();
            }

            return new FeatureTableResult(names.ToArray(), table);
        }

        [AdvancedAnalyticsTool("ExtractDateTimeParts")]
        [Description(
            "Splits ISO 8601 date-time strings (such as 2024-03-15T14:30:00Z or 2024-03-15 14:30) into numeric parts: year, quarter, month, day, dayOfWeek (Monday 1 to Sunday 7), dayOfYear, isoWeek, hour, minute, second, secondsOfDay, isWeekend, isMonthStart, isMonthEnd. " +
            "Strings without an offset are read as UTC. Use utcOffsetMinutes to shift to local time first (for example 60 for UTC+1) so hour-of-day features reflect the customer's clock. " +
            "Returns the parts as a table; a string that cannot be parsed gives a row of NaN and is counted in UnparsedRows. Ask for only the parts you need with the parts argument.")]
        public DatePartsResult ExtractDateTimeParts(
            [Description("ISO 8601 date-time strings, one per row. Empty or unparseable strings give NaN parts.")]
            string[] isoDateTimes,
            [Description("Minutes to add to UTC before extracting parts, between -840 and 840. Default 0.")]
            int utcOffsetMinutes = 0,
            [Description("Names of the parts to return from the list in the description. Leave empty for all parts.")]
            string[]? parts = null)
        {
            if (isoDateTimes == null || isoDateTimes.Length == 0)
            {
                throw new ArgumentException("Supply at least one date-time string.", nameof(isoDateTimes));
            }

            var moments = isoDateTimes.Select(s => TryParse(s, out var m) ? (DateTime?)m : null).ToArray();

            return BuildParts(moments, utcOffsetMinutes, parts);
        }

        [AdvancedAnalyticsTool("ExtractEpochParts")]
        [Description(
            "Splits Unix epoch timestamps (seconds or milliseconds since 1970-01-01 UTC) into numeric parts: year, quarter, month, day, dayOfWeek (Monday 1 to Sunday 7), dayOfYear, isoWeek, hour, minute, second, secondsOfDay, isWeekend, isMonthStart, isMonthEnd. " +
            "Use utcOffsetMinutes to shift to local time first. Returns the parts as a table; NaN, infinite or out-of-range timestamps give a row of NaN and are counted in UnparsedRows. Ask for only the parts you need with the parts argument.")]
        public DatePartsResult ExtractEpochParts(
            [Description("Unix timestamps, one per row. NaN or infinity marks a missing value.")]
            double[] epochs,
            [Description("Seconds or Milliseconds. Default Seconds.")]
            EpochUnit unit = EpochUnit.Seconds,
            [Description("Minutes to add to UTC before extracting parts, between -840 and 840. Default 0.")]
            int utcOffsetMinutes = 0,
            [Description("Names of the parts to return from the list in the description. Leave empty for all parts.")]
            string[]? parts = null)
        {
            DataPreparationGuard.Vector(epochs, nameof(epochs));

            var moments = epochs.Select(e =>
            {
                if (DataPreparationGuard.IsMissing(e))
                {
                    return (DateTime?)null;
                }

                var seconds = unit == EpochUnit.Seconds ? e : e / 1000;

                if (seconds < -62135596800 || seconds > 253402300799)
                {
                    return null;
                }

                return DateTime.UnixEpoch.AddSeconds(seconds);
            }).ToArray();

            return BuildParts(moments, utcOffsetMinutes, parts);
        }

        [AdvancedAnalyticsTool("ComputeElapsedTime")]
        [Description(
            "Computes the elapsed time between two lists of ISO 8601 date-time strings, row by row, as (to - from) in seconds, minutes, hours or days. " +
            "Use it for account age, time since last transaction, or time between events. Strings without an offset are read as UTC. A row where either string cannot be parsed gives NaN. Negative values mean 'to' is earlier than 'from'.")]
        public double[] ComputeElapsedTime(
            [Description("Start ISO 8601 date-time strings, one per row.")]
            string[] fromIsoDateTimes,
            [Description("End ISO 8601 date-time strings, one per row, same length as the start list.")]
            string[] toIsoDateTimes,
            [Description("Seconds, Minutes, Hours or Days. Default Days.")]
            ElapsedUnit unit = ElapsedUnit.Days)
        {
            if (fromIsoDateTimes == null || toIsoDateTimes == null || fromIsoDateTimes.Length == 0)
            {
                throw new ArgumentException("Supply at least one pair of date-time strings.", nameof(fromIsoDateTimes));
            }

            DataPreparationGuard.SameLength(fromIsoDateTimes.Length, toIsoDateTimes.Length, nameof(toIsoDateTimes),
                nameof(fromIsoDateTimes));

            var divisor = unit switch
            {
                ElapsedUnit.Seconds => 1d,
                ElapsedUnit.Minutes => 60d,
                ElapsedUnit.Hours => 3600d,
                _ => 86400d
            };

            return fromIsoDateTimes.Select((f, i) =>
                TryParse(f, out var start) && TryParse(toIsoDateTimes[i], out var end)
                    ? (end - start).TotalSeconds / divisor
                    : double.NaN).ToArray();
        }

        [AdvancedAnalyticsTool("EncodeCyclicalFeature")]
        [Description(
            "Encodes a cyclic quantity (hour of day with period 24, day of week with period 7, month with period 12) as a sine and a cosine column so that the ends of the cycle are neighbours: hour 23 and hour 0 become close. " +
            "Returns two columns, sin and cos, computed as sin(2 pi x / period) and cos(2 pi x / period). A missing value gives NaN in both.")]
        public FeatureTableResult EncodeCyclicalFeature(
            [Description(
                "The cyclic values, one per row, such as hours 0 to 23. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("The length of one full cycle in the same units as the values, above 0. Default 24.")]
            double period = 24)
        {
            DataPreparationGuard.Vector(values, nameof(values));

            if (!(period > 0) || !double.IsFinite(period))
            {
                throw new ArgumentException("period must be a positive number, for example 24 for hours.",
                    nameof(period));
            }

            var table = values.Select(v => DataPreparationGuard.IsMissing(v)
                ? new[] { double.NaN, double.NaN }
                : new[] { Math.Sin(2 * Math.PI * v / period), Math.Cos(2 * Math.PI * v / period) }).ToArray();

            return new FeatureTableResult(new[] { "sin", "cos" }, table);
        }

        [AdvancedAnalyticsTool("SelectTableColumns")]
        [Description(
            "Returns a table containing only the named columns, in the order you list them. Use it after a selection tool to keep the chosen features, or to reorder columns. " +
            "An unknown name is rejected with the list of valid names.")]
        public FeatureTableResult SelectTableColumns(
            [Description("Table rows: one array of numbers per case, all the same length.")]
            double[][] data,
            [Description("The name of every column of the table, in column order.")]
            string[] columnNames,
            [Description("The names of the columns to keep, in the desired output order.")]
            string[] keep)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            return DataPreparationGuard.SelectColumns(data, names, keep, nameof(keep));
        }

        private static void Combinations(int columns, int size, bool distinct, int start, List<int> current,
            List<int[]> output)
        {
            if (current.Count == size)
            {
                output.Add(current.ToArray());

                return;
            }

            for (var c = start; c < columns; c++)
            {
                current.Add(c);
                Combinations(columns, size, distinct, distinct ? c + 1 : c, current, output);
                current.RemoveAt(current.Count - 1);
            }
        }

        private static bool TryParse(string? text, out DateTime moment)
        {
            moment = default;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                moment = parsed.UtcDateTime;

                return true;
            }

            return false;
        }

        private static DatePartsResult BuildParts(DateTime?[] moments, int utcOffsetMinutes, string[]? parts)
        {
            if (utcOffsetMinutes < -840 || utcOffsetMinutes > 840)
            {
                throw new ArgumentException("utcOffsetMinutes must be between -840 and 840.",
                    nameof(utcOffsetMinutes));
            }

            var selected = parts is { Length: > 0 } ? parts : datePartNames;

            foreach (var part in selected)
            {
                if (!datePartNames.Contains(part, StringComparer.Ordinal))
                {
                    throw new ArgumentException(
                        $"'{part}' is not a known part. Use any of: {string.Join(", ", datePartNames)}.",
                        nameof(parts));
                }
            }

            var table = moments.Select(m =>
            {
                if (!m.HasValue)
                {
                    return selected.Select(_ => double.NaN).ToArray();
                }

                var t = m.Value.AddMinutes(utcOffsetMinutes);
                var dayOfWeek = t.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)t.DayOfWeek;
                var daysInMonth = DateTime.DaysInMonth(t.Year, t.Month);
                var quarter = (t.Month - 1) / 3 + 1;

                return selected.Select(p => p switch
                {
                    "year" => t.Year,
                    "quarter" => quarter,
                    "month" => t.Month,
                    "day" => t.Day,
                    "dayOfWeek" => dayOfWeek,
                    "dayOfYear" => t.DayOfYear,
                    "isoWeek" => ISOWeek.GetWeekOfYear(t),
                    "hour" => t.Hour,
                    "minute" => t.Minute,
                    "second" => t.Second,
                    "secondsOfDay" => t.TimeOfDay.TotalSeconds,
                    "isWeekend" => dayOfWeek >= 6 ? 1 : 0,
                    "isMonthStart" => t.Day == 1 ? 1 : 0,
                    _ => t.Day == daysInMonth ? 1 : 0
                }).ToArray();
            }).ToArray();

            return new DatePartsResult(selected.ToArray(), table, moments.Count(m => !m.HasValue));
        }
    }

    public sealed record FeatureTableResult(string[] ColumnNames, double[][] Table);

    public sealed record PairFeatureResult(string[] ColumnNames, double[][] Table, int[] UndefinedCells);

    public sealed record DatePartsResult(string[] ColumnNames, double[][] Table, int UnparsedRows);
}