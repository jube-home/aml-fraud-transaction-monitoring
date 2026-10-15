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
using System.Text.RegularExpressions;
using Jube.Analytics.Categorical;
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public sealed class DatasetColumnAccumulator
    {
        public const int MaxRawDistinctTracked = 500;
        private const double MaxExactInteger = 9007199254740992d;

        private static readonly Regex thousandsPattern =
            new(@"^[+-]?\d{1,3}(,\d{3})+(\.\d+)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex decimalCommaPattern =
            new(@"^[+-]?\d+,\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex numericStringPattern =
            new(@"^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public readonly CategoricalCounter Categorical = new();
        private readonly Dictionary<string, long> rawDistinct = new(StringComparer.Ordinal);
        public long Booleans;
        public bool DecimalCommaHint;
        public long FormulaLike;
        public long LargeIntegers;
        public double Max = double.NegativeInfinity;
        public double Min = double.PositiveInfinity;
        public long Missing;
        public long Numbers;
        public long NumericStrings;
        public long Ones;
        public bool RawDistinctTruncated;
        public double Sum;
        public long Texts;
        public bool ThousandsHint;
        public long Zeros;
        public IReadOnlyDictionary<string, long> RawDistinct => rawDistinct;

        public DatasetCell AddMissing()
        {
            Missing++;
            Categorical.Add(null);

            return new DatasetCell(DatasetCellKind.Missing, 0, null);
        }

        public DatasetCell AddNumber(double value)
        {
            if (value == 0)
            {
                value = 0d;
            }

            Numbers++;
            Sum += value;
            Min = Math.Min(Min, value);
            Max = Math.Max(Max, value);

            if (value == 0)
            {
                Zeros++;
            }
            else if (Math.Abs(value - 1) < 0.0001)
            {
                Ones++;
            }

            Categorical.Add(value.ToString(CultureInfo.InvariantCulture));

            return new DatasetCell(DatasetCellKind.Number, value, null);
        }

        public DatasetCell AddBoolean(bool value)
        {
            Booleans++;

            return AddNumber(value ? 1d : 0d);
        }

        public DatasetCell AddLargeInteger(double value)
        {
            LargeIntegers++;

            return AddNumber(value);
        }

        public static bool IsBeyondExactInteger(double value)
        {
            return Math.Abs(value) > MaxExactInteger;
        }

        public DatasetCell AddText(string text, DatasetLimits limits, bool countNumericStrings = false)
        {
            Texts++;

            if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@')
            {
                FormulaLike++;
            }

            if (thousandsPattern.IsMatch(text))
            {
                ThousandsHint = true;
            }
            else if (decimalCommaPattern.IsMatch(text))
            {
                DecimalCommaHint = true;
            }

            if (countNumericStrings && numericStringPattern.IsMatch(text))
            {
                NumericStrings++;
            }

            var stored = text.Length > limits.MaxStoredTextLength ? text[..limits.MaxStoredTextLength] : text;
            Categorical.Add(stored);
            TrackRaw(stored);

            return new DatasetCell(DatasetCellKind.Text, 0, stored);
        }

        private void TrackRaw(string text)
        {
            if (rawDistinct.TryGetValue(text, out var count))
            {
                rawDistinct[text] = count + 1;

                return;
            }

            if (rawDistinct.Count >= MaxRawDistinctTracked)
            {
                RawDistinctTruncated = true;

                return;
            }

            rawDistinct[text] = 1;
        }
    }
}