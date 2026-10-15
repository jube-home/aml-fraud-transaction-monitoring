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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    public enum ProportionIntervalMethod
    {
        Wilson = 0,
        ClopperPearson = 1,
        AgrestiCoull = 2,
        Wald = 3,
        Jeffreys = 4
    }

    internal static class CategoricalGuards
    {
        public static void Level(double level, string parameterName)
        {
            if (double.IsNaN(level) || level <= 0d || level >= 1d)
            {
                throw new ArgumentException(
                    "The confidence level must be strictly between 0 and 1, for example 0.95 for a 95 percent interval.",
                    parameterName);
            }
        }

        public static void Required(object? value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentException($"{parameterName} is required and cannot be null.", parameterName);
            }
        }

        public static void SameLength(int first, int second, string firstName, string secondName)
        {
            if (first != second)
            {
                throw new ArgumentException(
                    $"{firstName} and {secondName} must have the same number of entries but have {first} and {second}. Supply one entry per row in the same order.",
                    firstName);
            }
        }

        public static void BinaryLabels(int[] labels, string parameterName)
        {
            Required(labels, parameterName);

            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] != 0 && labels[i] != 1)
                {
                    throw new ArgumentException(
                        $"{parameterName} must contain only 0 (event absent) or 1 (event present); entry {i} is {labels[i]}.",
                        parameterName);
                }
            }
        }

        public static void NonNegativeCount(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentException($"{parameterName} must be a finite count that is not negative.",
                    parameterName);
            }
        }

        public static void PositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                throw new ArgumentException($"{parameterName} must be a finite number greater than zero.",
                    parameterName);
            }
        }

        public static double[][] Table(double[][] counts, string parameterName, int minimumRows = 2,
            int minimumColumns = 2)
        {
            Required(counts, parameterName);

            if (counts.Length < minimumRows)
            {
                throw new ArgumentException(
                    $"{parameterName} needs at least {minimumRows} rows of counts (one array per category of the row variable).",
                    parameterName);
            }

            var columns = counts[0].Length;

            if (columns < minimumColumns)
            {
                throw new ArgumentException(
                    $"{parameterName} needs at least {minimumColumns} columns of counts in every row.", parameterName);
            }

            var copy = new double[counts.Length][];

            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] == null || counts[i].Length != columns)
                {
                    throw new ArgumentException(
                        $"Every row of {parameterName} must have the same number of columns ({columns}); row {i} does not.",
                        parameterName);
                }

                copy[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    var value = counts[i][j];

                    if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                    {
                        throw new ArgumentException(
                            $"{parameterName} must hold finite counts that are not negative; row {i} column {j} is {value}.",
                            parameterName);
                    }

                    copy[i][j] = value;
                }
            }

            return copy;
        }

        public static double Total(double[][] table)
        {
            return table.Sum(r => r.Sum());
        }

        public static string[] Names(string[]? names, int count, string prefix, string parameterName)
        {
            if (names == null || names.Length == 0)
            {
                return Enumerable.Range(0, count).Select(i => prefix + (i + 1)).ToArray();
            }

            if (names.Length != count)
            {
                throw new ArgumentException(
                    $"{parameterName} must have exactly {count} names, one per position, or be left empty.",
                    parameterName);
            }

            return names;
        }

        public static double TwoSidedZ(double level)
        {
            return CategoricalLibrary.NormalQuantile(1d - (1d - level) / 2d);
        }

        public static double Round(double value, int digits)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? value : Math.Round(value, digits);
        }
    }
}