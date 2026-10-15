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

using Jube.Analytics.Categorical;
using Jube.Analytics.Categorical.Models;
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public static class DatasetProfile
    {
        public static void Finish(List<DatasetColumnDefinition> columns, DatasetColumnAccumulator[] accumulators,
            DatasetParseReport report, CategoricalOptions? categoricalOptions = null)
        {
            var options = (categoricalOptions ?? new CategoricalOptions()).Sanitised();

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var a = accumulators[i];
                column.NumericCount = a.Numbers;
                column.TextCount = a.Texts;
                column.MissingCount = a.Missing;
                column.ZeroCount = a.Zeros;
                column.OneCount = a.Ones;
                column.FormulaLikeCount = a.FormulaLike;

                if (a.Numbers > 0)
                {
                    column.Min = a.Min;
                    column.Max = a.Max;
                    column.Mean = a.Sum / a.Numbers;
                }

                column.Kind = a.Numbers == 0 && a.Texts == 0 ? DatasetColumnKinds.Empty
                    : a.Texts == 0 ? DatasetColumnKinds.Numeric
                    : a.Numbers == 0 ? DatasetColumnKinds.Text
                    : DatasetColumnKinds.Mixed;

                column.IsBinary = column.Kind == DatasetColumnKinds.Numeric && a.Missing == 0 &&
                                  a.Zeros + a.Ones == a.Numbers;

                column.IsConstant = column.Kind == DatasetColumnKinds.Numeric && a.Numbers > 0 &&
                                    Math.Abs(a.Min - a.Max) < 0.0001;
                column.Include = column.Kind == DatasetColumnKinds.Numeric && !column.IsConstant;

                if (a.RawDistinct.Count > 0)
                {
                    column.DistinctValues = a.RawDistinct
                        .OrderByDescending(o => o.Value)
                        .ThenBy(o => o.Key, StringComparer.Ordinal)
                        .Select(s => new DatasetDistinctValue(s.Key, s.Value))
                        .ToList();
                }

                if (column.Kind == DatasetColumnKinds.Empty)
                {
                    report.Warnings.Add($"Column '{column.Header}' has no values and is excluded.");
                }

                if (column.IsConstant)
                {
                    report.Warnings.Add($"Column '{column.Header}' is constant and is excluded by default.");
                }

                if (a.Missing > 0 && column.Kind != DatasetColumnKinds.Empty)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has {a.Missing} missing values, which training reads as 0.");
                }

                if (a.Booleans > 0)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has {a.Booleans} boolean values, which are stored as 1 (true) and 0 (false).");
                }

                if (a.NumericStrings > 0)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has {a.NumericStrings} numbers supplied as strings, which are text and not coerced to numbers; supply JSON numbers.");
                }

                if (a.LargeIntegers > 0)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has {a.LargeIntegers} integers beyond 2^53, which lose precision as double values.");
                }

                if (a.FormulaLike > 0)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has {a.FormulaLike} text cells beginning with = + - or @; they are stored as data and never evaluated.");
                }

                if (a.ThousandsHint)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has values with thousands separators, which are not accepted as numbers; remove the separators.");
                }

                if (a.DecimalCommaHint)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has values with a decimal comma, which are not accepted as numbers; use a decimal point.");
                }

                if (a.RawDistinctTruncated)
                {
                    report.Warnings.Add(
                        $"Column '{column.Header}' has more than {DatasetColumnAccumulator.MaxRawDistinctTracked} distinct values; only the most frequent are listed.");
                }
            }

            FitCategoricalColumns(columns, accumulators, report, options);

            if (report.BlankLineCount > 0)
            {
                report.Warnings.Add($"{report.BlankLineCount} blank lines were skipped.");
            }

            report.Columns = columns;
        }

        private static void FitCategoricalColumns(List<DatasetColumnDefinition> columns,
            DatasetColumnAccumulator[] accumulators, DatasetParseReport report, CategoricalOptions options)
        {
            var candidateColumns = new List<DatasetColumnDefinition>();
            var candidates = new List<CategoricalFieldCandidate>();

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                if (column.Kind is not (DatasetColumnKinds.Text or DatasetColumnKinds.Mixed))
                {
                    continue;
                }

                candidateColumns.Add(column);

                candidates.Add(new CategoricalFieldCandidate(column.Header, accumulators[i].Categorical,
                    true, false));
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var plan = CategoricalPlanner.Fit(candidates, options);
            var encoderBySource = plan.Encoders.ToDictionary(d => d.SourceName, StringComparer.Ordinal);
            var reasonBySource = plan.Report.ToDictionary(d => d.SourceName, d => d.Reason, StringComparer.Ordinal);

            foreach (var column in candidateColumns)
            {
                if (encoderBySource.TryGetValue(column.Header, out var encoder))
                {
                    column.IsCategorical = true;
                    column.CategoricalEncoderJson = encoder.ToJson();

                    if (encoder.Levels.Count == 2)
                    {
                        column.Include = false;

                        report.Warnings.Add(
                            $"Column '{column.Header}' has exactly two values and is often the outcome a Class " +
                            "Definition filter targets, so it is excluded from training by default. It stays " +
                            "available for the filter; include it manually to also train on it.");
                    }
                    else
                    {
                        column.Include = true;

                        report.Warnings.Add(
                            $"Column '{column.Header}' is categorical: {encoder.Levels.Count} levels kept as " +
                            $"{encoder.Indicators.Count} indicators.");
                    }
                }
                else
                {
                    var reason =
                        reasonBySource.GetValueOrDefault(column.Header, "It could not be fitted as categorical.");

                    report.Warnings.Add(
                        $"Column '{column.Header}' is not numeric ({column.NumericCount} numeric, {column.TextCount} " +
                        $"text values) and is excluded: {reason}");
                }
            }
        }
    }
}